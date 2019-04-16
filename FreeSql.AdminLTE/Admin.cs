using FreeSql.Extensions.EntityUtil;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FreeSql.Internal.Model;
using System.Linq.Expressions;

namespace FreeSql {
	static class Admin {

		static string _tplViewDir;
		static Lazy<FreeSql.Generator.TemplateEngin> _tpl = new Lazy<Generator.TemplateEngin>(() => {
			_tplViewDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FreeSql.AdminLTE.Views/");
			if (Directory.Exists(_tplViewDir) == false) Directory.CreateDirectory(_tplViewDir);
			return new FreeSql.Generator.TemplateEngin(_tplViewDir);
		});
		static ConcurrentDictionary<string, object> newTplLock = new ConcurrentDictionary<string, object>();
		static ConcurrentDictionary<string, bool> newTpl = new ConcurrentDictionary<string, bool>();

		static void MakeTemplateFile(string tplName, string tplCode) {
			var tplPath = _tplViewDir + $@"\{tplName}";
			if (newTpl.ContainsKey(tplPath) == false) {
				var lck = newTplLock.GetOrAdd(tplPath, ent => new object());
				lock (lck) {
					if (newTpl.ContainsKey(tplPath) == false) {
						if (File.Exists(tplPath)) File.Delete(tplPath);
						File.WriteAllText(tplPath, tplCode, Encoding.UTF8);
						newTpl.TryAdd(tplPath, true);
					}
				}
			}
		}

