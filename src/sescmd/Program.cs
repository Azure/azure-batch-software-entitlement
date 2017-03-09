using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class Program
    {
        static int Main(string[] args)
        {
            var parser = new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.EnableDashDash = true;
                settings.IgnoreUnknownArguments = false;
                settings.HelpWriter = Console.Error;
            });

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

        public static int Generate(GenerateOptions options)
        {
            var logger = SimpleLoggerFactory.Logger;
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

        public static int Verify(VerifyOptions options)
        {
            return 0;
        }

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

        private static DirectoryInfo FindContentDirectory()
        {
            var hostAssembly = typeof(Startup).GetTypeInfo().Assembly;
            var hostFileInfo = new FileInfo(hostAssembly.Location);
            var hostDirectory = hostFileInfo.Directory;
            return hostDirectory;
        }

        private static void SetupLogging(OptionsBase options)
        {
            var logger = SimpleLoggerFactory.CreateLogger(options.LogLevel);
            logger.LogInformation("Software Entitlement Service Command Line Utility");
        }
    }
}
