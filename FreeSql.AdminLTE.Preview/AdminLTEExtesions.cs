using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text;

namespace FreeSql
{
    public static class AdminLteExtesions
    {

		public static IApplicationBuilder UseFreeAdminLtePreview(this IApplicationBuilder app, string requestPathBase, params Type[] entityTypes) {

			requestPathBase = requestPathBase.ToLower();
			if (requestPathBase.StartsWith("/") == false) requestPathBase = $"/{requestPathBase}";
			if (requestPathBase.EndsWith("/") == false) requestPathBase = $"{requestPathBase}/";
			var restfulRequestPath = $"{requestPathBase}restful-api";

			IFreeSql fsql = app.ApplicationServices.GetService(typeof(IFreeSql)) as IFreeSql;
			if (fsql == null) throw new Exception($"UseFreeAdminLtePreview 错误，找不到 IFreeSql，请提前注入");

			var dicEntityTypes = entityTypes.ToDictionary(a => a.Name);

			app.UseFreeAdminLteStaticFiles(requestPathBase);

			app.Use(async (context, next) => {

				var req = context.Request;
				var res = context.Response;
				var location = req.Path.Value;
				var is301 = false;

				if (location.EndsWith("/") == false) {
					is301 = true;
					location = $"{location}/";
				}

				var reqPath = location.ToLower();
				try {
					if (reqPath == requestPathBase) {
						if (is301) {
							res.StatusCode = 301;
							res.Headers["Location"] = location;
							return;
						}
						//首页
						var sb = new StringBuilder();
						sb.AppendLine(@"<ul class=""treeview-menu"">");
						foreach (var et in dicEntityTypes) {
							sb.AppendLine($@"<li><a href=""{requestPathBase}{et.Key}/""><i class=""fa fa-circle-o""></i>{fsql.CodeFirst.GetTableByEntity(et.Value).Comment.IsNullOrEmtpty(et.Key)}</a></li>");
						}
						sb.AppendLine(@"</ul>");
						await res.WriteAsync(Views.Index.Replace(@"<ul class=""treeview-menu""></ul>", sb.ToString()));
						return;
					}
					else if (reqPath.StartsWith(restfulRequestPath)) {
						//动态接口
						if (await Restful.Use(context, fsql, restfulRequestPath, dicEntityTypes)) return;
					}
					else if (reqPath.StartsWith(requestPathBase)) {
						//前端UI
						if (await Admin.Use(context, fsql, requestPathBase, dicEntityTypes)) return;
					}

				} catch (Exception ex) {
					await Utils.Jsonp(context, new { code = 500, message = ex.Message });
					return;
				}
				await next();
			});

			return app;
		}

		public static string IsNullOrEmtpty(this string that, string newvalue) => string.IsNullOrEmpty(that) ? newvalue : that;
	}
}
