#if _MSC_VER
// The use of std::getenv generates a warning on Windows.
// Since the aim is to have portable code, suppress the warnings.
#define _CRT_SECURE_NO_WARNINGS
#endif
#include "SoftwareEntitlementClient.h"
#include <algorithm>
#include <cstddef>
#include <mutex>
#include <sstream>
#include <vector>
#include <cstdlib>
#include <cstdint>
#include <cstring>
#include <curl/curl.h>
#include <openssl/bio.h>
#include <openssl/err.h>
#include <openssl/pem.h>
#include <openssl/ssl.h>
#include <openssl/x509.h>

//
// Work around non-standard/incomplete C++ support in VC11/VC12
//
#if defined _MSC_VER && _MSC_VER < 1900
//
// When building with earlier versions of Visual Studio, users must:
// - modify 'AdditionalIncludeDirectories' to include the vcpkg include
//   directory, e.g. 'D:\GitHub\vcpkg\installed\x64-windows\include'
// - modify 'AdditionalLibraryDirectories' to include the vcpkg lib
//   directory, e.g. 'D:\GitHub\vcpkg\installed\x64-windows\lib'
// - modify 'AdditionalDependencies' to include the following libs:
//      o ssleay32.lib
//      o libeay32.lib
//      o libcurl_imp.lib
//
#include "json_vc11.hpp"
#else
#include "json.hpp"
#endif

#ifdef _WIN32
#include <Wincrypt.h>
#include <winhttp.h>
#endif


