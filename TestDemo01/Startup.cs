using FreeSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

                .UseMonitorCommand(cmd => Trace.WriteLine(cmd.CommandText))
                .Build();


        }

        public IConfiguration Configuration { get; }
        public static IFreeSql Fsql { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMvc();
            services.AddSingleton<IFreeSql>(Fsql);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseHttpMethodOverride(new HttpMethodOverrideOptions { FormFieldName = "X-Http-Method-Override" });
            app.UseDeveloperExceptionPage();
            app.UseMvc();

            //可以配置子目录访问，如：/testadmin/
            app.UseFreeAdminLTE("/",
                typeof(TestDemo01.Entitys.Song),
                typeof(TestDemo01.Entitys.Tag),
                typeof(TestDemo01.Entitys.User),
                typeof(TestDemo01.Entitys.UserImage));

            var gen = new FreeSql.AdminLTE.Generator(new FreeSql.AdminLTE.GeneratorOptions
            {
                ControllerBase = "Controller",
                NameSpace = "FreeSql.AdminLTE",
                ControllerRouteBase = "/adminlte/"
            });
            gen.WriteFullCsproj(@"C:\Users\28810\Desktop\test", new[] {
                typeof(TestDemo01.Entitys.Song),
                typeof(TestDemo01.Entitys.Tag),
                typeof(TestDemo01.Entitys.User),
                typeof(TestDemo01.Entitys.UserImage)
            });

            var controller = gen.GetControllerCode(typeof(TestDemo01.Entitys.Song));
        }
    }

    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
