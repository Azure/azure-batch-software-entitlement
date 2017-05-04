#include "SoftwareEntitlementClient.h"
#include <algorithm>
#include <cstddef>
#include <mutex>
#include <sstream>
#include <vector>
#include <cstdint>
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
#include "Wincrypt.h"
#endif


namespace Microsoft {
namespace Azure {
namespace Batch {
namespace SoftwareEntitlement {
namespace {

std::mutex s_lock;


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

    bool operator !=(std::nullptr_t)
    {
        return _cert != nullptr;
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


std::vector<std::uint8_t> ThumbprintToBinary(const std::string& thumbprint)
{
    std::string digits = StripNonHexThumbprintDigits(thumbprint);
    if (digits.size() != 40)
    {
        throw Exception("Malformed thumbprint: '" + digits + "'");
    }

    //
    // Convert to binary representation of thumbprint.
    //
    std::vector<std::uint8_t> input;
    input.reserve(EVP_MAX_MD_SIZE);

    size_t pos = 0;
    while (pos < digits.size() && input.size() < EVP_MAX_MD_SIZE)
    {
        input.push_back(static_cast<std::uint8_t>(std::stoul(digits.substr(pos, 2), 0, 16)));
        pos += 2;
    }

    return input;
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

private:
    void ThrowIfCurlError(CURLcode res)
    {
        if (res == CURLE_OK)
        {
            return;
        }

        std::ostringstream what;
        what << "libcurl_error " << res << ": " << _errbuf;
        throw Exception(what.str());
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

        headers.value = curl_slist_append(nullptr, "Content-Type: application/json");

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

    X509 FindCertificate(const std::string& thumbprint)
    {
        std::vector<std::uint8_t> thumb = ThumbprintToBinary(thumbprint);

        curl_certinfo* info;
        ThrowIfCurlError(curl_easy_getinfo(_curl.get(), CURLINFO_CERTINFO, &info));

        for (int i = 0; i < info->num_of_certs; i++)
        {
            X509 cert = GetCertificate(info->certinfo[i]);
            if (cert != nullptr && cert.Thumbprint() == thumb)
            {
                return cert;
            }
        }

        throw Exception("Certificate with thumbprint '" + thumbprint + "' not found in certificate chain.");
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
};

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
    , m_vmId(ExtractValue(response, "vmId"))
{
}

Entitlement::~Entitlement()
{
}

const std::string& Entitlement::Id()
{
    return m_id;
}

const std::string& Entitlement::VmId()
{
    return m_vmId;
}


int Init()
{
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
    const std::string& url,
    const std::string& ssl_cert_thumbprint,
    const std::string& ssl_cert_common_name,
    const std::string& entitlement_token,
    const std::string& requested_entitlement
    )
{
    std::lock_guard<std::mutex> lock(s_lock);

    if (url.rfind("https://", 0) == std::string::npos)
    {
        throw Exception("Invalid input URL: must start with \"https://\"");
    }

    if (url.find("/", 8) != std::string::npos)
    {
        throw Exception("Invalid input URL: should not include any slashes after the hostname.");
    }

    Curl curl;
    curl.Post(url + "/softwareEntitlements/?apiVersion=2017-01-01.3.1", entitlement_token, requested_entitlement);

    //
    // Perform additional certificate checks:
    // - Find the certificate identified by 'ssl_cert_thumbprint' in the certificate chain.
    // - Verify that the cetificate has 'ssl_cert_common_name' as common name.
    //
    auto cert = curl.FindCertificate(ssl_cert_thumbprint);
    std::string actual = cert.CommonName();
    if (ssl_cert_common_name != actual)
    {
        throw Exception(
            "Certificate common name does not match, expected '" +
            ssl_cert_common_name +
            "' but got '" +
            actual +
            "'");
    }

    return curl.GetEntitlement();
}

}
}
}
}
