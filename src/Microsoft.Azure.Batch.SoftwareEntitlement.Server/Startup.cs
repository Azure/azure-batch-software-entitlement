using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Batch.SoftwareEntitlement.Server.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Batch.SoftwareEntitlement.Server
{
    public class Startup
    {
        private readonly ILoggerProvider _provider;

        public Startup(IHostingEnvironment env, ILoggerProvider provider)
        {
            _provider = provider;
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        [SuppressMessage(
            "Performance",
            "CA1822: Member ConfigureServices does not access instance data and can be marked as static",
            Justification = "Must be non-static to be called by the ASP.NET Runtime.")]
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddSingleton<ITokenPropertyParser>(provider =>
            {
                var serverOptions = provider.GetService<SoftwareEntitlementsController.ServerOptions>();
                return new JwtPropertyParser(
                    serverOptions.Audience,
                    serverOptions.Issuer,
                    serverOptions.SigningKey,
                    serverOptions.EncryptionKey);
            });
            services.TryAddSingleton<TokenVerifier>();
            services.TryAddSingleton<EntitlementStore>();
        }

        [SuppressMessage(
            "Redundancy", "RCS1163:Unused parameter.", Justification = "This method gets called by the ASP.NET runtime. ")]
        [SuppressMessage(
            "Redundancies in Symbol Declarations", "RECS0154:Parameter is never used", Justification = "This method gets called by the ASP.NET runtime. ")]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddProvider(_provider);
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
