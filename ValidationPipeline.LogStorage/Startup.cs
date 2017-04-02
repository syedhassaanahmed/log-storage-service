using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using Swashbuckle.Swagger.Model;
using ValidationPipeline.LogStorage.FileProviders;
using ValidationPipeline.LogStorage.Middlewares;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage
{
    public class Startup
    {
        private const string BlobStorageKey = "BlobStorage";
        private const string CdnKey = "Cdn";
        private const string BasicAuthenticationKey = "BasicAuthentication";
        private const string StaticFilesCacheMaxAgeKey = "StaticFiles:CacheMaxAgeSeconds";
        private const string ControllerCacheDurationKey = "Controller:CacheDurationMinutes";

        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            AddMvcWithCaching(services)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = 
                        new CamelCasePropertyNamesContractResolver();
                });

            services.AddOptions()
                .Configure<BlobStorageOptions>(Configuration.GetSection(BlobStorageKey))
                .Configure<CdnOptions>(Configuration.GetSection(CdnKey))
                .Configure<BasicAuthenticationOptions>(Configuration.GetSection(BasicAuthenticationKey));

            services.TryAddTransient<IArchiveService, ArchiveService>();
            services.TryAddTransient<IStorageService, StorageService>();

            services.TryAddTransient<IFileProvider, LogStorageFileProvider>();

            ConfigureSwagger(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            //Order is important here
            app.UseBasicAuthentication()
                .UseStaticFiles(GetStaticFileOptions(app))
                .UseMvc()
                .UseSwagger() // Enable middleware to serve generated Swagger as a JSON endpoint
                .UseSwaggerUi(); // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
        }

        #region Helpers

        private IMvcBuilder AddMvcWithCaching(IServiceCollection services)
        {
            return services.AddMvc(options =>
            {
                options.CacheProfiles.Add("Default",
                    new CacheProfile
                    {
                        Duration = Configuration.GetValue(ControllerCacheDurationKey, 30)
                    });
                options.CacheProfiles.Add("Never",
                    new CacheProfile
                    {
                        Location = ResponseCacheLocation.None,
                        NoStore = true
                    });
            });
        }

        private StaticFileOptions GetStaticFileOptions(IApplicationBuilder app)
        {
            var cacheMaxAge = Configuration.GetValue<long>(StaticFilesCacheMaxAgeKey, 3600);

            return new StaticFileOptions
            {
                FileProvider = app.ApplicationServices.GetService<IFileProvider>(),

                // TODO: Enabling ServeUnknownFileTypes is a security risk and using it is discouraged. 
                // FileExtensionContentTypeProvider provides a safer alternative to serving files 
                // with non-standard extensions.
                ServeUnknownFileTypes = true,

                RequestPath = new PathString(CommonConstants.StaticFilesPath),
                OnPrepareResponse = responseContext =>
                {
                    responseContext.Context.Response.Headers[HeaderNames.CacheControl] =
                        $"public,max-age={cacheMaxAge}";
                }
            };
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            // Inject an implementation of ISwaggerProvider with defaulted settings applied
            services.AddSwaggerGen();

            services.ConfigureSwaggerGen(options =>
            {
                options.SingleApiVersion(GetSwaggerInfo());

                var basePath = PlatformServices.Default.Application.ApplicationBasePath;

                //Set the comments path for the swagger json and ui.
                var xmlPath = Path.Combine(basePath, "ValidationPipeline.LogStorage.xml");
                options.IncludeXmlComments(xmlPath);
            });
        }

        private static Info GetSwaggerInfo()
        {
            return new Info
            {
                Version = "v1",
                Title = "Log Storage Service",
                Description = "Simple REST API which ingests zipped log files, stores them and exposes unzipped content through API call",
                TermsOfService = "None",
                Contact = new Contact
                {
                    Name = "Syed Hassaan Ahmed",
                    Email = "hassaan.ahmed@notmyemail.com",
                    Url = "https://github.com/syedhassaanahmed/log-storage-service"
                },
                License = new License
                {
                    Name = "Use under MIT",
                    Url = "https://raw.githubusercontent.com/syedhassaanahmed/log-storage-service/develop/LICENSE"
                }
            };
        }

        #endregion
    }
}
