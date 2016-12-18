using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using ValidationPipeline.LogStorage.FileProviders;
using ValidationPipeline.LogStorage.Models;
using ValidationPipeline.LogStorage.Services;

namespace ValidationPipeline.LogStorage
{
    public class Startup
    {
        private const string StaticFilesCacheMaxAgeKey = "StaticFiles:CacheMaxAgeSeconds";
        private const string ControllerCacheDurationKey = "StaticFiles:CacheDurationMinutes";

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
                .Configure<BlobStorageOptions>(Configuration.GetSection("BlobStorage"));

            services.TryAddTransient<IArchiveService, ArchiveService>();
            services.TryAddTransient<IStorageService, StorageService>();

            services.TryAddTransient<IFileProvider, LogStorageFileProvider>();
        }

        private IMvcBuilder AddMvcWithCaching(IServiceCollection services)
        {
            return services.AddMvc(options =>
            {
                options.CacheProfiles.Add("Default",
                    new CacheProfile
                    {
                        Duration = Configuration.GetValue<int>(ControllerCacheDurationKey)
                    });
                options.CacheProfiles.Add("Never",
                    new CacheProfile
                    {
                        Location = ResponseCacheLocation.None,
                        NoStore = true
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseMvc()
                .UseStaticFiles(GetStaticFileOptions(app));
        }

        private StaticFileOptions GetStaticFileOptions(IApplicationBuilder app)
        {
            var cacheMaxAge = Configuration.GetValue<long>(StaticFilesCacheMaxAgeKey);

            return new StaticFileOptions
            {
                FileProvider = app.ApplicationServices.GetService<IFileProvider>(),

                // TODO: This is a security risk!!!
                // Replace this with FileExtensionContentTypeProvider 
                // once inner file extensions are known
                ServeUnknownFileTypes = true,

                RequestPath = new PathString(CommonConstants.StaticFilesPath),
                OnPrepareResponse = responseContext =>
                {
                    responseContext.Context.Response.Headers[HeaderNames.CacheControl] =
                        $"public,max-age={cacheMaxAge}";
                }
            };
        }
    }
}
