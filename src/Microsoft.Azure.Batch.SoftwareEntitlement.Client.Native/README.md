# Software entitlement service native client library

This static library implements the client-side logic for verifying a software entitlement token provides a particular entitlement.  By default, it validates the connection to the server against a list of well-known long-lived Microsoft intermediate certificates, but supports additional certificates programatically.

**This is draft documentation subject to change.**

This project depends on two open-source packages: [OpenSSL](https://www.openssl.org/) and [libcurl](https://curl.haxx.se/libcurl/c/libcurl.html).

## Windows build
The included project for Visual Studio 2017 depends libcurl and OpenSSL being available.  The simplest mechanism to achieve this is to use [vcpkg](https://github.com/Microsoft/vcpkg), following the Quick Start instructions, including user-wide integration.

## Building with Visual Studio 2012 or Visual Studio 2013
The libcurl and OpenSSL libraries expose a standard C interface, so can be built using Visual Studio 2017 and referenced in projects using earlier versions of Visual Studio without issue.  However, the vcpkg user-wide integration does not support versions prior to Visual Studio 2015, requiring the following project configuration settings to be modified:
* C/C++ | General | AdditionalIncludeDirectories: include the vcpkg include directory, e.g. 'D:\GitHub\vcpkg\installed\x64-windows\include'
* Linker | General | AdditionalLibraryDirectories: include the vcpkg lib directory, e.g. 'D:\GitHub\vcpkg\installed\x64-windows\lib'
* Linker | Input | AdditionalDependencies: include the following libs:
	* ssleay32.lib
	* libeay32.lib
	* libcurl_imp.lib

## Installing OpenSSL
For 32-bit builds:
```
> vcpkg install openssl
```

For 64-bit builds:
```
> vcpkg install openssl --triplet x64-windows
```

## Installing libcurl
For 32-bit builds:
```
> vcpkg install curl
```

For 64-bit builds:
```
> vcpkg install curl --triplet x64-windows
```

## Configuring libcurl to use OpenSSL
In order to validate an intermediate certificate in the server's certificate chain (not just the server's certificate), configure libcurl to use OpenSSL as the SSL library.

In your vcpkg git repository clone, apply the following patch:

```
diff --git a/ports/curl/portfile.cmake b/ports/curl/portfile.cmake
index 35bfbd5..ac3b57f 100644
--- a/ports/curl/portfile.cmake
+++ b/ports/curl/portfile.cmake
@@ -43,6 +43,7 @@ else()
             -DBUILD_TESTING=OFF
             -DBUILD_CURL_EXE=OFF
             -DENABLE_MANUAL=OFF
+            -DCMAKE_USE_OPENSSL=ON
             -DCURL_STATICLIB=${CURL_STATICLIB}
         OPTIONS_DEBUG
             -DENABLE_DEBUG=ON
```

Now build and reinstall:

```
> vcpkg build curl [--triplet x64-Windows]
> vcpkg remove curl [--triplet x64-Windows]
> vcpkg install curl [--triplet x64-Windows]
```

## Usage
```
//
// The following initialization function must be invoked from the program's
// entry function (e.g. 'main'), as the library uses libcurl which has an
// absurd requirement that no other threads exist in the application when it
// is initialized.  See https://curl.haxx.se/libcurl/c/curl_global_init.html.
//
// Returns 0 if successful.
//
int err = Microsoft::Azure::Batch::SoftwareEntitlement::Init();
if (err != 0)
{
    ...
}

try
{
    //
    // Include the following call if you want to allow validating a server
    // connection using test certificates, passing the thumbprint and common
    // name of a certificate in the server's SSL certificate chain.  Remove it
    // for production releases: this will ensure that the code will only
    // authenticate to Azure Batch servers for token validation.
    //
    Microsoft::Azure::Batch::SoftwareEntitlement::AddSslCertificate(
        ssl_cert_thumbprint,
        ssl_cert_common_name
    );

    auto entitlement = Microsoft::Azure::Batch::SoftwareEntitlement::GetEntitlement(
        url,
        entitlement_token,
        requested_entitlement
    );
}
catch (const std::runtime_error& e)
{
    ...
}
//
// The entitlement can now be queried for its properties.
//
...

Microsoft::Azure::Batch::SoftwareEntitlement::Cleanup();
```

## Limitations
When calling ```AddSslCertificate```, you must not specify the thumbprint and common name of the root certificate of the server's SSL certificate chain.  This is because OpenSSL does not include the root certificate in the list of certificates.

## Troubleshooting

### Cannot open include file: 'curl/curl.h'

Cannot open include file: 'curl/curl.h' may indicate that you don't have the appropriate version of [**libcurl**](https://curl.haxx.se/libcurl/c/libcurl.html) installed for your target platform. (This can happen if you install the x64 version of `libcurl` but then ask for an `x86` build.)

**Solution**: Install `libcurl` for your target platform.
