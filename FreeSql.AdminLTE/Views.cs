using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSql {
	internal class Views {

		public static readonly string Edit = @"
{%
var columns = tb.Columns as Dictionary<string, FreeSql.Internal.Model.ColumnInfo>;
object getitem = gethash;
var getlistfilter = getlistFilter as Dictionary<string, (FreeSql.Internal.Model.TableRef, string, string, Dictionary<string, object>, List<Dictionary<string, object>>)>;
var getlistmanyed = getlistManyed as Dictionary<string, IEnumerable<string>>;

var sb5 = new StringBuilder();
%}

<div class=""box"">
	<div class=""box-header with-border"">
		<h3 class=""box-title"" id=""box-title""></h3>
	</div>
	<div class=""box-body"">
		<div class=""table-responsive"">
			<form id=""form_add"" method=""post"">
				<input type=""hidden"" name=""__callback"" value=""edit_callback"" />
				<div>
					<table cellspacing=""0"" rules=""all"" class=""table table-bordered table-hover"" border=""1"" style=""border-collapse:collapse;"">
{for colVal2 in columns}

{%
FreeSql.Internal.Model.ColumnInfo colVal = colVal2.Value;

var manyToOneFilter = getlistfilter.Values.Where(a => 
		a.Item1.RefType == FreeSql.Internal.Model.TableRefType.ManyToOne &&
		a.Item1.Columns.Where(b => b.CsName == colVal.CsName).Any()
	).FirstOrDefault();
%}

	{% if (manyToOneFilter.Item1 != null) { %}
		<tr @if=""manyToOneFilter.Item1.RefEntityType == tb.Type"">
			<td>{#colVal.CsName}</td>
			<td id=""{#colVal.CsName}_td""></td>
			{%
			sb5.AppendFormat(@""
			$('#{3}_td').html(yieldTreeSelect(yieldTreeArray({0}, {1}, '{2}', '{3}'), '{4}', '{2}')).find('select').attr('name', '{5}');"",
				Newtonsoft.Json.JsonConvert.SerializeObject(manyToOneFilter.Item5),
				Newtonsoft.Json.JsonConvert.SerializeObject(FreeSql.Internal.Utils.GetDataReaderValue(manyToOneFilter.Item1.Columns[0].CsType, null)),
				manyToOneFilter.Item1.RefColumns[0].CsName,
				manyToOneFilter.Item1.Columns[0].CsName,
				""{"" + ""#"" + (columns.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault()?.CsName ?? manyToOneFilter.Item1.Columns[0].CsName) + ""}"",
				colVal.CsName);
			%}
		</tr>
		<tr @else="""">
			<td>{#colVal.CsName}</td>
			<td>
				<select name=""{#colVal.CsName}"">
					<option value="""">------ 请选择 ------</option>
					<option @for=""eo in manyToOneFilter.Item4"" value=""{#eo.Key}"">{#eo.Value}</option>
				</select>
			</td>
		</tr>
	{% } else if (colVal.Attribute.IsVersion || colVal.Attribute.IsPrimary && (new [] { typeof(Guid), typeof(Guid?) }.Contains(colVal.CsType) || colVal.Attribute.IsIdentity)) { %}
		{% if (getitem != null) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<input name=""{#colVal.CsName}"" type=""text"" readonly class=""datepicker"" style=""width:20%;background-color:#ddd;"" />
			</td>
		</tr>
		{% } %}
	{% } else if (new [] { typeof(bool), typeof(bool?) }.Contains(colVal.CsType)) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<input name=""{#colVal.CsName}"" type=""checkbox"" value=""true"" />
			</td>
		</tr>
	{% } else if (new [] { typeof(DateTime), typeof(DateTime?) }.Contains(colVal.CsType) && new [] { ""create_time"", ""update_time"", ""createtime"", ""updatetime"" }.Contains(colVal.CsName.ToLower())) { %}
		{% if (getitem != null) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<input name=""{#colVal.CsName}"" type=""text"" readonly class=""datepicker"" style=""width:20%;background-color:#ddd;"" />
			</td>
		</tr>
		{% } %}
	{% } else if (new [] { typeof(int), typeof(int?), typeof(uint), typeof(uint?), typeof(long), typeof(long?), typeof(ulong), typeof(ulong?), typeof(short), typeof(short?), typeof(ushort), typeof(ushort?), typeof(byte), typeof(byte?), typeof(sbyte), typeof(sbyte?) }.Contains(colVal.CsType)) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<input name=""{#colVal.CsName}"" type=""text"" class=""form-control"" data-inputmask=""'mask': '9', 'repeat': 6, 'greedy': false"" data-mask style=""width:200px;"" />
			</td>
		</tr>
	{% } else if (new [] { typeof(decimal), typeof(decimal?), typeof(double), typeof(double?), typeof(float), typeof(float?) }.Contains(colVal.CsType)) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<div class=""input-group"" style=""width:200px;"">
					<span class=""input-group-addon"">￥</span>
					<input name=""{#colVal.CsName}"" type=""text"" class=""form-control"" data-inputmask=""'mask': '9', 'repeat': 10, 'greedy': false"" data-mask />
					<span class=""input-group-addon"">.00</span>
				</div>
			</td>
		</tr>
	{% } else if (new [] { typeof(DateTime), typeof(DateTime?), typeof(DateTimeOffset), typeof(DateTimeOffset?) }.Contains(colVal.CsType)) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<div class=""input-group date"" style=""width:200px;"">
					<div class=""input-group-addon""><i class=""fa fa-calendar""></i></div>
					<input name=""{#colVal.CsName}"" type=""text"" data-provide=""datepicker"" class=""form-control pull-right"" readonly />
				</div>
			</td>
		</tr>
	{% } else if (new [] { typeof(string) }.Contains(colVal.CsType) && (colVal.CsName.ToLower() == ""img"" || colVal.CsName.ToLower().StartsWith(""img_"") || colVal.CsName.ToLower().EndsWith(""_img"") || colVal.CsName.ToLower() == ""path"" || colVal.CsName.ToLower().StartsWith(""path_"") || colVal.CsName.ToLower().EndsWith(""_path""))) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<input name=""{#colVal.CsName}"" type=""text"" class=""datepicker"" style=""width:60%;"" />
				<input name=""{#colVal.CsName}_file"" type=""file"">
			</td>
		</tr>
	{% } else if (new [] { typeof(string) }.Contains(colVal.CsType) && new [] { ""varchar(255)"",""nvarchar2(255)"",""varchar(255)"",""nvarchar(255)"",""nvarchar(255)"" }.Contains(colVal.Attribute.DbType.ToLower()) == false) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<textarea name=""{#colVal.CsName}"" style=""width:100%;height:100px;"" editor=""ueditor""></textarea>
			</td>
		</tr>
	{% } else if (colVal.CsType.IsEnum && colVal.Attribute.DbType == ""set"") { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<select name=""{#colVal.CsName}"" data-placeholder=""Select {#colVal.CsName}"" class=""form-control select2"" multiple>
					<option @for=""eo in Enum.GetValues(colVal.CsType)"" value=""{#eo}"">{#eo}</option>
				</select>
			</td>
		</tr>
	{% } else if (colVal.CsType.IsEnum) { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<select name=""{#colVal.CsName}"">
					<option value="""">------ 请选择 ------</option>
					<option @for=""eo in Enum.GetValues(colVal.CsType)"" value=""{#eo}"">{#eo}</option>
				</select>
			</td>
		</tr>
	{% } else { %}
		<tr>
			<td>{#colVal.CsName}</td>
			<td>
				<input name=""{#colVal.CsName}"" type=""text"" class=""datepicker"" style=""width:60%;"" />
			</td>
		</tr>
	{% } %}

{/for}

{% foreach (var trygetf in getlistfilter) { %}
	<tr @if=""trygetf.Value.Item1.RefType == FreeSql.Internal.Model.TableRefType.ManyToMany"">
		<td>{#trygetf.Key}</td>
		<td>
			<select name=""mn_{#trygetf.Key}"" data-placeholder=""Select {#trygetf.Value.Item1.RefEntityType.Name}"" class=""form-control select2"" multiple>
				<option @for=""eo in trygetf.Value.Item4"" value=""{#eo.Key}"">{#eo.Value}</option>
			</select>
		</td>
	</tr>
{% } %}

						<tr>
							<td width=""8%"">&nbsp</td>
							<td>
								<input @if=""getitem == null"" type=""submit"" value=""添加"" /><input @else="""" type=""submit"" value=""更新"" />&nbsp;<input type=""button"" value=""取消"" />
							</td>
						</tr>
					</table>
				</div>
			</form>

		</div>
	</div>
</div>

<script type=""text/javascript"">
	(function () {
		top.edit_callback = function (rt) {
			if (rt.success) return top.mainViewNav.goto('./?' + new Date().getTime());
			alert(rt.message);
		};
{#sb5.ToString()}

		var form = $('#form_add')[0];
		var item = null;
{% if (getitem != null) { %}
		item = {#Newtonsoft.Json.JsonConvert.SerializeObject(getitem)};
		fillForm(form, item);
{% } %}

{%
	foreach (var trygetmanyed in getlistmanyed) {
		foreach (var trygetmanyeditem in trygetmanyed.Value) { %}
			$(form.mn_{#trygetmanyed.Key}).find('option[value=""{#trygetmanyeditem}""]').attr('selected', 'selected');
{%
		}
	}
%}

		top.mainViewInit();
	})();
</script>";

		public static readonly string List = @"<div class=""box"">
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
				<input type=""hidden"" name=""__callback"" value=""del_callback""/>
				<table id=""GridView1"" cellspacing=""0"" rules=""all"" border=""1"" style=""border-collapse:collapse;"" class=""table table-bordered table-hover"">
					<tr>
						<th scope=""col"" style=""width:2%;""><input type=""checkbox"" onclick=""$('#GridView1 tbody tr').each(function (idx, el) { var chk = $(el).find('td:first input[type=\'checkbox\']')[0]; if (chk) chk.checked = !chk.checked; });"" /></th>

						<th @for=""colVal in tb.ColumnsByCs"" scope=""col"">{#colVal.Key}</th>
						
						<th scope=""col"" style=""width:5%;"">&nbsp;</th>
					</tr>
					<tbody>
						<tr @for=""rowitem,index in getlist"">
{%
var rowitemIdVal = """";
var rowitemIdValIndex = 0;
var editParams = """";
foreach (var colPk in tb.Primarys) {
	if (rowitemIdValIndex++ > 0) {
		rowitemIdVal += "","";
		editParams += ""&"";
	}
	var tmpPkVal = rowitem[colPk.CsName];
    rowitemIdVal += tmpPkVal;
	editParams += colPk.CsName + ""="" + tmpPkVal;
}
%}
							<td><input type=""checkbox"" id=""id"" name=""id"" value=""{#rowitemIdVal}"" /></td>

							<td @for=""colVal in tb.ColumnsByCs"">{#rowitem[colVal.Key]}</td>

							<td><a href=""./edit?{#editParams}"">修改</a></td>
						</tr>
					</tbody>
				</table>
			</form>
			<a id=""btn_delete_sel"" href=""#"" class=""btn btn-danger pull-right"">删除选中项</a>
			<div id=""kkpager""></div>
		</div>
	</div>
</div>
<script type=""text/javascript"">
	(function () {
		top.del_callback = function(rt) {
			if (rt.success) return top.mainViewNav.goto('./?' + new Date().getTime());
			alert(rt.message);
		};

		var qs = _clone(top.mainViewNav.query);
		var page = cint(qs.page, 1);
		delete qs.page;
		$('#kkpager').html(cms2Pager({#getlistTotal}, page, 20, qs, 'page'));
		var fqs = _clone(top.mainViewNav.query);
		delete fqs.page;
		var fsc = [
{for navfilterc in getlistFilter}
{ name: '{#navfilterc.Item2}', field: '{#navfilterc.Item3}', text: {#Newtonsoft.Json.JsonConvert.SerializeObject(navfilterc.Item4.Values)}, value: {#Newtonsoft.Json.JsonConvert.SerializeObject(navfilterc.Item4.Keys)} },
{/for}
			null
		];
		fsc.pop();
		cms2Filter(fsc, fqs);
		top.mainViewInit();
	})();
</script>
";

		public static readonly string Index = @"<!DOCTYPE html>
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
						<ul class=""treeview-menu""></ul>
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
		function hash_encode(str) { return url_encode(base64.encode(str)).replace(/%/g, '_'); }
		function hash_decode(str) { return base64.decode(url_decode(str.replace(/_/g, '%'))); }
		window.div_left_router = {};
		$('li.treeview.active ul li a').each(function(index, ele) {
			var href = $(ele).attr('href');
			$(ele).attr('href', '#base64url' + hash_encode(href));
			window.div_left_router[href] = $(ele).text();
		});
		(function () {
			function Vipspa() {
			}
			Vipspa.prototype.start = function (config) {
				Vipspa.mainView = $(config.view);
				startRouter();
				window.onhashchange = function () {
					if (location._is_changed) return location._is_changed = false;
					startRouter();
				};
			};
			function startRouter() {
				var hash = location.hash;
				if (hash === '') return //location.hash = $('li.treeview.active ul li a:first').attr('href');//'#base64url' + hash_encode('/resume_type/');
				if (hash.indexOf('#base64url') !== 0) return;
				var act = hash_decode(hash.substr(10, hash.length - 10));
				//叶湘勤增加的代码，加载或者提交form后，显示内容
				function ajax_success(refererUrl) {
					if (refererUrl == location.pathname) { startRouter(); return function(){}; }
					var hash = '#base64url' + hash_encode(refererUrl);
					if (location.hash != hash) {
						location._is_changed = true;
						location.hash = hash;
					}'\''
					return function (data, status, xhr) {
						var div;
						Function.prototype.ajax = $.ajax;
						top.mainViewNav = {
							url: refererUrl,
							trans: function (url) {
								var act = url;
								act = act.substr(0, 1) === '/' || act.indexOf('://') !== -1 || act.indexOf('data:') === 0 ? act : join_url(refererUrl, act);
								return act;
							},
							goto: function (url_or_form, target) {
								var form = url_or_form;
								if (typeof form === 'string') {
									var act = this.trans(form);
									if (String(target).toLowerCase() === '_blank') return window.open(act);
									location.hash = '#base64url' + hash_encode(act);
								}
								else {
									if (!window.ajax_form_iframe_max) window.ajax_form_iframe_max = 1;
									window.ajax_form_iframe_max++;
									var iframe = $('<iframe name=""ajax_form_iframe{0}""></iframe>'.format(window.ajax_form_iframe_max));
									Vipspa.mainView.append(iframe);
									var act = $(form).attr('action') || '';
									act = act.substr(0, 1) === '/' || act.indexOf('://') !== -1 ? act : join_url(refererUrl, act);
									if ($(form).find(':file[name]').length > 0) $(form).attr('enctype', 'multipart/form-data');
									$(form).attr('action', act);
									$(form).attr('target', iframe.attr('name'));
									iframe.on('load', function () {
										var doc = this.contentWindow ? this.contentWindow.document : this.document;
										if (doc.body.innerHTML.length === 0) return;
										if (doc.body.innerHTML.indexOf('Error:') === 0) return alert(doc.body.innerHTML.substr(6));
										//以下 '<script ' + '是防止与本页面相匹配，不要删除
										if (doc.body.innerHTML.indexOf('<script ' + 'type=""text/javascript"">location.href=""') === -1) {
											ajax_success(doc.location.pathname + doc.location.search)(doc.body.innerHTML, 200, null);
										}
									});
								}
							},
							reload: startRouter,
							query: qs_parseByUrl(refererUrl)
						};
						top.mainViewInit = function () {
							if (!div) return setTimeout(top.mainViewInit, 10);
							admin_init(function (selector) {
								if (/<[^>]+>/.test(selector)) return $(selector);
								return div.find(selector);
							}, top.mainViewNav);
						};
						if (/<body[^>]*>/i.test(data))
							data = data.match(/<body[^>]*>(([^<]|<(?!\/body>))*)<\/body>/i)[1];
						div = Vipspa.mainView.html(data);
					};
				};
				$.ajax({
					type: 'GET',
					url: act,
					dataType: 'html',
					success: ajax_success(act),
					error: function (jqXHR, textStatus, errorThrown) {
						var data = jqXHR.responseText;
						if (/<body[^>]*>/i.test(data))
							data = data.match(/<body[^>]*>(([^<]|<(?!\/body>))*)<\/body>/i)[1];
						Vipspa.mainView.html(data);
					}
				});
			}
			window.vipspa = new Vipspa();
		})();
		$(function () {
			vipspa.start({
				view: '#right_content',
			});
		});
		// 页面加载进度条
		$(document).ajaxStart(function() { Pace.restart(); });
	</script>
</body>
</html>";
	}
}