namespace Microsoft {
namespace Azure {
namespace Batch {
namespace SoftwareEntitlement {
namespace {

std::mutex s_lock;

typedef std::array<std::uint8_t, 20> SHA256Thumbprint;
struct CertInfo
{
    SHA256Thumbprint thumbprint;
    std::string common_name;
    std::string allowed_dns_namespace;
};

//
// Published Microsoft Intermediate Certificates from https://www.microsoft.com/pki/mscorp/cps/
//
CertInfo s_Microsoft_IT_SSL_SHA2 = {
    {{ 0x97,0xef,0xf3,0x02,0x86,0x77,0x89,0x4b,0xdd,0x4f,0x9a,0xc5,0x3f,0x78,0x9b,0xee,0x5d,0xf4,0xad,0x86 }} ,
    "Microsoft IT SSL SHA2"
};

CertInfo s_Microsoft_IT_SSL_SHA2_2 = {
    {{ 0x94,0x8e,0x16,0x52,0x58,0x62,0x40,0xd4,0x53,0x28,0x7a,0xb6,0x9c,0xae,0xb8,0xf2,0xf4,0xf0,0x21,0x17 }},
    "Microsoft IT SSL SHA2"
};

CertInfo s_Microsoft_IT_TLS_CA_1 = {
    {{ 0x41,0x7e,0x22,0x50,0x37,0xfb,0xfa,0xa4,0xf9,0x57,0x61,0xd5,0xae,0x72,0x9e,0x1a,0xea,0x7e,0x3a,0x42 }},
    "Microsoft IT TLS CA 1"
};

CertInfo s_Microsoft_IT_TLS_CA_2 = {
    {{ 0x54,0xd9,0xd2,0x02,0x39,0x08,0x0c,0x32,0x31,0x6e,0xd9,0xff,0x98,0x0a,0x48,0x98,0x8f,0x4a,0xdf,0x2d }},
    "Microsoft IT TLS CA 2"
};

CertInfo s_Microsoft_IT_TLS_CA_4 = {
    {{ 0x8a,0x38,0x75,0x5d,0x09,0x96,0x82,0x3f,0xe8,0xfa,0x31,0x16,0xa2,0x77,0xce,0x44,0x6e,0xac,0x4e,0x99 }},
    "Microsoft IT TLS CA 4"
};

CertInfo s_Microsoft_IT_TLS_CA_5 = {
    {{ 0xad,0x89,0x8a,0xc7,0x3d,0xf3,0x33,0xeb,0x60,0xac,0x1f,0x5f,0xc6,0xc4,0xb2,0x21,0x9d,0xdb,0x79,0xb7 }},
    "Microsoft IT TLS CA 5"
};

CertInfo s_Batch_USGov_CloudAPI_CA = {
    {{ 0x1f,0xb8,0x6b,0x11,0x68,0xec,0x74,0x31,0x54,0x06,0x2e,0x8c,0x9c,0xc5,0xb1,0x71,0xa4,0xb7,0xcc,0xb4 }},
    "DigiCert SHA2 Secure Server CA",
    ".batch.usgovcloudapi.net"
};

CertInfo s_Batch_China_CloudAPI_CA = {
	{ { 0x1f,0xb8,0x6b,0x11,0x68,0xec,0x74,0x31,0x54,0x06,0x2e,0x8c,0x9c,0xc5,0xb1,0x71,0xa4,0xb7,0xcc,0xb4 } },
	"DigiCert SHA2 Secure Server CA",
	".batch.chinacloudapi.cn"
};

CertInfo s_Batch_Germany_CloudAPI_CA = {
    {{ 0x2f,0xc5,0xde,0x65,0x28,0xcd,0xbe,0x50,0xa1,0x4c,0x38,0x2f,0xc1,0xde,0x52,0x4f,0xaa,0xbf,0x95,0xfc }},
    "D-TRUST SSL Class 3 CA 1 2009",
     ".batch.microsoftazure.de"
};

std::array<CertInfo, 9> s_microsoftIntermediateCerts = {{
    s_Microsoft_IT_SSL_SHA2,
    s_Microsoft_IT_SSL_SHA2_2,
    s_Microsoft_IT_TLS_CA_1,
    s_Microsoft_IT_TLS_CA_2,
    s_Microsoft_IT_TLS_CA_4,
    s_Microsoft_IT_TLS_CA_5,
    s_Batch_USGov_CloudAPI_CA,
    s_Batch_China_CloudAPI_CA,
    s_Batch_Germany_CloudAPI_CA
}};

std::vector<CertInfo> s_sslCerts;

std::string ExtractValue(const std::string& response, const std::string& key)
{
    nlohmann::json j = nlohmann::json::parse(response.c_str());

    return j.at(key);
}


template <class T, typename R, R(*F)(T* ptr)> struct OpenSSLDeleter
{
    void operator() (T* ptr)
    {
        F(ptr);
    }
};


extern "C" int cout_cb(const char* val, size_t /*len*/, void* u)
{
    std::ostringstream& str = *static_cast<std::ostringstream*>(u);
    str << val << std::endl;
    return 1;
}


#ifdef _WIN32
void ThrowIfOpenSSLError(bool error)
{
    if (error)
    {
        std::ostringstream str;
        ERR_print_errors_cb(cout_cb, &str);

        throw Exception(str.str());
    }
}


void ThrowIfWin32Error(bool error)
{
    if (!error)
    {
        return;
    }

    DWORD lastErr = GetLastError();
    throw std::system_error(std::error_code(lastErr, std::system_category()));
}
#endif // _WIN32


class X509
{
    std::unique_ptr<::x509_st, OpenSSLDeleter<::x509_st, void, &X509_free>> _cert;

public:
    X509(::x509_st*&& ptr)
        : _cert(ptr)
    {
    }

    X509(X509&& rhs)
    {
        _cert.swap(rhs._cert);
    }

    bool MatchesThumbprint(const SHA256Thumbprint& thumb) const
    {
        if (_cert == nullptr)
        {
            return false;
        }

        auto myThumb = Thumbprint();
        if (myThumb.size() != thumb.size())
        {
            return false;
        }

        return std::memcmp(thumb.data(), myThumb.data(), thumb.size()) == 0;
    }

    std::string CommonName() const
    {
        X509_name_st* subj = X509_get_subject_name(_cert.get());

        int lastpos = X509_NAME_get_index_by_NID(subj, NID_commonName, -1);
        if (lastpos == -1)
        {
            throw Exception("Certificate does not have a common name");
        }

        X509_NAME_ENTRY* nameEntry = X509_NAME_get_entry(subj, lastpos);
        ASN1_STRING* asn1String = X509_NAME_ENTRY_get_data(nameEntry);
        return reinterpret_cast<char*>(ASN1_STRING_data(asn1String));
    }

