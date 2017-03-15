# Software entitlement service native client library

This static library implements the client-side logic for verifying a software entitlement token provides a particular entitlement.

**This is draft documentation subject to change.**

This project depends on two open-source packages: [OpenSSL](https://www.openssl.org/) and [libcurl](https://curl.haxx.se/libcurl/c/libcurl.html).

## Windows build
The included project for Visual Studio 2017 depends libcurl and OpenSSL being available.  The simplest mechanism to achieve this is to use [vcpkg](https://github.com/Microsoft/vcpkg), following the Quick Start instructions, including user-wide integration.

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
    auto entitlement = Microsoft::Azure::Batch::SoftwareEntitlement::GetEntitlement(
        url,
        ssl_cert_thumbprint,
        ssl_cert_common_name,
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
The root certificate in the server's SSL certificate chain cannot be referenced by the ```ssl_cert_thumbprint```.