		async public static Task<bool> Use(HttpContext context, IFreeSql fsql, string requestPathBase, Dictionary<string, Type> dicEntityTypes) {
			HttpRequest req = context.Request;
			HttpResponse res = context.Response;

			var remUrl = req.Path.ToString().Substring(requestPathBase.Length).Trim(' ', '/').Split('/');
			var entityName = remUrl.FirstOrDefault();

			if (!string.IsNullOrEmpty(entityName)) {

				if (dicEntityTypes.TryGetValue(entityName, out var entityType) == false) throw new Exception($"UseFreeAdminLTE 错误，找不到实体类型：{entityName}");

				var tb = fsql.CodeFirst.GetTableByEntity(entityType);
				if (tb == null) throw new Exception($"UseFreeAdminLTE 错误，实体类型无法映射：{entityType.FullName}");

				var tpl = _tpl.Value;

				switch (remUrl.ElementAtOrDefault(1)?.ToLower()) {
					case null:
						//首页
						if (true) {
							MakeTemplateFile($"{entityName}-list.html", Views.List);

							//ManyToOne/OneToOne
							var getlistFilter = new List<(TableRef, string, string, string, string)>();
							foreach (var prop in tb.Properties) {
								if (tb.ColumnsByCs.ContainsKey(prop.Key)) continue;
								var tbref = tb.GetTableRef(prop.Key, false);
								if (tbref == null) continue;
								switch (tbref.RefType) {
									case TableRefType.OneToMany: continue;
									case TableRefType.ManyToOne:
										if (true) {
											var reftb = fsql.CodeFirst.GetTableByEntity(tbref.RefEntityType);
											var reflist = await fsql.Select<object>().AsType(tbref.RefEntityType).ToListAsync();
											var fitlerTextCol = reftb.ColumnsByCs.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault();
											var filterTextProp = fitlerTextCol == null ? null : reftb.Properties[fitlerTextCol.CsName];
											var filterText = new List<object>();
											var filterValue = new List<object>();
											var filterValueProp = reftb.Properties[reftb.Primarys.FirstOrDefault().CsName];
											foreach (var refitem in reflist) {
												filterText.Add(filterTextProp == null ? refitem : filterTextProp.GetValue(refitem));
												filterValue.Add(filterValueProp.GetValue(refitem));
											}
											//DateTimeOffset
											//new[] {typeof(string)}.Contains()
											getlistFilter.Add((
												tbref,
												tbref.RefEntityType.Name,
												tbref.RefEntityType.Name + "_" + reftb.Primarys.FirstOrDefault().CsName,
												JsonConvert.SerializeObject(filterText),
												JsonConvert.SerializeObject(filterValue)
											));
										}
										continue;
									case TableRefType.OneToOne:
										continue;
									case TableRefType.ManyToMany:
										if (true) {
											var reftb = fsql.CodeFirst.GetTableByEntity(tbref.RefEntityType);
											var reflist = await fsql.Select<object>().AsType(tbref.RefEntityType).ToListAsync();
											var fitlerTextCol = reftb.ColumnsByCs.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault();
											var filterTextProp = fitlerTextCol == null ? null : reftb.Properties[fitlerTextCol.CsName];
											var filterText = new List<object>();
											var filterValue = new List<object>();
											var filterValueProp = reftb.Properties[reftb.Primarys.FirstOrDefault().CsName];
											foreach (var refitem in reflist) {
												filterText.Add(filterTextProp == null ? refitem : filterTextProp.GetValue(refitem));
												filterValue.Add(filterValueProp.GetValue(refitem));
											}
											getlistFilter.Add((
												tbref,
												tbref.RefEntityType.Name,
												tbref.RefEntityType.Name + "_" + reftb.Primarys.FirstOrDefault().CsName,
												JsonConvert.SerializeObject(filterText),
												JsonConvert.SerializeObject(filterValue)
											));
										}
										continue;
								}
							}

							int.TryParse(req.Query["page"].FirstOrDefault(), out var getpage);
							int.TryParse(req.Query["limit"].FirstOrDefault(), out var getlimit);
							if (getpage <= 0) getpage = 1;
							if (getlimit <= 0) getlimit = 20;

							var getselect = fsql.Select<object>().AsType(entityType);
							foreach (var getlistF in getlistFilter) {
								var qv = req.Query[getlistF.Item3].ToArray();
								if (qv.Any()) {
									switch (getlistF.Item1.RefType) {
										case TableRefType.OneToMany: continue;
										case TableRefType.ManyToOne:
											if (true) {
												var funcType = typeof(Func<,>).MakeGenericType(typeof(object), typeof(bool));
												var expParam = Expression.Parameter(entityType);

												var expLambad = Expression.Lambda<Func<object, bool>>(
													Expression.Call(
														Expression.NewArrayInit(getlistF.Item1.Columns[0].CsType, qv.Select(c => Expression.Constant(c))),
														getlistF.Item1.Columns[0].CsType.MakeArrayType(1).GetMethod("Contains"),
														Expression.MakeMemberAccess(expParam, tb.Properties[getlistF.Item1.Columns[0].CsName])
													),
													expParam);

												getselect.Where(expLambad);
											}
											continue;
										case TableRefType.OneToOne:
											continue;
										case TableRefType.ManyToMany:
											if (true) {
												var funcType = typeof(Func<,>).MakeGenericType(typeof(object), typeof(bool));
												var expParam = Expression.Parameter(entityType);

												var expLambad = Expression.Lambda<Func<object, bool>>(
													Expression.Call(
														Expression.NewArrayInit(getlistF.Item1.Columns[0].CsType, qv.Select(c => Expression.Constant(c))),
														getlistF.Item1.Columns[0].CsType.MakeArrayType(1).GetMethod("Contains"),
														Expression.MakeMemberAccess(Expression.TypeAs(expParam, entityType), tb.Properties[getlistF.Item1.Columns[0].CsName])
													),
													expParam);

												getselect.Where(expLambad);
											}
											continue;
									}
								}
							}

							var getlistTotal = await getselect.CountAsync();
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

							var options = new Dictionary<string, object>();
							options["tb"] = tb;
							options["getlist"] = gethashlists;
							options["getlistTotal"] = getlistTotal;
							options["getlistFilter"] = getlistFilter;
							var str = _tpl.Value.RenderFile($"{entityName}-list.html", options);
							await res.WriteAsync(str);
						}
						return true;
					case "add":
					case "edit":
						//编辑页
						object getitem = null;
						Dictionary<string, object> gethash = null;
						if (req.Query.Any()) {
							getitem = Activator.CreateInstance(entityType);
							foreach (var getpk in tb.Primarys) {
								var reqv = req.Query[getpk.CsName].ToArray();
								if (reqv.Any())
									fsql.SetEntityValueWithPropertyName(entityType, getitem, getpk.CsName, reqv.Length == 1 ? (object)reqv.FirstOrDefault() : reqv);
							}
							getitem = await fsql.Select<object>().AsType(entityType).WhereDynamic(getitem).FirstAsync();
							if (getitem != null) {
								gethash = new Dictionary<string, object>();
								foreach (var getcol in tb.ColumnsByCs) {
									gethash.Add(getcol.Key, tb.Properties[getcol.Key].GetValue(getitem));
								}
							}
						}

						if (req.Method.ToLower() == "get") {
							MakeTemplateFile($"{entityName}-edit.html", Views.Edit);

							var options = new Dictionary<string, object>();
							options["tb"] = tb;
							options["gethash"] = gethash;
							options["postaction"] = $"{requestPathBase}restful-api/{entityName}";
							var str = _tpl.Value.RenderFile($"{entityName}-edit.html", options);
							await res.WriteAsync(str);

						} else {
							if (getitem == null) getitem = Activator.CreateInstance(entityType);
							if (req.Form.Any()) {
								foreach(var getcol in tb.Columns.Values) {
									var reqv = req.Form[getcol.CsName].ToArray();
									if (reqv.Any())
										fsql.SetEntityValueWithPropertyName(entityType, getitem, getcol.CsName, reqv.Length == 1 ? (object)reqv.FirstOrDefault() : reqv);
								}
							}

							using (var db = fsql.CreateDbContext()) {
								var dbset = db.Set<object>();
								dbset.AsType(entityType);

								await dbset.AddOrUpdateAsync(getitem);
								await db.SaveChangesAsync();
							}
							gethash = new Dictionary<string, object>();
							foreach (var getcol in tb.ColumnsByCs) {
								gethash.Add(getcol.Key, tb.Properties[getcol.Key].GetValue(getitem));
							}

							await Utils.Jsonp(context, new { code = 0, success = true, message = "Success", data = gethash });
						}
						return true;
				}
			}
			return false;
		}
	}
}
