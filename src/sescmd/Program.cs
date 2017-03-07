using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Batch.SoftwareEntitlement.Common;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server;
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

            return result;
        }

        public static int Generate(GenerateOptions options)
        {

            return 0;
        }

        public static int Verify(VerifyOptions options)
        {

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            host.Run();

            return 0;
        }

        {

            {
            }

        }
    }
}
