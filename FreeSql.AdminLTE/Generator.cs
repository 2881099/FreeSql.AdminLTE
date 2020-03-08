using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace FreeSql.AdminLTE
{
    public class Generator : IDisposable
    {
        public IFreeSql Orm;
        string _dbname;
        GeneratorOptions _options;
        public static Func<Stream> HtmStream { get; set; } = () => typeof(Generator).GetTypeInfo().Assembly
            .GetManifestResourceStream("FreeSql.AdminLTE.htm.zip");

        public Generator(GeneratorOptions options)
        {
            _dbname = AppDomain.CurrentDomain.BaseDirectory + "freesql_adminlte_test.db";
            Orm = new FreeSqlBuilder().UseConnectionString(DataType.Sqlite, $"data source={_dbname};max pool size=1").Build();
            _options = options;
        }
        ~Generator() => Dispose();
        bool _isdisposed = false;
        public void Dispose()
        {
            if (_isdisposed) return;
            _isdisposed = true;
            Orm?.Dispose();
            try { File.Delete(_dbname); } catch { }
        }

        public Action<string> TraceLog;

        /// <summary>
        /// 生成AdminLTE后台管理项目
        /// </summary>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="entityTypes">实体类数组</param>
        /// <param name="IsFirst">是否生成 ApiResult.cs、index.html、htm 静态资源目录</param>
        public void Build(string outputDirectory, Type[] entityTypes, bool IsFirst)
        {
            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
            outputDirectory = outputDirectory.TrimEnd('/', '\\');

            Action<string, string> writeFile = (path, content) =>
            {
                var filename = Path.GetFullPath($"{outputDirectory}/{path.TrimStart('/', '\\')}");
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    sw.Write(content);
                    sw.Close();
                }
                TraceLog?.Invoke($"OUT -> {filename}");
            };

            var isLazyLoading = false;
            #region Views/_ViewImports.cshtml
            var ns = new Dictionary<string, bool>();
            ns.Add("System", true);
            ns.Add("System.Collections.Generic", true);
            ns.Add("System.Collections", true);
            ns.Add("System.Linq", true);
            ns.Add("Newtonsoft.Json", true);
            ns.Add("FreeSql", true);
            foreach (var entityType in entityTypes)
            {
                
                var tb = Orm.CodeFirst.GetTableByEntity(entityType);
                if (tb == null) throw new Exception($"类型 {entityType.FullName} 错误，不能执行生成操作");

                if (!string.IsNullOrEmpty(entityType.Namespace) && !ns.ContainsKey(entityType.Namespace))
                    ns.Add(entityType.Namespace, true);

                foreach (var col in tb.Columns)
                {
                    if (tb.ColumnsByCsIgnore.ContainsKey(col.Key)) continue;
                    if (!string.IsNullOrEmpty(col.Value.CsType.Namespace) && !ns.ContainsKey(col.Value.CsType.Namespace))
                        ns.Add(col.Value.CsType.Namespace, true);
                }

                if (!isLazyLoading)
                {
                    foreach (var prop in tb.Properties)
                    {
                        if (tb.GetTableRef(prop.Key, false) == null) continue;
                        var getProp = entityType.GetMethod($"get_{prop.Key}");
                        var setProp = entityType.GetMethod($"set_{prop.Key}");
                        isLazyLoading = getProp != null || setProp != null;
                    }
                }
            }
            var viewImportsPath = $"{outputDirectory}/Views/{_options.ControllerRouteBase.Trim('/', '\'')}/_ViewImports.cshtml";
            var oldViewImports = (File.Exists(viewImportsPath) ? File.ReadAllText(viewImportsPath, Encoding.UTF8) : "").Split('\n').ToList();

            foreach(var nsk in ns.Keys)
                if (oldViewImports.Where(a => a.Trim().StartsWith("@using ") && Regex.IsMatch(a, @"@using\s+" + nsk.Replace(".", @"\.") + @"\s*;")).Any() == false)
                    oldViewImports.Add($"@using {nsk};");
            if (oldViewImports.Where(a => a.Trim().StartsWith("@inject ") && a.Contains("IFreeSql fsql")).Any() == false)
                oldViewImports.Add("@inject IFreeSql fsql;");
            if (oldViewImports.Where(a => a.Trim().StartsWith("@addTagHelper ") && Regex.IsMatch(a, @"\*\s*,\s*Microsoft\.AspNetCore\.Mvc\.TagHelpers")).Any() == false)
                oldViewImports.Add("@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers;");
            if (oldViewImports.Where(a => a.Trim().StartsWith("@addTagHelper ") && a.Contains("Microsoft.AspNetCore.Mvc.TagHelpers")).Any() == false)
                oldViewImports.Add("@addTagHelper Microsoft.AspNetCore.Mvc.TagHelpers;");

            writeFile($"/Views/_ViewImports.cshtml", string.Join("\r\n", oldViewImports.Select(a => a.Trim())));
            #endregion

            foreach (var et in entityTypes)
            {
                TraceLog?.Invoke("");
                writeFile($"/Controllers/{_options.ControllerRouteBase.Trim('/', '\'')}/{et.GetClassName().Replace(".", "_")}Controller.cs", this.GetControllerCode(et));
                writeFile($"/Views/{et.GetClassName().Replace(".", "_")}/List.cshtml", this.GetViewListCode(et));
                writeFile($"/Views/{et.GetClassName().Replace(".", "_")}/Edit.cshtml", this.GetViewEditCode(et));
            }

            if (IsFirst)
            {
                TraceLog?.Invoke("");
                #region Controllers/ApiResult.cs
                writeFile("/Controllers/ApiResult.cs", $@"using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[JsonObject(MemberSerialization.OptIn)]
public partial class ApiResult : ContentResult
{{
    /// <summary>
    /// 错误代码
    /// </summary>
    [JsonProperty(""code"")] public int Code {{ get; protected set; }}
    /// <summary>
    /// 错误消息
    /// </summary>
    [JsonProperty(""message"")] public string Message {{ get; protected set; }}

    public ApiResult() {{ }}
    public ApiResult(int code, string message) => this.SetCode(code);

    public virtual ApiResult SetCode(int value) {{ this.Code = value; return this; }}
    public virtual ApiResult SetCode(Enum value) {{ this.Code = Convert.ToInt32(value); this.Message = value.ToString(); return this; }}
    public virtual ApiResult SetMessage(string value) {{ this.Message = value; return this; }}

    #region form 表单 target=iframe 提交回调处理
    protected void Jsonp(ActionContext context)
    {{
        string __callback = context.HttpContext.Request.HasFormContentType ? context.HttpContext.Request.Form[""__callback""].ToString() : null;
        if (string.IsNullOrEmpty(__callback))
        {{
            this.ContentType = ""text/json;charset=utf-8;"";
            this.Content = JsonConvert.SerializeObject(this);
        }}
        else
        {{
            this.ContentType = ""text/html;charset=utf-8"";
            this.Content = $""<script>top.{{__callback}}({{GlobalExtensions.Json(null, this)}});</script>"";
        }}
    }}
    public override void ExecuteResult(ActionContext context)
    {{
        Jsonp(context);
        base.ExecuteResult(context);
    }}
    public override Task ExecuteResultAsync(ActionContext context)
    {{
        Jsonp(context);
        return base.ExecuteResultAsync(context);
    }}
    #endregion

    public static ApiResult Success => new ApiResult(0, ""成功"");
    public static ApiResult Failed => new ApiResult(99, ""失败"");
}}

[JsonObject(MemberSerialization.OptIn)]
public partial class ApiResult<T> : ApiResult
{{
    [JsonProperty(""data"")] public T Data {{ get; protected set; }}

    public ApiResult() {{ }}
    public ApiResult(int code) => this.SetCode(code);
    public ApiResult(string message) => this.SetMessage(message);
    public ApiResult(int code, string message) => this.SetCode(code).SetMessage(message);

    new public ApiResult<T> SetCode(int value) {{ this.Code = value; return this; }}
    new public ApiResult<T> SetCode(Enum value) {{ this.Code = Convert.ToInt32(value); this.Message = value.ToString(); return this; }}
    new public ApiResult<T> SetMessage(string value) {{ this.Message = value; return this; }}
    public ApiResult<T> SetData(T value) {{ this.Data = value; return this; }}

    new public static ApiResult<T> Success => new ApiResult<T>(0, ""成功"");
}}

public static class GlobalExtensions
{{
    public static object Json(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper html, object obj)
    {{
        string str = JsonConvert.SerializeObject(obj);
        if (!string.IsNullOrEmpty(str)) str = Regex.Replace(str, @""<(/?script[\s>])"", ""<\""+\""$1"", RegexOptions.IgnoreCase);
        if (html == null) return str;
        return html.Raw(str);
    }}
}}
");
                #endregion
                #region wwwroot/index.html
                writeFile($"/wwwroot/{_options.ControllerRouteBase.Trim('/', '\'')}/index.html", $@"<!DOCTYPE html>
<html lang=""zh-cmn-Hans"">
<head>
	<meta charset=""utf-8"" />
	<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
	<title>FreeSql.AdminLTE</title>
	<meta content=""width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no"" name=""viewport"" />
	<link href=""./htm/bootstrap/css/bootstrap.min.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/font-awesome/css/font-awesome.min.css"" rel=""stylesheet"" />
	<link href=""./htm/css/skins/_all-skins.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/pace/pace.min.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/datepicker/datepicker3.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/timepicker/bootstrap-timepicker.min.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/select2/select2.min.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/treetable/css/jquery.treetable.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/treetable/css/jquery.treetable.theme.default.css"" rel=""stylesheet"" />
	<link href=""./htm/plugins/multiple-select/multiple-select.css"" rel=""stylesheet"" />
	<link href=""./htm/css/system.css"" rel=""stylesheet"" />
	<link href=""./htm/css/index.css"" rel=""stylesheet"" />
	<script type=""text/javascript"" src=""./htm/js/jQuery-2.1.4.min.js""></script>
	<script type=""text/javascript"" src=""./htm/bootstrap/js/bootstrap.min.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/pace/pace.min.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/datepicker/bootstrap-datepicker.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/timepicker/bootstrap-timepicker.min.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/select2/select2.full.min.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/input-mask/jquery.inputmask.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/input-mask/jquery.inputmask.date.extensions.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/input-mask/jquery.inputmask.extensions.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/treetable/jquery.treetable.js""></script>
	<script type=""text/javascript"" src=""./htm/plugins/multiple-select/multiple-select.js""></script>
	<script type=""text/javascript"" src=""./htm/js/lib.js""></script>
	<script type=""text/javascript"" src=""./htm/js/bmw.js""></script>
	<!--[if lt IE 9]>
	<script type='text/javascript' src='./htm/plugins/html5shiv/html5shiv.min.js'></script>
	<script type='text/javascript' src='./htm/plugins/respond/respond.min.js'></script>
	<![endif]-->
</head>
<body class=""hold-transition skin-blue sidebar-mini"">
	<div class=""wrapper"">
		<!-- Main Header-->
		<header class=""main-header"">
			<!-- Logo--><a href=""./"" class=""logo"">
				<!-- mini logo for sidebar mini 50x50 pixels--><span class=""logo-mini""><b>FreeSql.AdminLTE</b></span>
				<!-- logo for regular state and mobile devices--><span class=""logo-lg""><b>FreeSql.AdminLTE</b></span>
			</a>
			<!-- Header Navbar-->
			<nav role=""navigation"" class=""navbar navbar-static-top"">
				<!-- Sidebar toggle button--><a href=""#"" data-toggle=""offcanvas"" role=""button"" class=""sidebar-toggle""><span class=""sr-only"">Toggle navigation</span></a>
				<!-- Navbar Right Menu-->
				<div class=""navbar-custom-menu"">
					<ul class=""nav navbar-nav"">
						<!-- User Account Menu-->
						<li class=""dropdown user user-menu"">
							<!-- Menu Toggle Button--><a href=""#"" data-toggle=""dropdown"" class=""dropdown-toggle"">
								<!-- The user image in the navbar--><img src=""/htm/img/user2-160x160.jpg"" alt=""User Image"" class=""user-image"">
								<!-- hidden-xs hides the username on small devices so only the image appears.--><span class=""hidden-xs""></span>
							</a>
							<ul class=""dropdown-menu"">
								<!-- The user image in the menu-->
								<li class=""user-header"">
									<img src=""/htm/img/user2-160x160.jpg"" alt=""User Image"" class=""img-circle"">
									<p></p>
								</li>
								<!-- Menu Footer-->
								<li class=""user-footer"">
									<div class=""pull-right"">
										<a href=""#"" onclick=""$('form#form_logout').submit();return false;"" class=""btn btn-default btn-flat"">安全退出</a>
										<form id=""form_logout"" method=""post"" action=""./exit.aspx""></form>
									</div>
								</li>
							</ul>
						</li>
					</ul>
				</div>
			</nav>
		</header>
		<!-- Left side column. contains the logo and sidebar-->
		<aside class=""main-sidebar"">
			<!-- sidebar: style can be found in sidebar.less-->
			<section class=""sidebar"">
				<!-- Sidebar Menu-->
				<ul class=""sidebar-menu"">
					<!-- Optionally, you can add icons to the links-->

					<li class=""treeview active"">
						<a href=""#""><i class=""fa fa-laptop""></i><span>通用管理</span><i class=""fa fa-angle-left pull-right""></i></a>
						<ul class=""treeview-menu"">{string.Join("\r\n", entityTypes.Select(et => $@"<li><a href=""{_options.ControllerRouteBase}{et.Name}/""><i class=""fa fa-circle-o""></i>{et.Name}</a></li>"))}</ul>
					</li>

				</ul>
				<!-- /.sidebar-menu-->
			</section>
			<!-- /.sidebar-->
		</aside>
		<!-- Content Wrapper. Contains page content-->
		<div class=""content-wrapper"">
			<!-- Main content-->
			<section id=""right_content"" class=""content"">
				<div style=""display:none;"">
					<!-- Your Page Content Here-->
					<h1>FreeSql.AdminLTE 中件间</h1>
					<h3>
这是 FreeSql 衍生出来的 .NETCore MVC 中间件扩展包，基于 AdminLTE 前端框架动态产生 FreeSql 实体的增删查改界面（QQ群：4336577）。
					</h3>
					<h2>&nbsp;</h2>
					<h2>开源地址：<a href='https://github.com/2881099/FreeSql' target='_blank'>https://github.com/2881099/FreeSql</a><h2>
				</div>
			</section>
			<!-- /.content-->
		</div>
		<!-- /.content-wrapper-->
	</div>
	<!-- ./wrapper-->
	<script type=""text/javascript"" src=""./htm/js/system.js""></script>
	<script type=""text/javascript"" src=""./htm/js/admin.js""></script>
	<script type=""text/javascript"">
		if (!location.hash) $('#right_content div:first').show();
		// 路由功能
		//针对上面的html初始化路由列表
		function hash_encode(str) {{ return url_encode(base64.encode(str)).replace(/%/g, '_'); }}
		function hash_decode(str) {{ return base64.decode(url_decode(str.replace(/_/g, '%'))); }}
		window.div_left_router = {{}};
		$('li.treeview.active ul li a').each(function(index, ele) {{
			var href = $(ele).attr('href');
			$(ele).attr('href', '#base64url' + hash_encode(href));
			window.div_left_router[href] = $(ele).text();
		}});
		(function () {{
			function Vipspa() {{
			}}
			Vipspa.prototype.start = function (config) {{
				Vipspa.mainView = $(config.view);
				startRouter();
				window.onhashchange = function () {{
					if (location._is_changed) return location._is_changed = false;
					startRouter();
				}};
			}};
			function startRouter() {{
				var hash = location.hash;
				if (hash === '') return //location.hash = $('li.treeview.active ul li a:first').attr('href');//'#base64url' + hash_encode('/resume_type/');
				if (hash.indexOf('#base64url') !== 0) return;
				var act = hash_decode(hash.substr(10, hash.length - 10));
				//叶湘勤增加的代码，加载或者提交form后，显示内容
				function ajax_success(refererUrl) {{
					if (refererUrl == location.pathname) {{ startRouter(); return function(){{}}; }}
					var hash = '#base64url' + hash_encode(refererUrl);
					if (location.hash != hash) {{
						location._is_changed = true;
						location.hash = hash;
					}}'\''
					return function (data, status, xhr) {{
						var div;
						Function.prototype.ajax = $.ajax;
						top.mainViewNav = {{
							url: refererUrl,
							trans: function (url) {{
								var act = url;
								act = act.substr(0, 1) === '/' || act.indexOf('://') !== -1 || act.indexOf('data:') === 0 ? act : join_url(refererUrl, act);
								return act;
							}},
							goto: function (url_or_form, target) {{
								var form = url_or_form;
								if (typeof form === 'string') {{
									var act = this.trans(form);
									if (String(target).toLowerCase() === '_blank') return window.open(act);
									location.hash = '#base64url' + hash_encode(act);
								}}
								else {{
									if (!window.ajax_form_iframe_max) window.ajax_form_iframe_max = 1;
									window.ajax_form_iframe_max++;
									var iframe = $('<iframe name=""ajax_form_iframe{{0}}""></iframe>'.format(window.ajax_form_iframe_max));
									Vipspa.mainView.append(iframe);
									var act = $(form).attr('action') || '';
									act = act.substr(0, 1) === '/' || act.indexOf('://') !== -1 ? act : join_url(refererUrl, act);
									if ($(form).find(':file[name]').length > 0) $(form).attr('enctype', 'multipart/form-data');
									$(form).attr('action', act);
									$(form).attr('target', iframe.attr('name'));
									iframe.on('load', function () {{
										var doc = this.contentWindow ? this.contentWindow.document : this.document;
										if (doc.body.innerHTML.length === 0) return;
										if (doc.body.innerHTML.indexOf('Error:') === 0) return alert(doc.body.innerHTML.substr(6));
										//以下 '<script ' + '是防止与本页面相匹配，不要删除
										if (doc.body.innerHTML.indexOf('<script ' + 'type=""text/javascript"">location.href=""') === -1) {{
											ajax_success(doc.location.pathname + doc.location.search)(doc.body.innerHTML, 200, null);
										}}
									}});
								}}
							}},
							reload: startRouter,
							query: qs_parseByUrl(refererUrl)
						}};
						top.mainViewInit = function () {{
							if (!div) return setTimeout(top.mainViewInit, 10);
							admin_init(function (selector) {{
								if (/<[^>]+>/.test(selector)) return $(selector);
								return div.find(selector);
							}}, top.mainViewNav);
						}};
						if (/<body[^>]*>/i.test(data))
							data = data.match(/<body[^>]*>(([^<]|<(?!\/body>))*)<\/body>/i)[1];
						div = Vipspa.mainView.html(data);
					}};
				}};
				$.ajax({{
					type: 'GET',
					url: act,
					dataType: 'html',
					success: ajax_success(act),
					error: function (jqXHR, textStatus, errorThrown) {{
						var data = jqXHR.responseText;
						if (/<body[^>]*>/i.test(data))
							data = data.match(/<body[^>]*>(([^<]|<(?!\/body>))*)<\/body>/i)[1];
						Vipspa.mainView.html(data);
					}}
				}});
			}}
			window.vipspa = new Vipspa();
		}})();
		$(function () {{
			vipspa.start({{
				view: '#right_content',
			}});
		}});
		// 页面加载进度条
		$(document).ajaxStart(function() {{ Pace.restart(); }});
	</script>
</body>
</html>");
                #endregion
                #region wwwroot/htm
                var htmDir = $"{outputDirectory}/wwwroot/{_options.ControllerRouteBase.Trim('/', '\'')}/htm";
                var zipPath = $"{htmDir}/{Guid.NewGuid()}.zip";
                if (Directory.Exists(htmDir)) Directory.Delete(htmDir, true);
                Directory.CreateDirectory(htmDir);
                using (var zip = HtmStream())
                {
                    using (var fs = File.Open(zipPath, FileMode.OpenOrCreate))
                    {
                        zip.CopyTo(fs);
                        fs.Close();
                    }
                    zip.Close();
                }

                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, htmDir, Encoding.UTF8);
                    TraceLog?.Invoke($"OUT -> {htmDir}/*");
                }
                catch (Exception ex)
                {
                    throw new Exception($"BuildProject 错误，资源文件解压失败：{ex.Message}", ex);
                }
                finally
                {
                    File.Delete(zipPath);
                }
                #endregion
            }
        }

        /// <summary>
        /// 获得控制器Controller代码
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public string GetControllerCode(Type entityType)
        {
            var tb = Orm.CodeFirst.GetTableByEntity(entityType);
            if (tb == null) throw new Exception($"类型 {entityType.FullName} 错误，不能执行生成操作");

            var ns = new Dictionary<string, bool>();
            ns.Add("System", true);
            ns.Add("System.Collections.Generic", true);
            ns.Add("System.Collections", true);
            ns.Add("System.Linq", true);
            ns.Add("System.Threading.Tasks", true);
            ns.Add("Microsoft.AspNetCore.Http", true);
            ns.Add("Microsoft.AspNetCore.Mvc", true);
            ns.Add("Microsoft.AspNetCore.Mvc.Filters", true);
            ns.Add("Microsoft.Extensions.Logging", true);
            ns.Add("Microsoft.Extensions.Configuration", true);
            ns.Add("Newtonsoft.Json", true);
            ns.Add("FreeSql", true);

            if (!string.IsNullOrEmpty(entityType.Namespace) && !ns.ContainsKey(entityType.Namespace))
                ns.Add(entityType.Namespace, true);

            #region 多对一，多对多设置
            var listKeyWhere = "";
            var listInclude = "";
            var listFromQuery = "";
            var listFromQuerySelect = "";
            var listFromQueryMultiCombine = "";
            var editIncludeMany = "";
            var editFromForm = "";
            var editFromFormAdd = "";
            var editFromFormEdit = "";
            //var editFromFormMultiCombine = "";
            var delFromNew = "";
            foreach (var col in tb.ColumnsByCs)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(col.Value.CsName)) continue;
                if (col.Value.CsType == typeof(string))
                    listKeyWhere += $" || a.{col.Value.CsName}.Contains(key)";

                if (!string.IsNullOrEmpty(col.Value.CsType.Namespace) && !ns.ContainsKey(col.Value.CsType.Namespace))
                    ns.Add(col.Value.CsType.Namespace, true);
            }
            foreach (var prop in tb.Properties)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(prop.Key)) continue;
                var tref = tb.GetTableRef(prop.Key, false);
                if (tref == null) continue;
                switch (tref.RefType)
                {
                    case TableRefType.ManyToMany:
                    case TableRefType.OneToMany:
                        break;
                    case TableRefType.ManyToOne:
                    case TableRefType.OneToOne:
                        listInclude += $".Include(a => a.{prop.Key})";
                        var treftb = Orm.CodeFirst.GetTableByEntity(tref.RefEntityType);
                        foreach (var col in treftb.Columns)
                        {
                            if (treftb.ColumnsByCsIgnore.ContainsKey(col.Value.CsName)) continue;
                            if (col.Value.CsType == typeof(string))
                                listKeyWhere += $" || a.{prop.Key}.{col.Value.CsName}.Contains(key)";
                        }
                        break;
                }
                switch (tref.RefType)
                {
                    case TableRefType.ManyToOne:
                        if (tref.Columns.Count == 1)
                        {
                            var fkNs = $"{prop.Key}_{tref.RefColumns[0].CsName}";
                            listFromQuery += $", [FromQuery] {tref.Columns[0].CsType.GetGenericName()}[] {fkNs}";
                            listFromQuerySelect += $"\r\n                .WhereIf({fkNs}?.Any() == true, a => {fkNs}.Contains(a.{tref.Columns[0].CsName}))";
                        }
                        else
                        {
                            var multiNs = $"{prop.Key}_{tref.RefColumns[0].CsName}";
                            for (var a = 0; a < tref.Columns.Count; a++)
                            {
                                var fkNs = $"{prop.Key}_{tref.RefColumns[a].CsName}";
                                listFromQuery += $", [FromQuery] {tref.Columns[a].CsType.GetGenericName()}[] {fkNs}";
                                if (a > 0)
                                    multiNs += $@"?.Select((a, idx) => a + ""|"" + {fkNs}[idx])";
                            }
                            multiNs += "?.ToArray()";
                            listFromQueryMultiCombine += $"\r\n            var {prop.Key}_multi = {multiNs};";
                            listFromQuerySelect += $"\r\n                .WhereIf({prop.Key}_multi?.Any() == true, a => {prop.Key}_multi.Contains({string.Join(@" + ""|"" + ", tref.Columns.Select(a => $"a.{a.CsName}"))}))";
                        }
                        break;
                }
            }
            foreach (var prop in tb.Properties.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(prop.Name)) continue;
                var tref = tb.GetTableRef(prop.Name, false);
                if (tref == null) continue;
                switch (tref.RefType)
                {
                    case TableRefType.ManyToMany:
                        if (tref.RefColumns.Count == 1)
                        {
                            var mnNs = $"mn_{prop.Name}_{tref.RefColumns[0].CsName}";
                            listFromQuery += $", [FromQuery] {tref.RefColumns[0].CsType.GetGenericName()}[] {mnNs}";
                            listFromQuerySelect += $"\r\n                .WhereIf({mnNs}?.Any() == true, a => a.{prop.Name}.AsSelect().Any(b => {mnNs}.Contains(b.{tref.RefColumns[0].CsName})))";

                            editIncludeMany += $".IncludeMany(a => a.{prop.Name})";
                            editFromForm += $", [FromForm] {tref.RefColumns[0].CsType.GetGenericName()}[] {mnNs}";
                            editFromFormAdd += $@"
                //关联 {tref.RefEntityType.GetClassName()}
                await ctx.SaveManyAsync(item, ""{prop.Name}"");";
                            editFromFormEdit += $@"
                //关联 {tref.RefEntityType.GetClassName()}
                await ctx.SaveManyAsync(item, ""{prop.Name}"");";
                            /*
                            editFromFormAdd += $@"
                //关联 {tref.RefEntityType.GetClassName()}
                var mn_{prop.Name} = {mnNs}.Select((mn, idx) => new {tref.RefMiddleEntityType.GetClassName()} {{ {tref.MiddleColumns[tref.Columns.Count].CsName} = mn, {string.Join(", ", tref.Columns.Select((a, idx) => $"{tref.MiddleColumns[idx].CsName} = item.{a.CsName}"))} }}).ToArray();
                await ctx.AddRangeAsync(mn_{prop.Name});";
                            editFromFormEdit += $@"
                //关联 {tref.RefEntityType.GetClassName()}
                if ({mnNs} != null)
                {{
                    var {mnNs}_list = {mnNs}.ToList();
                    var oldlist = ctx.Set<{tref.RefMiddleEntityType.GetClassName()}>().Where(a => {string.Join(" && ", tref.Columns.Select((a, idx) => $"a.{tref.MiddleColumns[idx].CsName} == item.{a.CsName}"))}).ToList();
                    foreach (var olditem in oldlist)
                    {{
                        var idx = {mnNs}_list.FindIndex(a => a == olditem.{tref.MiddleColumns[tref.Columns.Count].CsName});
                        if (idx == -1) ctx.Remove(olditem);
                        else {mnNs}_list.RemoveAt(idx);
                    }}
                    var mn_{prop.Name} = {mnNs}_list.Select((mn, idx) => new {tref.RefMiddleEntityType.GetClassName()} {{ {tref.MiddleColumns[tref.Columns.Count].CsName} = mn, {string.Join(", ", tref.Columns.Select((a, idx) => $"{tref.MiddleColumns[idx].CsName} = item.{a.CsName}"))} }}).ToArray();
                    await ctx.AddRangeAsync(mn_{prop.Name});
                }}";
                               */
                        }
                        //else
                        //{
                        //    var multiNs = $"mn_{tref.RefMiddleEntityType.Name}_{tref.RefColumns[0].CsName}";
                        //    for (var a = 0; a < tref.RefColumns.Count; a++)
                        //    {
                        //        var mnNs = $"mn_{tref.RefMiddleEntityType.Name}_{tref.RefColumns[a].CsName}";
                        //        listFromQuery += $", [FromQuery] {tref.RefColumns[a].CsType.GetGenericName()}[] {mnNs}";
                        //        if (a > 0)
                        //            multiNs += $@"?.Select((a, idx) => a + ""|"" + {mnNs}[idx])";
                        //    }
                        //    multiNs += "?.ToArray()";
                        //    listFromQueryMultiCombine += $"\r\n            var mn_{tref.RefMiddleEntityType.Name}_multi = {multiNs};";
                        //    listFromQuerySelect += $"\r\n                .WhereIf(mn_{tref.RefMiddleEntityType.Name}_multi?.Any() == true, a => a.{prop.Key}.AsSelect().Any(b => mn_{tref.RefMiddleEntityType.Name}_multi.Contains({string.Join(@" + ""|"" + ", tref.RefColumns.Select(a => $"b.{a.CsName}"))})))";
                        //}
                        break;
                }
            }

            var editGet = "";
            var editPost = "";
            if (tb.Primarys.Any())
            {
                delFromNew += $"{tb.Primarys[0].CsName}?.Select((a, idx) => new {entityType.GetClassName()} {{ ";
                foreach (var pk in tb.Primarys)
                    delFromNew += $"{pk.CsName} = {pk.CsName}[idx], ";
                delFromNew = delFromNew.Remove(delFromNew.Length - 2) + " })";

                editGet = $@"

        [HttpGet(""edit"")]
        async public Task<ActionResult> Edit({string.Join(", ", tb.Primarys.Select(pk => $"[FromQuery] {pk.CsType.GetGenericName()} {pk.CsName}"))})
        {{
            var item = await fsql.Select<{entityType.GetClassName()}>(){editIncludeMany}.Where(a => {string.Join(" && ", tb.Primarys.Select(pk => $"a.{pk.CsName} == {pk.CsName}"))}).FirstAsync();
            if (item == null) return ApiResult.Failed.SetMessage(""记录不存在"");
            ViewBag.item = item;
            return View();
        }}";
                editPost = $@"

        [HttpPost(""edit"")]
        [ValidateAntiForgeryToken]
        async public Task<ApiResult> _Edit({string.Join(", ", tb.Columns.Values.Where(a => !a.Attribute.IsIgnore).Select(col => $"[FromForm] {col.CsType.GetGenericName()} {col.CsName}"))}{editFromForm})
        {{
            //var item = new {entityType.GetClassName()}();
            {string.Join("\r\n            ", tb.Primarys.Select(col => $"//item.{col.CsName} = {col.CsName};"))}
            using (var ctx = fsql.CreateDbContext())
            {{
                //ctx.Attach(item);
                var item = await ctx.Set<{entityType.GetClassName()}>().Where(a => {string.Join(" && ", tb.Primarys.Select(pk => $"a.{pk.CsName} == {pk.CsName}"))}).FirstAsync();
                {string.Join("\r\n                ", tb.Columns.Values.Where(a => !a.Attribute.IsPrimary && !a.Attribute.IsIgnore).Select(col => $"item.{col.CsName} = {col.CsName};"))}
                await ctx.UpdateAsync(item);{editFromFormEdit}
                var affrows = await ctx.SaveChangesAsync();
                if (affrows > 0) return ApiResult.Success.SetMessage($""更新成功，影响行数：{{affrows}}"");
            }}
            return ApiResult.Failed;
        }}

        [HttpPost(""del"")]
        [ValidateAntiForgeryToken]
        async public Task<ApiResult> _Del({string.Join(", ", tb.Primarys.Select(pk => $"[FromForm] {pk.CsType.GetGenericName()}[] {pk.CsName}"))})
        {{
            var items = {delFromNew};
            var affrows = await fsql.Delete<{entityType.GetClassName()}>().WhereDynamic(items).ExecuteAffrowsAsync();
            return ApiResult.Success.SetMessage($""更新成功，影响行数：{{affrows}}"");
        }}";
            }
            #endregion

            #region 拼接代码
            return $@"using {string.Join(";\r\nusing ", ns.Keys)};

