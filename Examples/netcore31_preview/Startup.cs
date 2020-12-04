using FreeSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using netcore31_preview.Entitys;
using System;
using System.Diagnostics;

namespace TestDemo01
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;

            Fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.Sqlite, @"Data Source=|DataDirectory|/document.db;Pooling=true;Max Pool Size=10")
                .UseAutoSyncStructure(true)
                .UseLazyLoading(true)
                .UseMonitorCommand(cmd => Console.WriteLine(cmd.CommandText + "\r\n"))
                .Build();
        }

        public IConfiguration Configuration { get; }
        public static IFreeSql Fsql { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSingleton(Fsql);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpMethodOverride(new HttpMethodOverrideOptions { FormFieldName = "X-Http-Method-Override" });
            app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseEndpoints(a => a.MapControllers());

            //可以配置子目录访问，如：/testadmin/
            app.UseFreeAdminLtePreview("/",
                typeof(Song),
                typeof(Tag),
                typeof(User),
                typeof(UserImage)
            );
        }
    }

    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