    std::vector<std::uint8_t> Thumbprint() const
    {
        std::vector<std::uint8_t> thumb;

        //
        // Resize so we can use data() below to fill in.
        //
        thumb.resize(EVP_MAX_MD_SIZE);
        unsigned int cbDigest = EVP_MAX_MD_SIZE;
        if (X509_digest(_cert.get(), EVP_sha1(), thumb.data(), &cbDigest) != 1)
        {
            throw Exception("Failed to calculate thumbprint for certificate " + CommonName());
        }

        thumb.resize(cbDigest);
        return thumb;
    }
};


std::string StripNonHexThumbprintDigits(const std::string& input)
{
    std::string output = input;
    output.erase(
        std::remove_if(output.begin(), output.end(), [](const char x) { return !isxdigit(x); }),
        output.end());

    return output;
}


SHA256Thumbprint ThumbprintToBinary(const std::string& thumbprint)
{
    std::string digits = StripNonHexThumbprintDigits(thumbprint);
    SHA256Thumbprint sha256Thumb;
    if (digits.size() != sha256Thumb.size() * 2)
    {
        throw Exception("Malformed thumbprint: '" + digits + "'");
    }

    //
    // Convert to binary representation of thumbprint.
    //
    for (size_t digit = 0; digit < sha256Thumb.size(); digit++)
    {
        sha256Thumb[digit] = static_cast<std::uint8_t>(std::stoul(digits.substr(digit * 2, 2), 0, 16));
    }

    return sha256Thumb;
}


class Curl
{
    struct CurlDeleter
    {
        void operator ()(CURL* curl)
        {
            curl_easy_cleanup(curl);
        }
    };
    std::unique_ptr<CURL, CurlDeleter> _curl;

    char _errbuf[CURL_ERROR_SIZE];
    std::string _response;

public:
    class CurlException : public Exception
    {
        CURLcode _code;

    public:
        CurlException(CURLcode code, const std::string& message)
            : Exception(message)
            , _code(code)
        {}

        CurlException(CURLcode code, const char *message)
            : Exception(message)
            , _code(code)
        {}

        CURLcode GetCode() const
        {
            return _code;
        }
    };

private:
    void ThrowIfCurlError(CURLcode res)
    {
        if (res == CURLE_OK)
        {
            return;
        }

        std::ostringstream what;
        what << "libcurl_error " << res << ": " << _errbuf;
        throw CurlException(res, what.str());
    }

    static size_t WriteCallback(char* ptr, size_t size, size_t nmemb, void* context)
    {
        Curl* self = static_cast<Curl*>(context);
        try
        {
            size_t nbytes = size * nmemb;
            self->_response.append(ptr, nbytes);
            return nbytes;
        }
        catch (const std::exception&)
        {
            return 0;
        }
    }

    std::string GetDetailedErrorMessage()
    {
        try
        {
            nlohmann::json j = nlohmann::json::parse(_response.c_str());

            try
            {
                return j.at("message").at("value");
            }
            catch (const nlohmann::detail::exception&)
            {
                return j.at("code");
            }
        }
        catch (const nlohmann::detail::exception&)
        {
            return "Unknown error: HTTP Status 400 missing expected output";
        }
    }

