using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class Program
    {
        static int Main(string[] args)
        {
            var parser = new Parser(ConfigureParser);

            var parseResult = parser
                .ParseArguments<GenerateOptions, VerifyOptions, ServerOptions>(args);

            parseResult.WithParsed((OptionsBase options) => SetupLogging(options));

            var exitCode = parseResult.MapResult(
                (GenerateOptions options) => Generate(options),
                (VerifyOptions options) => Verify(options),
                (ServerOptions options) => Serve(options),
                errors => 1);

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
            }

            return exitCode;
        }

        /// <summary>
        /// Generation mode - create a new token for testing
        /// </summary>
        /// <param name="options">Options from the command line.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Generate(GenerateOptions options)
        {
            var logger = GlobalLogger.Logger;
            var entitlement = new SoftwareEntitlement(logger)
                .WithVirtualMachineId(options.VirtualMachineId)
                .ForTimeRange(options.NotBefore, options.NotAfter);

            if (entitlement.HasErrors)
            {
                logger.LogError("Unable to generate template; please address the reported errors and try again.");
                return -1;
            }

            var generator = new TokenGenerator(logger);
            var token = generator.Generate(entitlement);
            if (token == null)
            {
                return -1;
            }

            logger.LogInformation("Token: {JWT}", token);
            return 0;
        }

        /// <summary>
        /// Verify mode - check that a token is valid
        /// </summary>
        /// <param name="options">Options from the command line.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Verify(VerifyOptions options)
        {
            return 0;
        }

        /// <summary>
        /// Serve mode - run as a standalone webapp 
        /// </summary>
        /// <param name="options">Options from the commandline.</param>
        /// <returns>Exit code to return from this process.</returns>
        public static int Serve(ServerOptions options)
        {
            var contentDirectory = FindContentDirectory();
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(contentDirectory.FullName)
                .UseStartup<Startup>()
                .Build();

            // This sends output directly to the console which is a bit naff
            // but avoiding it would probably be brittle.
            host.Run();

            return 0;
        }

        /// <summary>
        /// Find our content directory for static content
        /// </summary>
        /// <remarks>Does not include the wwwroot part of the path.</remarks>
        /// <returns>Information about the directory to use.</returns>
        private static DirectoryInfo FindContentDirectory()
        {
            var hostAssembly = typeof(Startup).GetTypeInfo().Assembly;
            var hostFileInfo = new FileInfo(hostAssembly.Location);
            var hostDirectory = hostFileInfo.Directory;
            return hostDirectory;
        }

        /// <summary>
        /// Ensure our logging is properly initialized
        /// </summary>
        /// <param name="options">Options selected by the user (if any).</param>
        private static void SetupLogging(OptionsBase options)
        {
            var logger = GlobalLogger.CreateLogger(options.LogLevel);
            logger.LogInformation("Software Entitlement Service Command Line Utility");
        }

        /// <summary>
        /// Configure parsing of our commandline options
        /// </summary>
        /// <param name="settings">Settings instance to update.</param>
        private static void ConfigureParser(ParserSettings settings)
        {
            settings.CaseInsensitiveEnumValues = true;
            settings.CaseSensitive = false;
            settings.EnableDashDash = true;
            settings.IgnoreUnknownArguments = false;
            settings.HelpWriter = Console.Error;
        }


    }
}
