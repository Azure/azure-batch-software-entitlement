#include "stdafx.h"


void ShowUsage(const char* exeName)
{
    std::cerr
        << exeName << ":" << std::endl
        << "Contacts the specified Azure Batch software entitlement server to verify the provided token." << std::endl
        << std::endl
        << "Mandatory parameters:" << std::endl
        << "    --url <software entitlement server URL>" << std::endl
        << "    --token <software entitlement token to pass to the server>" << std::endl
        << "    --application <name of the license ID being requested>" << std::endl
        << std::endl
        << "Mandatory parameters:" << std::endl
        << "    --thumbprint <thumbprint of a certificate expected in the server's SSL certificate chain>" << std::endl
        << "    --common-name <common name of the certificate with the specified thumbprint>" << std::endl;
}


static const std::array<std::string, 3> mandatoryParameterNames = {
    "--url",
    "--token",
    "--application"
};

static const std::array<std::string, 2> optionalParameterNames = {
    "--thumbprint",
    "--common-name"
};

struct Initializer
{
    Initializer()
    {
        int err = Microsoft::Azure::Batch::SoftwareEntitlement::Init();
        if (err != 0)
        {
            throw std::runtime_error(
                "Microsoft::Azure::Batch::SoftwareEntitlement::Init failed with error " +
                std::to_string(err)
            );
        }
    }

    ~Initializer()
    {
        Microsoft::Azure::Batch::SoftwareEntitlement::Cleanup();
    }
};

struct ParameterParser
{
public:

    bool parse(int argc, char** argv)
    {
        if ((argc % 2) == 1)
        {
            // We have pairs of parameters, collect them into our map
            while (argc != 1)
            {
                auto value = argv[--argc];
                auto key = argv[--argc];
                _parameters.emplace(key, value);
            }
        }

        if (_parameters.size() == 0)
        {
            // Need to show usage to the end user
            return true;
        }

        checkForMandatoryParameters(_parameters);
        checkForExtraParameters(_parameters);

        return false;
    }

    bool contains(const std::string& name) const
    {
        return _parameters.find(name) != _parameters.end();
    }

    std::string find(const std::string& name) const
    {
        return _parameters.find(name)->second;
    }

    bool hasConfigurationError() const
    {
        return _hasConfigurationError;
    }

private:
    bool _hasConfigurationError = false;
    std::unordered_map<std::string, std::string> _parameters;

    void checkForMandatoryParameters(const std::unordered_map<std::string, std::string>& parameters)
    {
        for (const auto& param : mandatoryParameterNames)
        {
            if (parameters.find(param) == parameters.end())
            {
                std::cerr << "Missing mandatory parameter " << param << std::endl;
                _hasConfigurationError = true;
            }
        }
    }

    void checkForExtraParameters(const std::unordered_map<std::string, std::string>& parameters)
    {
        for (const auto& parameter : parameters)
        {
            if (std::find(mandatoryParameterNames.begin(), mandatoryParameterNames.end(), parameter.first) != mandatoryParameterNames.end()) {
                continue;
            }

            if (std::find(optionalParameterNames.begin(), optionalParameterNames.end(), parameter.first) != optionalParameterNames.end()) {
                continue;
            }

            std::cerr << "Unexpected additional parameter: " << parameter.first << " " << parameter.second << std::endl;
            _hasConfigurationError = true;
        }
    }
};

bool configureConnection(const ParameterParser& parameters)
{
    if (!parameters.contains("--thumbprint")
        && !parameters.contains("--common-name"))
    {
        // We have neither value - and that's ok
        return true;
    }

    if (!parameters.contains("--thumbprint"))
    {
        std::cerr << "--thumbprint must also be used when --common-name is used" << std::endl;
        return false;
    }

    if (!parameters.contains("--common-name"))
    {
        std::cerr << "--common-name must also be used when --thumbprint is used" << std::endl;
        return false;
    }

    auto thumbprintParameter = parameters.find("--thumbprint");
    auto commonNameParameter = parameters.find("--common-name");

    Microsoft::Azure::Batch::SoftwareEntitlement::AddSslCertificate(thumbprintParameter, commonNameParameter);
    return true;
}

std::string readToken(ParameterParser& parameters)
{
    auto token = parameters.find("--token");
    if (token == "-")
    {
        // Read the token from stdin.
        std::getline(std::cin, token);
    }

    return token;
}

int main(int argc, char** argv)
{
    try
    {
        Initializer init;
        ParameterParser parser;

        auto shouldShowUsage = parser.parse(argc, argv);
        if (shouldShowUsage)
        {
            ShowUsage(argv[0]);
            return -1;
        }

        if (parser.hasConfigurationError())
        {
            return -EINVAL;
        }

        auto token = readToken(parser);
        auto connectionConfigured = configureConnection(parser);

        if (!connectionConfigured)
        {
            return -EINVAL;
        }

        auto entitlement = Microsoft::Azure::Batch::SoftwareEntitlement::GetEntitlement(
            parser.find("--url"),
            token,
            parser.find("--application")
        );

        std::cout << entitlement->Id() << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cerr << e.what() << std::endl;
        return -1;
    }

    return 0;
}

