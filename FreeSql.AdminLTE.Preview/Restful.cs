using FreeSql.Extensions.EntityUtil;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql {
	class Restful {

		async public static Task<bool> Use(HttpContext context, IFreeSql fsql, string restfulRequestPath, Dictionary<string, Type> dicEntityTypes) {
			HttpRequest req = context.Request;
			HttpResponse res = context.Response;

			var remUrl = req.Path.ToString().Substring(restfulRequestPath.Length).Trim(' ', '/').Split('/');
			var entityName = remUrl.FirstOrDefault();

			if (!string.IsNullOrEmpty(entityName)) {

				if (dicEntityTypes.TryGetValue(entityName, out var entityType) == false) throw new Exception($"UseFreeAdminLtePreview 错误，找不到实体类型：{entityName}");

				var tb = fsql.CodeFirst.GetTableByEntity(entityType);
				if (tb == null) throw new Exception($"UseFreeAdminLtePreview 错误，实体类型无法映射：{entityType.FullName}");

				using (var db = new FreeContext(fsql)) {
					var dbset = db.Set<object>();
					dbset.AsType(entityType);

					switch (req.Method.ToLower()) {
						case "get":
							if (remUrl.Length == 1) {
								int.TryParse(req.Query["page"].FirstOrDefault(), out var getpage);
								int.TryParse(req.Query["limit"].FirstOrDefault(), out var getlimit);
								if (getpage <= 0) getpage = 1;
								if (getlimit <= 0) getlimit = 20;
								long getlistTotal = 0;

								var getselect = fsql.Select<object>().AsType(entityType);
								if (getpage == 1) getlistTotal = await getselect.CountAsync();
								var getlist = await getselect.Page(getpage, getlimit).ToListAsync();
								var gethashlists = new Dictionary<string, object>[getlist.Count];
								var gethashlistsIndex = 0;
								foreach (var getlistitem in getlist) {
									var gethashlist = new Dictionary<string, object>();
									foreach (var getcol in tb.ColumnsByCs) {
										gethashlist.Add(getcol.Key, tb.Properties[getcol.Key].GetValue(getlistitem));
									}
									gethashlists[gethashlistsIndex++] = gethashlist;
								}
								if (getpage == 1) {
									await Utils.Jsonp(context, new { code = 0, message = "Success", data = gethashlists, totalCount = getlistTotal });
									return true;
								}
								await Utils.Jsonp(context, new { code = 0, message = "Success", data = gethashlists });
								return true;
							}
							if (remUrl.Length - 1 != tb.Primarys.Length) throw new Exception($"UseFreeAdminLtePreview 查询，参数数目与主键不匹配，应该相同");
							var getitem = Activator.CreateInstance(entityType);
							var getindex = 1;
							foreach (var getpk in tb.Primarys)
								fsql.SetEntityValueWithPropertyName(entityType, getitem, getpk.CsName, remUrl[getindex++]);
							getitem = await fsql.Select<object>().AsType(entityType).WhereDynamic(getitem).FirstAsync();
							var gethash = new Dictionary<string, object>();
							foreach (var getcol in tb.ColumnsByCs) {
								gethash.Add(getcol.Key, tb.Properties[getcol.Key].GetValue(getitem));
							}
							await Utils.Jsonp(context, new { code = 0, message = "Success", data = gethash });
							return true;
						case "post":
							var postbodyraw = await Utils.GetBodyRawText(req);
							var postjsonitem = Newtonsoft.Json.JsonConvert.DeserializeObject(postbodyraw, entityType);
							await dbset.AddAsync(postjsonitem);
							await db.SaveChangesAsync();
							await Utils.Jsonp(context, new { code = 0, message = "Success", data = postjsonitem });
							return true;
						case "put":
							if (remUrl.Length - 1 != tb.Primarys.Length) throw new Exception($"UseFreeAdminLtePreview 更新错误，参数数目与主键不匹配，应该相同");
							var putbodyraw = await Utils.GetBodyRawText(req);
							var putjsonitem = Newtonsoft.Json.JsonConvert.DeserializeObject(putbodyraw, entityType);
							var putindex = 1;
							foreach (var putpk in tb.Primarys)
								fsql.SetEntityValueWithPropertyName(entityType, putjsonitem, putpk.CsName, remUrl[putindex++]);
							await dbset.UpdateAsync(putjsonitem);
							await db.SaveChangesAsync();
							await Utils.Jsonp(context, new { code = 0, message = "Success" });
							return true;
						case "delete":
							if (remUrl.Length - 1 != tb.Primarys.Length) throw new Exception($"UseFreeAdminLtePreview 删除错误，参数数目与主键不匹配，应该相同");
							var delitem = Activator.CreateInstance(entityType);
							var delindex = 1;
							foreach (var delpk in tb.Primarys)
								fsql.SetEntityValueWithPropertyName(entityType, delitem, delpk.CsName, remUrl[delindex++]);
							dbset.Remove(delitem);
							await db.SaveChangesAsync();
							await Utils.Jsonp(context, new { code = 0, message = "Success" });
							return true;
					}
				}
			}
			return false;
		}
	}
}