    std::string GetErrorMessage(long code)
    {
        switch (code)
        {
        case 400:
        case 403:
            return GetDetailedErrorMessage();

        default:
            return "Unexpected error: HTTP status " + std::to_string(code);
        }
    }

#ifdef _WIN32
    static CURLcode OpenSSLContextCallback(CURL* /*curl*/, void* ssl_ctx, void* /*userptr*/)
    {
        struct CertCloseStoreDeleter
        {
            void operator ()(HCERTSTORE hStore)
            {
                CertCloseStore(hStore, 0);
            }
        };

        try
        {
            //
            // Populate the OpenSSL certificate store with the system root certificates.
            //
            std::unique_ptr<void, CertCloseStoreDeleter> hStore(CertOpenSystemStoreW(0, L"ROOT"));
            ThrowIfWin32Error(hStore == nullptr);

            X509_STORE* sslStore = SSL_CTX_get_cert_store(static_cast<SSL_CTX*>(ssl_ctx));
            ThrowIfOpenSSLError(sslStore == nullptr);

            PCCERT_CONTEXT pCertContext = CertEnumCertificatesInStore(hStore.get(), nullptr);
            while (pCertContext != nullptr)
            {
                const BYTE* cert = pCertContext->pbCertEncoded;
                ::X509* x509 = d2i_X509(nullptr, &cert, pCertContext->cbCertEncoded);
                if (x509 != nullptr)
                {
                    //
                    // Ignore failures - missing certs will manifest in cert chain validation failures.
                    //
                    X509_STORE_add_cert(sslStore, x509);
                    X509_free(x509);
                }
                pCertContext = CertEnumCertificatesInStore(hStore.get(), pCertContext);
            }
        }
        catch (const Exception&)
        {
            return CURLE_OUT_OF_MEMORY;
        }

        return CURLE_OK;
    }
#endif  // _WIN32

    X509 GetCertificate(const curl_slist* certinfo)
    {
        for (;
            certinfo != nullptr;
            certinfo = certinfo->next)
        {
            std::string data(certinfo->data);
            size_t pos = data.rfind("Cert:", 0);
            if (pos == std::string::npos)
            {
                continue;
            }

            std::unique_ptr<BIO, OpenSSLDeleter<BIO, int, &BIO_free>>bio(BIO_new(BIO_s_mem()));
            BIO_puts(bio.get(), data.substr(5).c_str());
            return X509(PEM_read_bio_X509(bio.get(), nullptr, nullptr, nullptr));
        }

        return nullptr;
    }

public:
    Curl()
        : _curl(curl_easy_init())
    {
        memset(_errbuf, 0, sizeof(_errbuf));

        if (_curl == nullptr)
        {
            throw Exception("curl_easy_init failed.");
        }

        curl_easy_setopt(_curl.get(), CURLOPT_ERRORBUFFER, _errbuf);

        //
        // Require TLSv1_2 always.
        //
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_USE_SSL, CURLUSESSL_ALL));
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_SSLVERSION, CURL_SSLVERSION_TLSv1_2));
        //
        // During testing, if the certificate chain leaves something to be
        // desired, disable the following two options by setting them to 0.
        //
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_SSL_VERIFYHOST, 2));
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_SSL_VERIFYPEER, 1));
        //
        // Collect certificate info to allow for common name and thumbprint checking.
        //
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_CERTINFO, 1));

        //
        // Set context for write callback.
        //
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_WRITEDATA, this));
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_WRITEFUNCTION, WriteCallback));

        //
        // On Windows, set the OpenSSL SSL_CTX callback in order to populate the
        // OpenSSL certificate store with the system root certificates.
        //
