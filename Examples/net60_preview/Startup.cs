using FreeSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using net60_preview.Entitys;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace net60_preview
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;

            Fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.Sqlite, @"Data Source=|DataDirectory|/document.db;Pooling=true;Max Pool Size=10")
                .UseAutoSyncStructure(true)
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

            app.UseDefaultFiles();
            app.UseStaticFiles();

            //可以配置子目录访问，如：/testadmin/
            app.UseFreeAdminLtePreview("/",
                typeof(Song),
                typeof(Tag),
                typeof(User),
                typeof(UserImage)
            );
        }

        void TestRoute()
        {
            var controllers = typeof(Startup).Assembly.GetTypes().Where(a => typeof(Controller).IsAssignableFrom(a)).ToArray();
            var routes = controllers.Select(a =>
            {
                var tb = Fsql.CodeFirst.GetTableByEntity(a);
                var name = string.IsNullOrWhiteSpace(tb.Comment) ? a.Name : tb.Comment;
                var controller = a.Name.EndsWith("Controller") ? a.Name.Remove(a.Name.Length - 10) : a.Name;
                var path = a.GetCustomAttribute<RouteAttribute>()?.Template.Replace("[controller]", controller) ?? $"/controller";
                var route = new AdminRoute
                {
                    Name = name,
                    Path = path,
                    Create_time = DateTime.Now,
                    Extdata = JsonConvert.SerializeObject(new { icon = "org_tree_page", path = "/authuser", name = "sysadmin_authuser", component = "@/view/sysadmin/authuser-page.vue" }),
                    Childs = a.GetMethods().Select(m =>
                    {
                        HttpMethodAttribute httpmethod = m.GetCustomAttribute<HttpGetAttribute>();
                        if (httpmethod == null) httpmethod = m.GetCustomAttribute<HttpPostAttribute>();
                        if (httpmethod == null) httpmethod = m.GetCustomAttribute<HttpPutAttribute>();
                        if (httpmethod == null) httpmethod = m.GetCustomAttribute<HttpDeleteAttribute>();
                        if (httpmethod != null) return new AdminRoute
                        {
                            Name = LocalGetMethodName(httpmethod.Name),
                            Path = $"{httpmethod.HttpMethods.FirstOrDefault()} {path}/{httpmethod.Template}",
                            Create_time = DateTime.Now,
                        };
                        return null;
                    }).Where(a => a != null).ToList()
                };
                return route;
            }).ToList();

            string LocalGetMethodName(string name)
            {
                switch (name)
                {
                    case "": return "列表";
                    case "add": return "添加";
                    case "edit": return "编辑";
                    case "del": return "添加";
                }
                return name;
            }
        }
    }

    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
