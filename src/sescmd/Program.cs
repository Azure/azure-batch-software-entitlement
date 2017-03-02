using CommandLine;
using Serilog;
using Serilog.Events;

namespace Microsoft.Azure.Batch.SoftwareEntitlement
{
    public class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default
                .ParseArguments<GenerateOptions, VerifyOptions, ServerOptions>(args)
                .MapResult(
                    (GenerateOptions options) => Generate(options),
                    (VerifyOptions options) => Verify(options),
                    (ServerOptions options) => Serve(options),
                    errors => 1);
        }

        public static int Generate(GenerateOptions options)
        {
            var logger = CreateLogger(options);

            return 0;
        }

        public static int Verify(VerifyOptions options)
        {
            var logger = CreateLogger(options);
            return 0;
        }

        public static int Serve(ServerOptions options)
        {
            var logger = CreateLogger(options);
            return 0;
        }
    }
}