#ifdef _WIN32
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_SSL_CTX_FUNCTION, OpenSSLContextCallback));
#endif // _WIN32

        // Allow overriding the default connection timeout of 300 seconds.
        const char* env = std::getenv("AZ_BATCH_SES_CURLOPT_CONNECTTIMEOUT");
        long timeout = 0;
        if (env != nullptr)
        {
            timeout = std::strtol(env, nullptr, 10);
        }
        if (timeout <= 0 || timeout == LONG_MAX)
        {
            timeout = 300L;
        }
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_CONNECTTIMEOUT, timeout));
    }

    void Post(
        const std::string& url,
        const std::string& entitlement_token,
        const std::string& requested_entitlement)
    {
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_URL, url.c_str()));

        //
        // We don't use std::unique_ptr here because curl_slist_append does not free
        // the previous structure, and could change in the future to be implemented
        // as a stack rather than a list (so that insertions don't walk the list).
        //
        struct headers_guard
        {
            curl_slist *value;

            headers_guard()
                :value(nullptr)
            {}

            ~headers_guard()
            {
                if (value != nullptr)
                {
                    curl_slist_free_all(value);
                }
            }
        } headers;

        headers.value = curl_slist_append(nullptr, "Content-Type: application/json; odata=minimalmetadata");

        if (headers.value == nullptr)
        {
            throw Exception("Failed to allocate Content-Type header");
        }

        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_HTTPHEADER, headers.value));

        nlohmann::json j;
        j["token"] = entitlement_token;
        j["applicationId"] = requested_entitlement;

        //
        // We need to ensure the payload remains resident for the duration of
        // the transfer.  We store it in a temporary here rather than have
        // libcurl buffer it for us (by using CURLOPT_COPYPOSTFIELDS).
        //
        std::string body = j.dump();
        ThrowIfCurlError(curl_easy_setopt(_curl.get(), CURLOPT_POSTFIELDS, body.c_str()));

        ThrowIfCurlError(curl_easy_perform(_curl.get()));
    }

    //
    // Perform additional certificate checks:
    // - Find any one of the certificates in the s_sslCerts vector by thumbprint.
    // - Verify that such cetificate has the matching common name.
    //
    void VerifyIntermediateCertificate(const std::string& url)
    {
        curl_certinfo* info;
        ThrowIfCurlError(curl_easy_getinfo(_curl.get(), CURLINFO_CERTINFO, &info));

        for (int i = 0; i < info->num_of_certs; i++)
        {
            X509 cert = GetCertificate(info->certinfo[i]);

            for (const auto& validCert : s_sslCerts)
            {
                if (!cert.MatchesThumbprint(validCert.thumbprint))
                {
                    continue;
                }

                auto certName = cert.CommonName();
                if (certName != validCert.common_name)
                {
                    //
                    // Thumbprint match, but common name mismatch.
                    //
                    throw Exception(
                        "Certificate common name does not match, expected '" +
                        validCert.common_name +
                        "' but got '" +
                        certName +
                        "'");
                }

                if (validCert.allowed_dns_namespace.empty())
                {
                    return;
                }

                if (url.find(validCert.allowed_dns_namespace) != std::string::npos)
                {
                    return;
                }
            }
        }

        throw Exception("None of the candidate certificates were found in certificate chain.");
    }

    std::unique_ptr<Entitlement> GetEntitlement()
    {
        long code;
        ThrowIfCurlError(curl_easy_getinfo(_curl.get(), CURLINFO_RESPONSE_CODE, &code));

        if (code == 200)
        {
            return std::unique_ptr<Entitlement>(new Entitlement(_response));
        }

        throw Exception(GetErrorMessage(code));
    }

    static std::unique_ptr<Entitlement> GetEntitlement(
        const std::string& url,
        const std::string& entitlement_token,
        const std::string& requested_entitlement)
    {
        Curl curl;
        curl.Post(url + "softwareEntitlements?api-version=2017-05-01.5.0", entitlement_token, requested_entitlement);

        curl.VerifyIntermediateCertificate(url);

        return curl.GetEntitlement();
    }
};

struct WinHttpDeleter
{
    void operator()(HINTERNET h)
    {
        WinHttpCloseHandle(h);
    }
};

typedef std::unique_ptr<void, WinHttpDeleter> WinHttpHandle;

