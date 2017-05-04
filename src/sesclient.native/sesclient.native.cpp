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


static const std::array<std::string, 5> parameterNames = {
    "--url",
    "--thumbprint",
    "--common-name",
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


int main(int argc, char** argv)
{
    try
    {
        Initializer init;

        if (argc != 11)
        {
            ShowUsage(argv[0]);
            return -1;
        }

        std::unordered_map<std::string, std::string> parameters;
        while (argc != 1)
        {
            auto value = argv[--argc];
            auto key = argv[--argc];
            parameters.emplace(key, value);
        }

        for (const auto& param : parameterNames)
        {
            if (parameters.find(param) == parameters.end())
            {
                std::cout << "Missing parameter " << param << std::endl;
                return -EINVAL;
            }
        }

        std::string token = parameters.find("--token")->second;
        if (token == "-")
        {
            //
            // Read the thumbprint from stdin.
            //
            std::getline(std::cin, token);
        }

        auto entitlement = Microsoft::Azure::Batch::SoftwareEntitlement::GetEntitlement(
            parameters.find("--url")->second,
            parameters.find("--thumbprint")->second,
            parameters.find("--common-name")->second,
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

