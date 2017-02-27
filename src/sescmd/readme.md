# Software entitlement service testing utility

This command line executable is intended to aid with testing of integration with the Azure Batch Software Entitlement Service.

**This is draft documentation subject to change.**

## Available Modes

```
  generate    Generate a token with specified parameters

  verify      Verify a provided token by calling into the software entitlement service

  serve       Run as a standalone software entitlement server.

  help        Display more information on a specific command.

  version     Display version information.
```

## Standalone server

```
sescmd help serve

  -a, --authority             Certificate thumbprint used to sign the cert used for the HTTPS connection

  -s, --signature             Certificate thumbprint of the certificate used to sign the token

  -e, --encrypt               Certificate thumbprint of the certificate used to encrypt the token

  -x, --exit-after-request    Request the server exits after serving one request
```

## Token Verification

```
ses help verify

  --token                The token to verify

  --application-id       Unique identifier for the application

  --vmid                 Unique identifier for the Azure virtual machine

  --batch-account-url    URL of the Azure Batch account server

  -a, --authority        Certificate thumbprint used to sign the cert used for the HTTPS connection
```

## Token Generation

```
ses help generate

  --application-id    Unique identifier for the application

  --vmid              Unique identifier for the Azure virtual machine

  --not-before        The moment at which the token becomes active and the application is entitled to execute.

  --not-after         The moment at which the token expires and the application is no longer entitled to execute.

  --address           The externally visible IP address of the machine entitled to execute the application.

  -s, --signature     Certificate thumbprint of the certificate used to sign the token

  -e, --encrypt       Certificate thumbprint of the certificate used to encrypt the token
```