//
// OpenSSL does not hook into the Windows Automatic Root Certificates Update process.
// This results in certificate validation failures, so we perform a dummy connection
// using WinHTTP which will setup the root cert store correctly.
//
void EnsureRootCertsArePopulated(const std::string& url)
{
    std::string prefix("https://");
    size_t start = url.rfind(prefix, 0);
    if (start == std::string::npos)
    {
        throw Exception("Malformed URL: " + url);
    }

    start += prefix.length();
    size_t end = url.find("/", start);
    std::string temp;
    if (end == std::string::npos)
    {
        temp = url.substr(start);
    }
    else
    {
        temp = url.substr(start, end - start);
    }

    std::wstring hostname(temp.cbegin(), temp.cend());

    WinHttpHandle hSession(
        WinHttpOpen(
            L"Azure Batch Software Entitlement Service client",
            WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY,
            WINHTTP_NO_PROXY_NAME,
            WINHTTP_NO_PROXY_BYPASS,
            0)
    );
    ThrowIfWin32Error(hSession == nullptr);

    WinHttpHandle hConn(
        WinHttpConnect(hSession.get(), hostname.c_str(), INTERNET_DEFAULT_HTTPS_PORT, 0)
    );
    ThrowIfWin32Error(hConn == nullptr);

    WinHttpHandle hRequest(
        WinHttpOpenRequest(
            hConn.get(),
            nullptr,
            nullptr,
            nullptr,
            WINHTTP_NO_REFERER,
            WINHTTP_DEFAULT_ACCEPT_TYPES,
            WINHTTP_FLAG_SECURE)
    );
    ThrowIfWin32Error(hRequest == nullptr);

    ThrowIfWin32Error(TRUE != WinHttpSendRequest(
        hRequest.get(),
        WINHTTP_NO_ADDITIONAL_HEADERS,
        0,
        WINHTTP_NO_REQUEST_DATA,
        0,
        0,
        0)
    );
}

}   // anonymous namespace


Exception::Exception(const std::string& message)
    : std::runtime_error(message)
{
}

Exception::Exception(const char *message)
    : std::runtime_error(message)
{
}


Entitlement::Entitlement(const std::string& response)
    : m_id(ExtractValue(response, "id"))
    , m_vmid(ExtractValue(response, "vmid"))
{
}

Entitlement::~Entitlement()
{
}

const std::string& Entitlement::Id() const
{
    return m_id;
}

const std::string& Entitlement::VmId() const
{
    return m_vmid;
}


int Init()
{
    s_sslCerts.insert(s_sslCerts.end(), s_microsoftIntermediateCerts.cbegin(), s_microsoftIntermediateCerts.cend());
    return curl_global_init(CURL_GLOBAL_ALL);
}


void Cleanup()
{
    curl_global_cleanup();
}


//
// Returns an Entitlement object, throws an EntitlementException
// providing details of entitlement validation failure.
//
std::unique_ptr<Entitlement> GetEntitlement(
    std::string url,
    const std::string& entitlement_token,
    const std::string& requested_entitlement,
    unsigned int retries)
{
    std::lock_guard<std::mutex> lock(s_lock);

    if (url.rfind("https://", 0) == std::string::npos)
    {
        throw Exception("Invalid input URL: must start with \"https://\"");
    }

    if (url.find('?') != std::string::npos)
    {
        throw Exception("Invalid input URL: must not contain any query params");
    }

    size_t pos = url.find('/', 8);
    if (pos < url.length() - 1)
    {
        pos = url.find('/', pos + 1);
        if (pos < url.length() - 1)
        {
            throw Exception("Invalid input URL: should not include more than one slash after the hostname (excluding trailing slash).");
        }
    }

    if (pos == std::string::npos)
    {
        url += '/';
    }

    for (unsigned int retry = 1; retry <= retries; ++retry)
    {
        try
        {
            return Curl::GetEntitlement(url, entitlement_token, requested_entitlement);
        }
        catch (const Curl::CurlException& e)
        {
            if (e.GetCode() == CURLE_OPERATION_TIMEDOUT)
            {
                std::this_thread::sleep_for(std::chrono::seconds(retry));
            }
#ifdef WIN32
            else if (e.GetCode() == CURLE_SSL_CACERT)
            {
                EnsureRootCertsArePopulated(url);
            }
#endif
            else
            {
                throw e;
            }
        }
    }

    return Curl::GetEntitlement(url, entitlement_token, requested_entitlement);
}


void AddSslCertificate(
    const std::string& ssl_cert_thumbprint,
    const std::string& ssl_cert_common_name)
{
    std::lock_guard<std::mutex> lock(s_lock);

    CertInfo info = { ThumbprintToBinary(ssl_cert_thumbprint), ssl_cert_common_name, {} };
    s_sslCerts.push_back(info);
}


}
}
}
}