namespace {_options.ControllerNameSpace}.Controllers
{{
    [Route(""{_options.ControllerRouteBase}[controller]"")]
    public class {entityType.GetClassName().Replace(".", "_")}Controller : {_options.ControllerBase}
    {{
        IFreeSql fsql;
        public {entityType.GetClassName().Replace(".", "_")}Controller(IFreeSql orm) {{
            fsql = orm;
        }}

        [HttpGet]
        async public Task<ActionResult> List([FromQuery] string key{listFromQuery}, [FromQuery] int limit = 20, [FromQuery] int page = 1)
        {{{listFromQueryMultiCombine}
            var select = fsql.Select<{entityType.GetClassName()}>(){listInclude}{(string.IsNullOrEmpty(listKeyWhere) ? "" : $"\r\n                .WhereIf(!string.IsNullOrEmpty(key), a => {listKeyWhere.Substring(4)})")}{listFromQuerySelect};
            var items = await select.Count(out var count).Page(page, limit).ToListAsync();
            ViewBag.items = items;
            ViewBag.count = count;
            return View();
        }}

        [HttpGet(""add"")]
        public ActionResult Edit() => View();{editGet}

        /***************************************** POST *****************************************/

        [HttpPost(""add"")]
        [ValidateAntiForgeryToken]
        async public Task<ApiResult> _Add({string.Join(", ", tb.Columns.Values.Where(a => !a.Attribute.IsIgnore && !a.Attribute.IsIdentity && (!a.Attribute.IsPrimary || a.Attribute.IsPrimary && a.CsType.NullableTypeOrThis() != typeof(Guid))).Select(col => $"[FromForm] {col.CsType.GetGenericName()} {col.CsName}"))}{editFromForm})
        {{
            var item = new {entityType.GetClassName()}();
            {string.Join("\r\n            ", tb.Columns.Values.Where(a => !a.Attribute.IsIgnore && !a.Attribute.IsIdentity && (!a.Attribute.IsPrimary || a.Attribute.IsPrimary && a.CsType.NullableTypeOrThis() != typeof(Guid))).Select(col => $"item.{col.CsName} = {col.CsName};"))}
            using (var ctx = fsql.CreateDbContext())
            {{
                await ctx.AddAsync(item);{editFromFormAdd}
                await ctx.SaveChangesAsync();
            }}
            return ApiResult<object>.Success.SetData(item);
        }}{editPost}
    }}
}}";
            #endregion
        }

        /// <summary>
        /// 获取视图View列表页代码
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public string GetViewListCode(Type entityType)
        {
            var tb = Orm.CodeFirst.GetTableByEntity(entityType);
            if (tb == null) throw new Exception($"类型 {entityType.FullName} 错误，不能执行生成操作");

            #region THead Td
            var listTh = new StringBuilder();
            var listTd = new StringBuilder();
            var dicCol = new Dictionary<string, bool>();

            if (tb.Primarys.Any())
            {
                listTd.Append($"\r\n								<td><input type=\"checkbox\" id=\"id\" name=\"id\" value=\"{string.Join(",", tb.Primarys.Select(pk => $"@item.{pk.CsName}"))}\" /></td>");
                foreach (var col in tb.Primarys)
                {
                    listTh.Append($"\r\n						<th scope=\"col\">{(col.Comment ?? col.CsName)}{(col.Attribute.IsIdentity ? "(自增)" : "")}</th>");
                    listTd.Append($"\r\n								<td>@item.{col.CsName}</td>");
                    if (dicCol.ContainsKey(col.CsName) == false) dicCol.Add(col.CsName, true);
                }
            }
            foreach (var prop in tb.Properties.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(prop.Name)) continue;
                var tref = tb.GetTableRef(prop.Name, false);
                if (tref == null) continue;
                switch (tref.RefType)
                {
                    case TableRefType.ManyToOne:
                    case TableRefType.OneToOne:
                        var tbref = Orm.CodeFirst.GetTableByEntity(tref.RefEntityType);
                        var tbrefName = tbref.Columns.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault()?.CsName;
                        if (!string.IsNullOrEmpty(tbrefName)) tbrefName = $"?.{tbrefName}";
                        listTh.Append($"\r\n						<th scope=\"col\">{string.Join(",", tref.Columns.Select(a => a.Comment ?? a.CsName))}</th>");
                        listTd.Append($"\r\n								<td>[{string.Join(",", tref.Columns.Select(a => $"@item.{a.CsName}"))}] @item.{prop.Name}{tbrefName}</td>");
                        foreach (var col in tref.Columns) if (dicCol.ContainsKey(col.CsName) == false) dicCol.Add(col.CsName, true);
                        break;
                }
            }
            foreach (var col in tb.Columns.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(col.CsName)) continue;
                if (dicCol.ContainsKey(col.CsName)) continue;
                listTh.Append($"\r\n						<th scope=\"col\">{(col.Comment ?? col.CsName)}</th>");
                listTd.Append($"\r\n								<td>@item.{col.CsName}</td>");
            }
            if (tb.Primarys.Any())
            {
                listTd.Append($"\r\n								<td><a href=\"./edit?{string.Join("&", tb.Primarys.Select(pk => $"{pk.CsName}=@item.{pk.CsName}"))}\">修改</a></td>");
            }
            #endregion

            #region 多对一、多对多
            var selectCode = "";
            var fscCode = "";
            foreach (var prop in tb.Properties.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(prop.Name)) continue;
                var tref = tb.GetTableRef(prop.Name, false);
                if (tref == null) continue;

                var tbref = Orm.CodeFirst.GetTableByEntity(tref.RefEntityType);
                var tbrefName = tbref.Columns.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault()?.CsName;
                if (!string.IsNullOrEmpty(tbrefName)) tbrefName = $".{tbrefName}";

                switch (tref.RefType)
                {
                    case TableRefType.ManyToOne:
                        selectCode += $"\r\n	var fk_{prop.Name}s = fsql.Select<{tref.RefEntityType.GetClassName()}>().ToList();";
                        fscCode += $"\r\n			{{ name: '{prop.Name}', field: '{string.Join(",", tref.Columns.Select(a => a.CsName))}', text: @Html.Json(fk_{prop.Name}s.Select(a => a{tbrefName})), value: @Html.Json(fk_{prop.Name}s.Select(a => {string.Join(" + \"|\" + ", tref.RefColumns.Select(a => "a." + a.CsName))})) }},";
                        break;
                    case TableRefType.ManyToMany:
                        selectCode += $"\r\n	var mn_{prop.Name} = fsql.Select<{tref.RefEntityType.GetClassName()}>().ToList();";
                        fscCode += $"\r\n			{{ name: '{prop.Name}', field: '{string.Join(",", tref.RefColumns.Select(a => $"mn_{prop.Name}_{a.CsName}"))}', text: @Html.Json(mn_{prop.Name}.Select(a => a{tbrefName})), value: @Html.Json(mn_{prop.Name}.Select(a => {string.Join(" + \"|\" + ", tref.RefColumns.Select(a => "a." + a.CsName))})) }},";
                        break;
                }
            }
            #endregion

            #region 拼接代码
            return $@"@{{
    Layout = """";
}}

<div class=""box"">
	<div class=""box-header with-border"">
		<h3 id=""box-title"" class=""box-title""></h3>
		<span class=""form-group mr15""></span><a href=""./add"" data-toggle=""modal"" class=""btn btn-success pull-right"">添加</a>
	</div>
	<div class=""box-body"">
		<div class=""table-responsive"">
			<form id=""form_search"">
				<div id=""div_filter""></div>
			</form>
			<form id=""form_list"" action=""./del"" method=""post"">
				@Html.AntiForgeryToken()
				<input type=""hidden"" name=""__callback"" value=""del_callback""/>
				<table id=""GridView1"" cellspacing=""0"" rules=""all"" border=""1"" style=""border-collapse:collapse;"" class=""table table-bordered table-hover text-nowrap"">
					<tr>
						<th scope=""col"" style=""width:2%;""><input type=""checkbox"" onclick=""$('#GridView1 tbody tr').each(function (idx, el) {{ var chk = $(el).find('td:first input[type=\'checkbox\']')[0]; if (chk) chk.checked = !chk.checked; }});"" /></th>
{listTh.ToString()}
						<th scope=""col"" style=""width:5%;"">&nbsp;</th>
					</tr>
					<tbody>
						@foreach({entityType.GetClassName()} item in ViewBag.items) {{
							<tr>
{listTd.ToString()}
                            </tr>
						}}
					</tbody>
				</table>
			</form>
			<a id=""btn_delete_sel"" href=""#"" class=""btn btn-danger pull-right"">删除选中项</a>
			<div id=""kkpager""></div>
		</div>
	</div>
</div>

@{{{selectCode}
}}
<script type=""text/javascript"">
	(function () {{
		top.del_callback = function(rt) {{
			if (rt.code == 0) return top.mainViewNav.goto('./?' + new Date().getTime());
			alert(rt.message);
		}};

		var qs = _clone(top.mainViewNav.query);
		var page = cint(qs.page, 1);
		delete qs.page;
		$('#kkpager').html(cms2Pager(@ViewBag.count, page, 20, qs, 'page'));
		var fqs = _clone(top.mainViewNav.query);
		delete fqs.page;
		var fsc = [{fscCode}
			null
		];
		fsc.pop();
		cms2Filter(fsc, fqs);
		top.mainViewInit();
	}})();
</script>";
            #endregion
        }

        /// <summary>
        /// 获取视图Edit编辑页代码
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public string GetViewEditCode(Type entityType)
        {
            var tb = Orm.CodeFirst.GetTableByEntity(entityType);
            if (tb == null) throw new Exception($"类型 {entityType.FullName} 错误，不能执行生成操作");

            #region 编辑项
            var editTr = new StringBuilder();
            var editTrMany = new StringBuilder();
            var editParentFk = new StringBuilder();
            var editInitSelectUI = new StringBuilder();
            Action<ColumnInfo> editTrAppend = col =>
            {
                var lname = col.CsName.ToLower();
                var csType = col.CsType.NullableTypeOrThis();
                if (csType == typeof(bool))
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td id=""{col.CsName}_td""><input name=""{col.CsName}"" type=""checkbox"" value=""true"" /></td>
						</tr>");
                else if (csType == typeof(DateTime) && new[] { "create_time", "update_time" }.Contains(lname))
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td><input name=""{col.CsName}"" type=""text"" class=""datepicker"" style=""width:20%;background-color:#ddd;"" /></td>
						</tr>");
                else if (new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(int), typeof(uint) }.Contains(csType))
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td><input name=""{col.CsName}"" type=""text"" class=""form-control"" data-inputmask=""'mask': '9', 'repeat': 6, 'greedy': false"" data-mask style=""width:200px;"" /></td>
						</tr>");
                else if (new[] { typeof(double), typeof(float), typeof(decimal) }.Contains(csType))
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td>
                                <div class=""input-group"" style=""width:200px;"">
									<span class=""input-group-addon"">￥</span>
									<input name=""{col.CsName}"" type=""text"" class=""form-control"" data-inputmask=""'mask': '9', 'repeat': 10, 'greedy': false"" data-mask />
									<span class=""input-group-addon"">.00</span>
								</div>
                            </td>
						</tr>");
                else if (new[] { typeof(DateTime), typeof(DateTimeOffset) }.Contains(csType))
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td><input name=""{col.CsName}"" type=""text"" class=""datepicker"" /></td>
						</tr>");
                else if (csType == typeof(string) && (lname == "img" || lname.StartsWith("img_") || lname.EndsWith("_img") ||
                    lname == "path" || lname.StartsWith("path_") || lname.EndsWith("_path")))
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td>
                                <input name=""{col.CsName}"" type=""text"" class=""datepicker"" style=""width:60%;"" />
								<input name=""{col.CsName}_file"" type=""file"">
                            </td>
						</tr>");
                else if (csType == typeof(string) && new[] { "content", "text", "descript", "description", "reason", "html", "data" }.Contains(lname))
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td><textarea name=""{col.CsName}"" style=""width:100%;height:100px;"" editor=""ueditor""></textarea></td>
						</tr>");
                else if (csType.IsEnum)
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td>
                                <select name=""{col.CsName}""{(csType.GetCustomAttribute<FlagsAttribute>() != null ? $@" data-placeholder=""Select a {csType.GetClassName()}"" class=""form-control select2"" multiple>" : @"><option value="""">------ 请选择 ------</option>")}
									@foreach (object eo in Enum.GetValues(typeof({csType.FullName}))) {{ <option value=""@eo"">@eo</option> }}
								</select>
                            </td>
						</tr>");
                else
                    editTr.Append($@"
					    <tr>
							<td>{(col.Comment ?? col.CsName)}</td>
							<td><input name=""{col.CsName}"" type=""text"" class=""datepicker"" style=""width:60%;"" /></td>
						</tr>");
            };
            var dicCol = new Dictionary<string, bool>();
            foreach (var col in tb.Primarys)
            {
                if (col.Attribute.IsIdentity || col.CsType == typeof(Guid))
                    editTr.Append($@"
						@if (item != null) {{
							<tr>
								<td>{(col.Comment ?? col.CsName)}{(col.Attribute.IsIdentity ? "(自增)" : "")}</td>
								<td><input name=""{col.CsName}"" type=""text"" readonly class=""datepicker"" style=""width:20%;background-color:#ddd;"" /></td>
							</tr>
						}}");
                else
                    editTrAppend(col);
                if (dicCol.ContainsKey(col.CsName) == false) dicCol.Add(col.CsName, true);
            }

            var selectCode = "";
            foreach (var prop in tb.Properties.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(prop.Name)) continue;
                var tref = tb.GetTableRef(prop.Name, false);
                if (tref == null) continue;

                var tbref = Orm.CodeFirst.GetTableByEntity(tref.RefEntityType);
                var tbrefName = tbref.Columns.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault()?.CsName;
                if (!string.IsNullOrEmpty(tbrefName)) tbrefName = $".{tbrefName}";

                switch (tref.RefType)
                {
                    case TableRefType.ManyToOne:
                        selectCode += $"\r\n	var fk_{prop.Name}s = fsql.Select<{tref.RefEntityType.GetClassName()}>().ToList();";
                        break;
                    case TableRefType.ManyToMany:
                        selectCode += $"\r\n	var mn_{prop.Name} = fsql.Select<{tref.RefEntityType.GetClassName()}>().ToList();";
                        break;
                }

                switch (tref.RefType)
                {
                    case TableRefType.ManyToOne:
                    case TableRefType.OneToOne:
                        if (tref.RefEntityType == entityType) //树形关系
                        {
                            editTr.Append($@"
						<tr>
							<td>{string.Join(",", tref.Columns.Select(a => a.Comment ?? a.CsName))}</td>
							<td id=""{prop.Name}_td""></td>
						</tr>");
                            editParentFk.Append($@"
        $('#{prop.Name}_td').html(yieldTreeSelect(yieldTreeArray(@Html.Json(fk_{prop.Name}s), null, '{string.Join(",", tref.RefColumns.Select(a => a.CsName))}', '{string.Join(",", tref.Columns.Select(a => a.CsName))}'), '{{#{(string.IsNullOrEmpty(tbrefName) ? tref.RefColumns[0].CsName : tbrefName.Substring(2))}}}', '{string.Join(",", tref.RefColumns.Select(a => a.CsName))}')).find('select').attr('name', '{string.Join(",", tref.Columns.Select(a => a.CsName))}');");
                        }
                        else
                            editTr.Append($@"
						<tr>
							<td>{string.Join(",", tref.Columns.Select(a => a.Comment ?? a.CsName))}</td>
							<td>
                                <select name=""{tref.Columns[0].CsName}"">
									<option value="""">------ 请选择 ------</option>
									@foreach (var fk in fk_{prop.Name}s) {{ <option value=""{string.Join(",", tref.RefColumns.Select(a => $"@fk.{a.CsName}"))}"">@fk{tbrefName}</option> }}
								</select>
                            </td>
					    </tr>");
                        foreach (var col in tref.Columns) if (dicCol.ContainsKey(col.CsName) == false) dicCol.Add(col.CsName, true);
                        break;
                    case TableRefType.ManyToMany:
                        editTrMany.Append($@"
						<tr>
							<td>{prop.Name}</td>
							<td>
								<select name=""mn_{prop.Name}_{tref.RefColumns[0].CsName}"" data-placeholder=""Select a {tref.RefEntityType.GetClassName()}"" class=""form-control select2"" multiple>
									@foreach (var mn in mn_{prop.Name}) {{ <option value=""@mn.{tref.RefColumns[0].CsName}"">@mn{tbrefName}</option> }}
								</select>
							</td>
						</tr>");
                        editInitSelectUI.Append($@"item.mn_{prop.Name} = @Html.Json(item.{prop.Name});
			for (var a = 0; a @Html.Raw('<') item.mn_{prop.Name}.length; a++) $(form.mn_{prop.Name}_{tref.RefColumns[0].CsName}).find('option[value=""{{0}}""]'.format(item.mn_{prop.Name}[a].{tref.RefColumns[0].CsName})).attr('selected', 'selected');");
                        break;
                }
            }
            foreach (var col in tb.Columns.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(col.CsName)) continue;
                if (dicCol.ContainsKey(col.CsName)) continue;
                editTrAppend(col);
            }
            editTr.Append(editTrMany);
            #endregion

            #region 拼接代码
            return $@"@{{
	Layout = """";
	{entityType.GetClassName()} item = ViewBag.item;{selectCode}
}}

<div class=""box"">
	<div class=""box-header with-border"">
		<h3 class=""box-title"" id=""box-title""></h3>
	</div>
	<div class=""box-body"">
		<div class=""table-responsive"">
			<form id=""form_add"" method=""post"">
				@Html.AntiForgeryToken()
				<input type=""hidden"" name=""__callback"" value=""edit_callback"" />
				<div>
					<table cellspacing=""0"" rules=""all"" class=""table table-bordered table-hover"" border=""1"" style=""border-collapse:collapse;"">{editTr.ToString()}
						<tr>
							<td width=""8%"">&nbsp</td>
							<td><input type=""submit"" value=""@(item == null ? ""添加"" : ""更新"")"" />&nbsp;<input type=""button"" value=""取消"" /></td>
						</tr>
					</table>
				</div>
			</form>

		</div>
	</div>
</div>

<script type=""text/javascript"">
	(function () {{
		top.edit_callback = function (rt) {{
			if (rt.code == 0) return top.mainViewNav.goto('./?' + new Date().getTime());
			alert(rt.message);
		}};{editParentFk}

		var form = $('#form_add')[0];
		var item = null;
		@if (item != null) {{
			<text>
			item = @Html.Json(item);
			fillForm(form, item);{editInitSelectUI}
			</text>
		}}

		top.mainViewInit();
	}})();
</script>";
            #endregion
        }
    }

    static class TypeExtens
    {
        public static string GetClassName(this Type that) => that.IsNested ? $"{that.DeclaringType.Name}.{that.Name}" : that.Name;
        public static string GetGenericName(this Type that)
        {
            var ret = that?.NullableTypeOrThis().Name;
            if (that == typeof(bool)) ret = "bool";

            else if (that == typeof(int)) ret = "int";
            else if (that == typeof(long)) ret = "long";
            else if (that == typeof(short)) ret = "short";
            else if (that == typeof(sbyte)) ret = "sbyte";

            else if (that == typeof(uint)) ret = "uint";
            else if (that == typeof(ulong)) ret = "ulong";
            else if (that == typeof(ushort)) ret = "ushort";
            else if (that == typeof(byte)) ret = "byte";

            else if (that == typeof(double)) ret = "double";
            else if (that == typeof(float)) ret = "float";
            else if (that == typeof(decimal)) ret = "decimal";

            else if (that == typeof(string)) ret = "string";

            return ret + (that.IsNullableType() ? "?" : "");
        }
    }
}
