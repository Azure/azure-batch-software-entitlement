using CommandLine;

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
            return 0;
        }

        public static int Verify(VerifyOptions options)
        {
            return 0;
        }

        public static int Serve(ServerOptions options)
        {
            return 0;
        }
    }
}