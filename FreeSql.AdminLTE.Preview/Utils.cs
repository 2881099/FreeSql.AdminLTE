using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using FreeSql.Internal.Model;
using System.Linq.Expressions;

namespace FreeSql {
	static class Utils {

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

		static ConcurrentDictionary<Type, MethodInfo[]> _dicMethods = new ConcurrentDictionary<Type, MethodInfo[]>();
		static ConcurrentDictionary<Type, MethodInfo> _dicGetLinqContains = new ConcurrentDictionary<Type, MethodInfo>();
		public static MethodInfo GetLinqContains(Type genericType) {
			return _dicGetLinqContains.GetOrAdd(genericType, gt => {
				var methods = _dicMethods.GetOrAdd(typeof(Enumerable), gt2 => gt2.GetMethods());
				var method = methods.Where(a => a.Name == "Contains" && a.GetParameters().Length == 2).FirstOrDefault();
				return method?.MakeGenericMethod(gt);
			});
		}

		async public static Task<(TableRef, string, string, Dictionary<string, object>, List<Dictionary<string, object>>)> GetTableRefData(IFreeSql fsql, TableRef tbref) {
			var reftb = fsql.CodeFirst.GetTableByEntity(tbref.RefEntityType);
			var reflist = await fsql.Select<object>().AsType(tbref.RefEntityType).ToListAsync();
			var fitlerTextCol = reftb.ColumnsByCs.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault();
			var filterTextProp = fitlerTextCol == null ? null : reftb.Properties[fitlerTextCol.CsName];
			var filterValueProp = reftb.Properties[reftb.Primarys.FirstOrDefault().CsName];
			var filterKv = new Dictionary<string, object>();
			var gethashlist = new List<Dictionary<string, object>>();
			foreach (var refitem in reflist) {
				filterKv[string.Concat(filterValueProp.GetValue(refitem))] = filterTextProp == null ? refitem : filterTextProp.GetValue(refitem);
				var gethash = new Dictionary<string, object>();
				foreach (var getcol in reftb.ColumnsByCs) {
					gethash.Add(getcol.Key, reftb.Properties[getcol.Key].GetValue(refitem));
				}
				gethashlist.Add(gethash);
			}
			return (
				tbref,
				tbref.RefEntityType.Name,
				tbref.RefEntityType.Name + "_" + reftb.Primarys.FirstOrDefault().CsName,
				filterKv,
				gethashlist
			);
		}

		public static Expression<Func<object, bool>> GetObjectWhereExpression(TableInfo tb, Type entityType, string csName, object constValue) {
			var mwhereParamExp = Expression.Parameter(typeof(object), "a");
			var mwhereExp = Expression.Lambda<Func<object, bool>>(
				Expression.Equal(
					Expression.MakeMemberAccess(
						Expression.TypeAs(mwhereParamExp, entityType),
						tb.Properties[csName]
					),
					Expression.Constant(constValue)
				),
				mwhereParamExp
			);
			return mwhereExp;
		}
		public static Expression<Func<object, bool>> GetObjectWhereExpressionContains(TableInfo tb, Type entityType, string csName, object[] arrayValue) {
			var mwhereParamExp = Expression.Parameter(typeof(object), "a");
			var selectExp = arrayValue.Select(c => Expression.Convert(Expression.Constant(FreeSql.Internal.Utils.GetDataReaderValue(tb.Columns[csName].CsType, c)), tb.Columns[csName].CsType)).ToArray();
			var mwhereExp = Expression.Lambda<Func<object, bool>>(
				Expression.Call(
					Utils.GetLinqContains(tb.Columns[csName].CsType),
					Expression.NewArrayInit(
						tb.Columns[csName].CsType,
						selectExp
					),
					Expression.MakeMemberAccess(Expression.TypeAs(mwhereParamExp, entityType), tb.Properties[csName])
				),
				mwhereParamExp);
			return mwhereExp;
		}
	}
}
