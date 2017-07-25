#include "stdafx.h"


void ShowUsage(const char* exeName)
{
    std::cout << exeName << ":" << std::endl;
    std::cout << "Contacts the specified Azure Batch software entitlement server to verify the provided token." << std::endl;
    std::cout << "Parameters:" << std::endl;
    std::cout << "    --url <software entitlement server URL>" << std::endl;
    std::cout << "    --thumbprint <thumbprint of a certificate expected in the server's SSL certificate chain>" << std::endl;
    std::cout << "    --common-name <common name of the certificate with the specified thumbprint>" << std::endl;
    std::cout << "    --token <software entitlement token to pass to the server>" << std::endl;
    std::cout << "    --application <name of the license ID being requested>" << std::endl;
}


static const std::array<std::string, 3> mandatoryParameterNames = {
    "--url",
    "--token",
    "--application"
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

auto shouldShowUsage = false;
auto configurationError = false;

std::unordered_map<std::string, std::string> ReadParameters(int argc, char** argv)
{
	std::unordered_map<std::string, std::string> parameters;

	if ((argc % 2) == 1)
	{
		// We have pairs of parameters, collect them into our map
		while (argc != 1)
		{
			auto value = argv[--argc];
			auto key = argv[--argc];
			parameters.emplace(key, value);
		}
	}

	if (parameters.size() == 0)
	{
		shouldShowUsage = true;
	}
	else
	{
		for (const auto& param : mandatoryParameterNames)
		{
			if (parameters.find(param) == parameters.end())
			{
				std::cout << "Missing mandatory parameter " << param << std::endl;
				configurationError = true;
			}
		}
	}

	return parameters;
}

std::string ReadToken(const std::unordered_map<std::string, std::string>& parameters)
{
	std::string token = parameters.find("--token")->second;
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

		auto parameters = ReadParameters(argc, argv);
		auto token = ReadToken(parameters);

        Microsoft::Azure::Batch::SoftwareEntitlement::AddSslCertificate(
            parameters.find("--thumbprint")->second,
            parameters.find("--common-name")->second
        );

		if (shouldShowUsage)
		{
			ShowUsage(argv[0]);
			return -1;
		}

		if (configurationError)
		{
			return -EINVAL;
		}

        auto entitlement = Microsoft::Azure::Batch::SoftwareEntitlement::GetEntitlement(
            parameters.find("--url")->second,
            token,
            parameters.find("--application")->second
        );
        std::cout << entitlement->Id() << std::endl;
    }
    catch (const std::exception& e)
    {
        std::cout << e.what() << std::endl;
        return -1;
    }
    return 0;
}

