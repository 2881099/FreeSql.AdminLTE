using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql {
	internal static class Utils {

		async public static Task<string> GetBodyRawText(HttpRequest req) {
			var charsetIndex = req.ContentType.IndexOf("charset=");
			var charset = Encoding.UTF8;
			if (charsetIndex != -1) {
				var charsetText = req.ContentType.Substring(charsetIndex + 8);
				charsetIndex = charsetText.IndexOf(';');
				if (charsetIndex != -1) charsetText = charsetText.Remove(charsetIndex);
				switch (charsetText.ToLower()) {
					case "utf8":
					case "utf-8":
						break;
					default:
						charset = Encoding.GetEncoding(charsetText);
						break;
				}
			}
			req.Body.Position = 0;
			using (var ms = new MemoryStream()) {
				await req.Body.CopyToAsync(ms);
				return charset.GetString(ms.ToArray());
			}
		}

		public static Task Jsonp(HttpContext context, object options) {
			string __callback = context.Request.HasFormContentType ? context.Request.Form["__callback"].ToString() : null;
			if (string.IsNullOrEmpty(__callback)) {
				context.Response.ContentType = "text/json;charset=utf-8;";
				return context.Response.WriteAsync(JsonConvert.SerializeObject(options));
			} else {
				context.Response.ContentType = "text/html;charset=utf-8";
				return context.Response.WriteAsync($"<script>top.{__callback}({JsonConvert.SerializeObject(options)});</script>");
			}
		}
	}
}
