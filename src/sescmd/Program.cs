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
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class Program
    {
        static int Main(string[] args)
        {
            var parseResult = Parser.Default
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

        private static void SetupLogging(OptionsBase options)
        {
            var level = options.SelectLogEventLevel();
            var logger = SimpleLoggerFactory.CreateLogger(level);
            logger.Information("Software Entitlement Service Command Line Utility");
            options.WarnAboutInactiveOptions(level, logger);
        }

        public static int Generate(GenerateOptions options)
        {
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
    }
}
