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
		static Lazy<FreeSql.Template.TemplateEngin> _tpl = new Lazy<Template.TemplateEngin>(() => {
			_tplViewDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FreeSql.AdminLTE.Views/");
			if (Directory.Exists(_tplViewDir) == false) Directory.CreateDirectory(_tplViewDir);
			return new FreeSql.Template.TemplateEngin(_tplViewDir);
		});
		static ConcurrentDictionary<string, object> newTplLock = new ConcurrentDictionary<string, object>();
		static ConcurrentDictionary<string, bool> newTpl = new ConcurrentDictionary<string, bool>();

		static void MakeTemplateFile(string tplName, string tplCode) {
			var tplPath = _tplViewDir + $@"{tplName}";
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

				if (dicEntityTypes.TryGetValue(entityName, out var entityType) == false) throw new Exception($"UseFreeAdminLtePreview 错误，找不到实体类型：{entityName}");

				var tb = fsql.CodeFirst.GetTableByEntity(entityType);
				if (tb == null) throw new Exception($"UseFreeAdminLtePreview 错误，实体类型无法映射：{entityType.FullName}");

				var tpl = _tpl.Value;

				switch (remUrl.ElementAtOrDefault(1)?.ToLower()) {
					case null:
						//首页
						if (true) {
							MakeTemplateFile($"{entityName}-list.html", Views.List);

							//ManyToOne/OneToOne
							var getlistFilter = new List<(TableRef, string, string, Dictionary<string, object>, List<Dictionary<string, object>>)>();
							foreach (var prop in tb.Properties) {
								if (tb.ColumnsByCs.ContainsKey(prop.Key)) continue;
								var tbref = tb.GetTableRef(prop.Key, false);
								if (tbref == null) continue;
								switch (tbref.RefType) {
									case TableRefType.OneToMany: continue;
									case TableRefType.ManyToOne:
										getlistFilter.Add(await Utils.GetTableRefData(fsql, tbref));
										continue;
									case TableRefType.OneToOne:
										continue;
									case TableRefType.ManyToMany:
										getlistFilter.Add(await Utils.GetTableRefData(fsql, tbref));
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
											getselect.Where(Utils.GetObjectWhereExpressionContains(tb, entityType, getlistF.Item1.Columns[0].CsName, qv));
											continue;
										case TableRefType.OneToOne:
											continue;
										case TableRefType.ManyToMany:
											if (true) {
												var midType = getlistF.Item1.RefMiddleEntityType;
												var midTb = fsql.CodeFirst.GetTableByEntity(midType);
												var midISelect = typeof(ISelect<>).MakeGenericType(midType);

												var funcType = typeof(Func<,>).MakeGenericType(typeof(object), typeof(bool));
												var expParam = Expression.Parameter(typeof(object), "a");
												var midParam = Expression.Parameter(midType, "mdtp");

												var anyMethod = midISelect.GetMethod("Any");
												var selectExp = qv.Select(c => Expression.Convert(Expression.Constant(FreeSql.Internal.Utils.GetDataReaderValue(getlistF.Item1.MiddleColumns[1].CsType, c)), getlistF.Item1.MiddleColumns[1].CsType)).ToArray();
												var expLambad = Expression.Lambda<Func<object, bool>>(
													Expression.Call(
														Expression.Call(
															Expression.Call(
																Expression.Constant(fsql),
																typeof(IFreeSql).GetMethod("Select", new Type[0]).MakeGenericMethod(midType)
															),
															midISelect.GetMethod("Where", new[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(midType, typeof(bool))) }),
															Expression.Lambda(
																typeof(Func<,>).MakeGenericType(midType, typeof(bool)),
																Expression.AndAlso(
																	Expression.Equal(
																		Expression.MakeMemberAccess(Expression.TypeAs(expParam, entityType), tb.Properties[getlistF.Item1.Columns[0].CsName]),
																		Expression.MakeMemberAccess(midParam, midTb.Properties[getlistF.Item1.MiddleColumns[0].CsName])
																	),
																	Expression.Call(
																		Utils.GetLinqContains(getlistF.Item1.MiddleColumns[1].CsType),
																		Expression.NewArrayInit(
																			getlistF.Item1.MiddleColumns[1].CsType,
																			selectExp
																		),
																		Expression.MakeMemberAccess(midParam, midTb.Properties[getlistF.Item1.MiddleColumns[1].CsName])
																	)
																),
																midParam
															)
														),
														anyMethod,
														Expression.Default(anyMethod.GetParameters().FirstOrDefault().ParameterType)
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

							//ManyToOne/OneToOne
							var getlistFilter = new Dictionary<string, (TableRef, string, string, Dictionary<string, object>, List<Dictionary<string, object>>)>();
							var getlistManyed = new Dictionary<string, IEnumerable<string>>();
							foreach (var prop in tb.Properties) {
								if (tb.ColumnsByCs.ContainsKey(prop.Key)) continue;
								var tbref = tb.GetTableRef(prop.Key, false);
								if (tbref == null) continue;
								switch (tbref.RefType) {
									case TableRefType.OneToMany: continue;
									case TableRefType.ManyToOne:
										getlistFilter.Add(prop.Key, await Utils.GetTableRefData(fsql, tbref));
										continue;
									case TableRefType.OneToOne:
										continue;
									case TableRefType.ManyToMany:
										getlistFilter.Add(prop.Key, await Utils.GetTableRefData(fsql, tbref));

										if (getitem != null) {
											var midType = tbref.RefMiddleEntityType;
											var midTb = fsql.CodeFirst.GetTableByEntity(midType);
											var manyed = await fsql.Select<object>().AsType(midType)
												.Where(Utils.GetObjectWhereExpression(midTb, midType, tbref.MiddleColumns[0].CsName, fsql.GetEntityKeyValues(entityType, getitem)[0]))
												.ToListAsync();
											getlistManyed.Add(prop.Key, manyed.Select(a => fsql.GetEntityValueWithPropertyName(midType, a, tbref.MiddleColumns[1].CsName).ToString()));
										}
										continue;
								}
							}

							var options = new Dictionary<string, object>();
							options["tb"] = tb;
							options["gethash"] = gethash;
							options["getlistFilter"] = getlistFilter;
							options["getlistManyed"] = getlistManyed;
							options["postaction"] = $"{requestPathBase}restful-api/{entityName}";
							var str = _tpl.Value.RenderFile($"{entityName}-edit.html", options);
							await res.WriteAsync(str);

						} else {
							if (getitem == null) {
								getitem = Activator.CreateInstance(entityType);
								foreach(var getcol in tb.Columns.Values) {
									if (new[] { typeof(DateTime), typeof(DateTime?) }.Contains(getcol.CsType) && new[] { "create_time", "createtime" }.Contains(getcol.CsName.ToLower()))
										fsql.SetEntityValueWithPropertyName(entityType, getitem, getcol.CsName, DateTime.Now);
								}
							}
							var manySave = new List<(TableRef, object[], List<object>)>();
							if (req.Form.Any()) {
								foreach(var getcol in tb.Columns.Values) {
									if (new[] { typeof(DateTime), typeof(DateTime?) }.Contains(getcol.CsType) && new[] { "update_time", "updatetime" }.Contains(getcol.CsName.ToLower()))
										fsql.SetEntityValueWithPropertyName(entityType, getitem, getcol.CsName, DateTime.Now);

									var reqv = req.Form[getcol.CsName].ToArray();
									if (reqv.Any())
										fsql.SetEntityValueWithPropertyName(entityType, getitem, getcol.CsName, reqv.Length == 1 ? (object)reqv.FirstOrDefault() : reqv);
								}
								//ManyToMany
								foreach (var prop in tb.Properties) {
									if (tb.ColumnsByCs.ContainsKey(prop.Key)) continue;
									var tbref = tb.GetTableRef(prop.Key, false);
									if (tbref == null) continue;
									switch (tbref.RefType) {
										case TableRefType.OneToMany: continue;
										case TableRefType.ManyToOne:
											continue;
										case TableRefType.OneToOne:
											continue;
										case TableRefType.ManyToMany:
											var midType = tbref.RefMiddleEntityType;
											var mtb = fsql.CodeFirst.GetTableByEntity(midType);

											var reqv = req.Form[$"mn_{prop.Key}"].ToArray();
											var reqvIndex = 0;
											var manyVals = new object[reqv.Length];
											foreach (var rv in reqv) {
												var miditem = Activator.CreateInstance(midType);
												foreach (var getcol in tb.Columns.Values) {
													if (new[] { typeof(DateTime), typeof(DateTime?) }.Contains(getcol.CsType) && new[] { "create_time", "createtime" }.Contains(getcol.CsName.ToLower()))
														fsql.SetEntityValueWithPropertyName(midType, miditem, getcol.CsName, DateTime.Now);

													if (new[] { typeof(DateTime), typeof(DateTime?) }.Contains(getcol.CsType) && new[] { "update_time", "updatetime" }.Contains(getcol.CsName.ToLower()))
														fsql.SetEntityValueWithPropertyName(midType, miditem, getcol.CsName, DateTime.Now);
												}
												//fsql.SetEntityValueWithPropertyName(midType, miditem, tbref.MiddleColumns[0].CsName, fsql.GetEntityKeyValues(entityType, getitem)[0]);
												fsql.SetEntityValueWithPropertyName(midType, miditem, tbref.MiddleColumns[1].CsName, rv);
												manyVals[reqvIndex++] = miditem;
											}
											var molds = await fsql.Select<object>().AsType(midType).Where(Utils.GetObjectWhereExpression(mtb, midType, tbref.MiddleColumns[0].CsName, fsql.GetEntityKeyValues(entityType, getitem)[0])).ToListAsync();
											manySave.Add((tbref, manyVals, molds));
											continue;
									}
								}
							}


							using (var db = fsql.CreateDbContext()) {

								var dbset = db.Set<object>();
								dbset.AsType(entityType);

								await dbset.AddOrUpdateAsync(getitem);

								foreach (var ms in manySave) {
									var midType = ms.Item1.RefMiddleEntityType;
									var moldsDic = ms.Item3.ToDictionary(a => fsql.GetEntityKeyString(midType, a, true));

									var manyset = db.Set<object>();
									manyset.AsType(midType);
									
									foreach (var msVal in ms.Item2) {
										fsql.SetEntityValueWithPropertyName(midType, msVal, ms.Item1.MiddleColumns[0].CsName, fsql.GetEntityKeyValues(entityType, getitem)[0]);
										await manyset.AddOrUpdateAsync(msVal);
										moldsDic.Remove(fsql.GetEntityKeyString(midType, msVal, true));
									}
									manyset.RemoveRange(moldsDic.Values);
								}

								await db.SaveChangesAsync();
							}
							gethash = new Dictionary<string, object>();
							foreach (var getcol in tb.ColumnsByCs) {
								gethash.Add(getcol.Key, tb.Properties[getcol.Key].GetValue(getitem));
							}

							await Utils.Jsonp(context, new { code = 0, success = true, message = "Success", data = gethash });
						}
						return true;
					case "del":
						if (req.Method.ToLower() == "post") {

							var delitems = new List<object>();
							var reqv = new List<string[]>();
							foreach(var delpk in tb.Primarys) {
								var reqvs = req.Form[delpk.CsName].ToArray();
								if (reqv.Count > 0 && reqvs.Length != reqv[0].Length) throw new Exception("删除失败，联合主键参数传递不对等");
								reqv.Add(reqvs);
							}
							if (reqv[0].Length == 0) return true;

							for (var a = 0; a < reqv[0].Length; a++)
							{
								object delitem = Activator.CreateInstance(entityType);
								var delpkindex = 0;
								foreach (var delpk in tb.Primarys)
									fsql.SetEntityValueWithPropertyName(entityType, delitem, delpk.CsName, reqv[delpkindex++][a]);
								delitems.Add(delitem);
							}
							using (var ctx = fsql.CreateDbContext()) {
								var dbset = ctx.Set<object>();
								dbset.AsType(entityType);

								//await dbset.RemoveCascadeByDatabaseAsync()
								dbset.RemoveRange(delitems);
								await ctx.SaveChangesAsync();
							}

							await Utils.Jsonp(context, new { code = 0, success = true, message = "Success" });
							return true;
						}
						break;
				}
			}
			return false;
		}
	}
}
