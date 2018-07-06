#pragma once
#include <string>
#include <exception>
#include <memory>

namespace Microsoft {
namespace Azure {
namespace Batch {
namespace SoftwareEntitlement {

//
// The following initialization function must be invoked from the program's
// entry function (e.g. 'main'), as this library uses libcurl which has an
// absurd requirement that no other threads exist in the application when it
// is initialized.  See https://curl.haxx.se/libcurl/c/curl_global_init.html.
//
// Returns 0 if successful.
//
int Init();

void Cleanup();


class Exception : public std::runtime_error
{
public:
    explicit Exception(const std::string& message);

    explicit Exception(const char *message);
};


class Entitlement
{
private:
    std::string m_id;
    std::string m_vmid;

public:
    Entitlement(const std::string& response);

    virtual ~Entitlement();

    const std::string& Id() const;

    const std::string& VmId() const;
};


//
// Returns an Entitlement object, throws an Exception providing details of
// entitlement validation failure.
//
std::unique_ptr<Entitlement> GetEntitlement(
    std::string url,
    const std::string& entitlement_token,
    const std::string& requested_entitlement,
    unsigned int retries = 5
);


void AddSslCertificate(
    const std::string& ssl_cert_thumbprint,
    const std::string& ssl_cert_common_name
);

}
}
}
}
