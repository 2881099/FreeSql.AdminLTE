using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FreeSql.AdminLTE
{
    public class Generator : IDisposable
    {
        string _dbname;
        IFreeSql _fsql;
        GeneratorOptions _options;

        public Generator(GeneratorOptions options)
        {
            _dbname = AppDomain.CurrentDomain.BaseDirectory + Guid.NewGuid().ToString("N") + ".db";
            _fsql = new FreeSqlBuilder().UseConnectionString(DataType.Sqlite, $"data source={_dbname};max pool size=1").Build();
            _options = options;
        }
        ~Generator() => Dispose();
        bool _isdisposed = false;
        public void Dispose()
        {
            if (_isdisposed) return;
            _isdisposed = true;
            _fsql?.Dispose();
            try
            {
                File.Delete(_dbname);
            }
            catch { }
        }

        /// <summary>
        /// 生成完整的AdminLTE后台管理项目
        /// </summary>
        /// <param name="outputDirectory"></param>
        /// <param name="entityTypes"></param>
        public void BuildProject(string outputDirectory, Type[] entityTypes)
        {
            if (string.IsNullOrEmpty(outputDirectory)) outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
            outputDirectory = outputDirectory.TrimEnd('/', '\\');

            Action<string, string> writeFile = (path, content) =>
            {
                var filename = $"{outputDirectory}/{path.TrimStart('/', '\\')}";
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    sw.Write(content);
                    sw.Close();
                }
            };
            
            #region appsettings.json
            writeFile("/appsettings.Development.json", @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Debug"",
      ""System"": ""Information"",
      ""Microsoft"": ""Information""
    }
  }
}
");
            writeFile("appsettings.json", @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}
");
            #endregion
            #region Program.cs
            writeFile("/Program.cs", $@"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace {_options.NameSpace} {{
    public class Program {{
        public static void Main(string[] args) {{
            CreateWebHostBuilder(args).Build().Run();
        }}

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }}
}}
");
            #endregion
            #region Starup.cs
            writeFile("/Starup.cs", $@"using FreeSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace {_options.NameSpace}
{{
    public class Startup
    {{
        public Startup(IConfiguration configuration, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {{
            Newtonsoft.Json.JsonConvert.DefaultSettings = () =>
            {{
                var st = new Newtonsoft.Json.JsonSerializerSettings();
                st.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                st.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
                st.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.RoundtripKind;
                return st;
            }};

            Configuration = configuration;
            Env = env;

            Fsql = new FreeSqlBuilder()
                .UseConnectionString(DataType.Sqlite, @""Data Source=|DataDirectory|/test.db;Pooling=true;Max Pool Size=5"")
                .UseAutoSyncStructure(true)
                .UseLazyLoading(true)
                .UseMonitorCommand(cmd => Trace.WriteLine(cmd.CommandText))
                .Build();
        }}

        public IConfiguration Configuration {{ get; }}
        public IHostingEnvironment Env {{ get; }}
        public static IFreeSql Fsql {{ get; private set; }}

        public void ConfigureServices(IServiceCollection services)
        {{
            services.AddSingleton<IFreeSql>(Fsql);
            services.AddScoped<CustomExceptionFilter>();
            services.AddCors(options => options.AddPolicy(""cors_all"", builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

            services.AddMvc();
            if (Env.IsDevelopment())
                services.AddSwaggerGen(options =>
                {{
                    options.IgnoreObsoleteActions();
                    //options.IgnoreObsoleteControllers(); // 类、方法标记 [Obsolete]，可以阻止【Swagger文档】生成
                    options.EnableAnnotations();
                    options.DescribeAllEnumsAsStrings();
                    options.CustomSchemaIds(a => a.FullName);
                    options.OperationFilter<FormDataOperationFilter>();
                    //options.IncludeXmlComments(""Admin.xml"");
                }});
        }}

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {{
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.GetEncoding(""GB2312"");
            Console.InputEncoding = Encoding.GetEncoding(""GB2312"");

            loggerFactory.AddConsole(Configuration.GetSection(""Logging""));
            loggerFactory.AddNLog().AddDebug();
            NLog.LogManager.LoadConfiguration(""nlog.config"");

            if (Env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseCors(""cors_all"");
            app.UseMvc();
            app.UseDefaultFiles();
		    app.UseStaticFiles();

            if (Env.IsDevelopment())
                app.UseSwagger().UseSwaggerUI(options => options.SwaggerEndpoint($""/swagger/V1/swagger.json"", ""V1""));
        }}

        public class FormDataOperationFilter : IOperationFilter
        {{
            public void Apply(Operation operation, OperationFilterContext context)
            {{
                if (context.ApiDescription.TryGetMethodInfo(out var method) == false) return;
                var actattrs = method.GetCustomAttributes(false);
                if (actattrs.OfType<HttpPostAttribute>().Any() ||
                    actattrs.OfType<HttpPutAttribute>().Any())
                    if (operation.Consumes.Count == 0)
                        operation.Consumes.Add(""application/x-www-form-urlencoded"");
            }}
        }}
    }}
}}
");
            #endregion
            #region nlog.config
            writeFile("/nlog.config", @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    autoReload=""true""
    internalLogLevel=""Warn""
    internalLogFile=""internal-nlog.txt"">

    <!-- Load the ASP.NET Core plugin -->
    <extensions>
        <add assembly=""NLog.Web.AspNetCore""/>
    </extensions>

    <!-- Layout: https://github.com/NLog/NLog/wiki/Layout%20Renderers -->
    <targets>
        <target xsi:type=""File"" name=""allfile"" fileName=""../nlog/all-${{shortdate}}.log""
            layout=""${{longdate}}|${{logger}}|${{uppercase:${{level}}}}|${{message}} ${{exception}}|${{aspnet-Request-Url}}"" />

        <target xsi:type=""File"" name=""ownFile-web"" fileName=""../nlog/own-${{shortdate}}.log""
            layout=""${{longdate}}|${{logger}}|${{uppercase:${{level}}}}|  ${{message}} ${{exception}}|${{aspnet-Request-Url}}"" />

        <target xsi:type=""Null"" name=""blackhole"" />
    </targets>

    <rules>
        <logger name=""*"" minlevel=""Error"" writeTo=""allfile"" />
        <logger name=""Microsoft.*"" minlevel=""Error"" writeTo=""blackhole"" final=""true"" />
        <logger name=""*"" minlevel=""Error"" writeTo=""ownFile-web"" />
    </rules>
</nlog>
");
            #endregion
            #region Common/Controllers/ApiResult.cs
            writeFile("/Common/Controllers/ApiResult.cs", $@"using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
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
");
            #endregion
            #region Common/Controllers/BaseController.cs
            writeFile("/Common/Controllers/BaseController.cs", $@"using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

[ServiceFilter(typeof(CustomExceptionFilter)), EnableCors(""cors_all"")]
public partial class BaseController : Controller
{{
    public ILogger _logger;
    public ISession Session {{ get {{ return HttpContext.Session; }} }}
    public HttpRequest Req {{ get {{ return Request; }} }}
    public HttpResponse Res {{ get {{ return Response; }} }}

    public string Ip => this.Request.Headers[""X-Real-IP""].FirstOrDefault() ?? this.Request.HttpContext.Connection.RemoteIpAddress.ToString();
    public IConfiguration Configuration => (IConfiguration)HttpContext.RequestServices.GetService(typeof(IConfiguration));

    [FromHeader(Name = ""token"")] public string Header_token {{ get; set; }}

    public BaseController(ILogger logger) {{ _logger = logger; }}

    async public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {{
        string token = Request.Headers[""token""].FirstOrDefault() ?? Request.Query[""token""].FirstOrDefault();
        if (!string.IsNullOrEmpty(token))
        {{
            try
            {{
            }}
            catch
            {{
                context.Result = ApiResult.Failed.SetMessage(""登陆TOKEN失效_请重新登陆""); ;
                return;
            }}
        }}
        if (this.ValidModelState(context) == false) return;
        await base.OnActionExecutionAsync(context, next);
    }}

    public override void OnActionExecuting(ActionExecutingContext context)
    {{
        if (this.ValidModelState(context) == false) return;
        base.OnActionExecuting(context);
    }}

    bool ValidModelState(ActionExecutingContext context)
    {{
        if (context.ModelState.IsValid == false)
            foreach (var value in context.ModelState.Values)
                if (value.Errors.Any())
                {{
                    context.Result = Json(ApiResult.Failed.SetMessage($""参数格式不正确：{{value.Errors.First().ErrorMessage}}""));
                    return false;
                }}
        return true;
    }}
}}");
            #endregion
            #region Common/Controllers/CustomExceptionFilter.cs
            writeFile("/Common/Controllers/CustomExceptionFilter.cs", $@"using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

public class CustomExceptionFilter : Attribute, IExceptionFilter
{{
    private ILogger _logger = null;
    private IConfiguration _cfg = null;
    private IHostingEnvironment _env = null;

    public CustomExceptionFilter(ILogger<CustomExceptionFilter> logger, IConfiguration cfg, IHostingEnvironment env)
    {{
        _logger = logger;
        _cfg = cfg;
        _env = env;
    }}

    public void OnException(ExceptionContext context)
    {{
        //在这里记录错误日志，context.Exception 为异常对象
        context.Result = ApiResult.Failed.SetMessage(context.Exception.Message); //返回给调用方
        var innerLog = context.Exception.InnerException != null ? $"" \r\n{{context.Exception.InnerException.Message}} \r\n{{ context.Exception.InnerException.StackTrace}}"" : """";
        _logger.LogError($""=============错误：{{context.Exception.Message}} \r\n{{context.Exception.StackTrace}}{{innerLog}}"");
        context.ExceptionHandled = true;
    }}
}}");
            #endregion
            #region Common/Extensions/GlobalExtensions.cs
            writeFile("/Common/Extensions/GlobalExtensions.cs", $@"using Newtonsoft.Json;
using System;
using System.Text;
using System.Text.RegularExpressions;

public static class GlobalExtensions {{
	public static object Json(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper html, object obj) {{
		string str = JsonConvert.SerializeObject(obj);
		if (!string.IsNullOrEmpty(str)) str = Regex.Replace(str, @""<(/?script[\s>])"", ""<\""+\""$1"", RegexOptions.IgnoreCase);
		if (html == null) return str;
		return html.Raw(str);
    }}

	/// <summary>
	/// 转格林时间，并以ISO8601格式化字符串
	/// </summary>
	/// <param name=""time""></param>
	/// <returns></returns>
	public static string ToGmtISO8601(this DateTime time) {{
		return time.ToUniversalTime().ToString(""yyyy-MM-ddTHH:mm:ssZ"");
	}}

	/// <summary>
	/// 获取时间戳，按1970-1-1
	/// </summary>
	/// <param name=""time""></param>
	/// <returns></returns>
	public static long GetTime(this DateTime time) {{
		return (long)time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
	}}

	static readonly DateTime dt19700101 = new DateTime(1970, 1, 1);
	/// <summary>
	/// 获取时间戳毫秒数，按1970-1-1
	/// </summary>
	/// <param name=""time""></param>
	/// <returns></returns>
	public static long GetTimeMilliseconds(this DateTime time) {{
		return (long)time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
	}}

    #region ==数据转换扩展==

    /// <summary>
    /// 转换成Byte
    /// </summary>
    /// <param name=""s"">输入字符串</param>
    /// <returns></returns>
    public static byte ToByte(this object s)
    {{
        if (s == null || s == DBNull.Value)
            return 0;

        byte.TryParse(s.ToString(), out byte result);
        return result;
    }}

    /// <summary>
    /// 转换成short/Int16
    /// </summary>
    /// <param name=""s""></param>
    /// <returns></returns>
    public static short ToShort(this object s)
    {{
        if (s == null || s == DBNull.Value)
            return 0;

        short.TryParse(s.ToString(), out short result);
        return result;
    }}

    /// <summary>
    /// 转换成Int/Int32
    /// </summary>
    /// <param name=""s""></param>
    /// <param name=""round"">是否四舍五入，默认false</param>
    /// <returns></returns>
    public static int ToInt(this object s, bool round = false)
    {{
        if (s == null || s == DBNull.Value)
            return 0;

        if (s.GetType().IsEnum)
        {{
            return (int)s;
        }}

        if (s is bool b)
            return b ? 1 : 0;

        if (int.TryParse(s.ToString(), out int result))
            return result;

        var f = s.ToFloat();
        return round ? Convert.ToInt32(f) : (int)f;
    }}

    /// <summary>
    /// 转换成Long/Int64
    /// </summary>
    /// <param name=""s""></param>
    /// <returns></returns>
    public static long ToLong(this object s)
    {{
        if (s == null || s == DBNull.Value)
            return 0L;

        long.TryParse(s.ToString(), out long result);
        return result;
    }}

    /// <summary>
    /// 转换成Float/Single
    /// </summary>
    /// <param name=""s""></param>
    /// <param name=""decimals"">小数位数</param>
    /// <returns></returns>
    public static float ToFloat(this object s, int? decimals = null)
    {{
        if (s == null || s == DBNull.Value)
            return 0f;

        float.TryParse(s.ToString(), out float result);

        if (decimals == null)
            return result;

        return (float)Math.Round(result, decimals.Value);
    }}

    /// <summary>
    /// 转换成Double/Single
    /// </summary>
    /// <param name=""s""></param>
    /// <param name=""digits"">小数位数</param>
    /// <returns></returns>
    public static double ToDouble(this object s, int? digits = null)
    {{
        if (s == null || s == DBNull.Value)
            return 0d;

        double.TryParse(s.ToString(), out double result);

        if (digits == null)
            return result;

        return Math.Round(result, digits.Value);
    }}

    /// <summary>
    /// 转换成Decimal
    /// </summary>
    /// <param name=""s""></param>
    /// <param name=""decimals"">小数位数</param>
    /// <returns></returns>
    public static decimal ToDecimal(this object s, int? decimals = null)
    {{
        if (s == null || s == DBNull.Value) return 0m;

        decimal.TryParse(s.ToString(), out decimal result);

        if (decimals == null)
            return result;

        return Math.Round(result, decimals.Value);
    }}

    /// <summary>
    /// 转换成DateTime
    /// </summary>
    /// <param name=""s""></param>
    /// <returns></returns>
    public static DateTime ToDateTime(this object s)
    {{
        if (s == null || s == DBNull.Value)
            return DateTime.MinValue;

        DateTime.TryParse(s.ToString(), out DateTime result);
        return result;
    }}

    /// <summary>
    /// 转换成Date
    /// </summary>
    /// <param name=""s""></param>
    /// <returns></returns>
    public static DateTime ToDate(this object s)
    {{
        return s.ToDateTime().Date;
    }}

    /// <summary>
    /// 转换成Boolean
    /// </summary>
    /// <param name=""s""></param>
    /// <returns></returns>
    public static bool ToBool(this object s)
    {{
        if (s == null) return false;
        s = s.ToString().ToLower();
        if (s.Equals(1) || s.Equals(""1"") || s.Equals(""true"") || s.Equals(""是"") || s.Equals(""yes""))
            return true;
        if (s.Equals(0) || s.Equals(""0"") || s.Equals(""false"") || s.Equals(""否"") || s.Equals(""no""))
            return false;

        Boolean.TryParse(s.ToString(), out bool result);
        return result;
    }}



    /// <summary>
    /// 泛型转换，转换失败会抛出异常
    /// </summary>
    /// <typeparam name=""T""></typeparam>
    /// <param name=""s""></param>
    /// <returns></returns>
    public static T To<T>(this object s)
    {{
        return (T)Convert.ChangeType(s, typeof(T));
    }}

    #endregion

    #region ==布尔转换==

    /// <summary>
    /// 布尔值转换为字符串1或者0
    /// </summary>
    /// <param name=""b""></param>
    /// <returns></returns>
    public static string ToIntString(this bool b)
    {{
        return b ? ""1"" : ""0"";
    }}

    /// <summary>
    /// 布尔值转换为整数1或者0
    /// </summary>
    /// <param name=""b""></param>
    /// <returns></returns>
    public static int ToInt(this bool b)
    {{
        return b ? 1 : 0;
    }}

    /// <summary>
    /// 布尔值转换为中文
    /// </summary>
    /// <param name=""b""></param>
    /// <returns></returns>
    public static string ToZhCn(this bool b)
    {{
        return b ? ""是"" : ""否"";
    }}

    #endregion

    #region ==字节转换==

    /// <summary>
    /// 转换为16进制
    /// </summary>
    /// <param name=""bytes""></param>
    /// <param name=""lowerCase"">是否小写</param>
    /// <returns></returns>
    public static string ToHex(this byte[] bytes, bool lowerCase = true)
    {{
        if (bytes == null)
            return null;

        var result = new StringBuilder();
        var format = lowerCase ? ""x2"" : ""X2"";
        for (var i = 0; i < bytes.Length; i++)
        {{
            result.Append(bytes[i].ToString(format));
        }}

        return result.ToString();
    }}

    /// <summary>
    /// 16进制转字节数组
    /// </summary>
    /// <param name=""s""></param>
    /// <returns></returns>
    public static byte[] HexToBytes(this string s)
    {{
        if (string.IsNullOrEmpty(s))
            return null;
        var bytes = new byte[s.Length / 2];

        for (int x = 0; x < s.Length / 2; x++)
        {{
            int i = (Convert.ToInt32(s.Substring(x * 2, 2), 16));
            bytes[x] = (byte)i;
        }}

        return bytes;
    }}

    /// <summary>
    /// 转换为Base64
    /// </summary>
    /// <param name=""bytes""></param>
    /// <returns></returns>
    public static string ToBase64(this byte[] bytes)
    {{
        if (bytes == null)
            return null;

        return Convert.ToBase64String(bytes);
    }}

    #endregion
}}
");
            #endregion
            #region wwwroot/index.html
            writeFile("/wwwroot/index.html", $@"<!DOCTYPE html>
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
            var zipPath = $"{outputDirectory}/{Guid.NewGuid()}.zip";
            using (var zip = StaticFiles.WwwrootStream())
            {
                using (var fs = File.Open(zipPath, FileMode.OpenOrCreate))
                {
                    zip.CopyTo(fs);
                    fs.Close();
                }
                zip.Close();
            }
            if (Directory.Exists($"{outputDirectory}/wwwroot/htm")) Directory.Delete($"{outputDirectory}/wwwroot/htm", true);
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, $"{outputDirectory}/wwwroot", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"WriteFullCsproj 错误，资源文件解压失败：{ex.Message}", ex);
            }
            finally
            {
                File.Delete(zipPath);
            }
            #endregion

            var isLazyLoading = false;
            #region Views
            var ns = new Dictionary<string, bool>();
            ns.Add("System", true);
            ns.Add("System.Collections.Generic", true);
            ns.Add("System.Collections", true);
            ns.Add("System.Linq", true);
            ns.Add("Newtonsoft.Json", true);
            ns.Add("FreeSql", true);
            foreach (var entityType in entityTypes)
            {
                var tb = _fsql.CodeFirst.GetTableByEntity(entityType);
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
            writeFile("/Views/_ViewImports.cshtml", $@"@using {string.Join(";\r\n@using ", ns.Keys)};

@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@inject IFreeSql fsql;
");
            writeFile("/Views/_ViewStart.cshtml", $@"@{{
	Layout = ""_Layout"";
}}");
            #endregion
            foreach (var et in entityTypes)
            {
                writeFile($"/Controllers/{et.Name}Controller.cs", this.GetControllerCode(et));
                writeFile($"/Views/{et.Name}/List.cshtml", this.GetViewListCode(et));
                writeFile($"/Views/{et.Name}/Edit.cshtml", this.GetViewEditCode(et));
            }

            #region FreeSql.AdminLTE.csproj
            writeFile($"/{_options.NameSpace}.csproj", $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

    <PropertyGroup>
        <TargetFramework>netcoreapp2.1</TargetFramework>
        <WarningLevel>3</WarningLevel>
        <ServerGarbageCollection>false</ServerGarbageCollection>
        <MvcRazorCompileOnPublish>false</MvcRazorCompileOnPublish>
        <LangVersion>7.1</LangVersion>
    </PropertyGroup>

    <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
      <DocumentationFile>Admin.xml</DocumentationFile>
    </PropertyGroup>

    <ItemGroup>
        <Content Update=""nlog.config"">
            <CopyToOutputDirectory>Always</CopyToOutputDirectory>
        </Content>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include=""Microsoft.AspNetCore.App"" />
        <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""2.1.1"" />
        <PackageReference Include=""Microsoft.AspNetCore.Session"" Version=""2.1.1"" />
        <PackageReference Include=""Microsoft.AspNetCore.Diagnostics"" Version=""2.1.1"" />
        <PackageReference Include=""Microsoft.Extensions.Configuration.EnvironmentVariables"" Version=""2.1.1"" />
        <PackageReference Include=""Microsoft.Extensions.Configuration.FileExtensions"" Version=""2.1.1"" />
        <PackageReference Include=""Microsoft.Extensions.Configuration.Json"" Version=""2.1.1"" />
        <PackageReference Include=""NLog.Extensions.Logging"" Version=""1.4.0"" />
        <PackageReference Include=""NLog.Web.AspNetCore"" Version=""4.8.0"" />
        <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.2"" />
        <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""4.0.1"" />
        <PackageReference Include=""Swashbuckle.AspNetCore.Annotations"" Version=""4.0.1"" />
        <PackageReference Include=""System.Drawing.Common"" Version=""4.5.1"" />
        <PackageReference Include=""System.Text.Encoding.CodePages"" Version=""4.5.1"" />

        <PackageReference Include=""CSRedisCore"" Version=""3.1.5"" />
        <PackageReference Include=""Caching.CSRedis"" Version=""3.0.51"" />
        <PackageReference Include=""FreeSql.Repository"" Version=""0.8.11"" />
        <PackageReference Include=""FreeSql.Provider.Sqlite"" Version=""0.8.11"" />{(isLazyLoading ? $"\r\n        <PackageReference Include=\"FreeSql.Extensions.LazyLoading\" Version=\"0.8.11\" />" : "")}
    </ItemGroup>

</Project>");
            #endregion
        }

        /// <summary>
        /// 获得控制器Controller代码
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public string GetControllerCode(Type entityType)
        {
            var tb = _fsql.CodeFirst.GetTableByEntity(entityType);
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
            foreach (var col in tb.Columns)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(col.Key)) continue;
                if (col.Value.CsType == typeof(string))
                    listKeyWhere += $" || a.{col.Key}.Contains(key)";

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
                        var treftb = _fsql.CodeFirst.GetTableByEntity(tref.RefEntityType);
                        foreach (var col in treftb.Columns)
                        {
                            if (treftb.ColumnsByCsIgnore.ContainsKey(col.Key)) continue;
                            if (col.Value.CsType == typeof(string))
                                listKeyWhere += $" || a.{prop.Key}.{col.Key}.Contains(key)";
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
                //关联 {tref.RefEntityType.Name}
                var mn_{prop.Name} = {mnNs}.Select((mn, idx) => new {tref.RefMiddleEntityType.Name} {{ {tref.MiddleColumns[tref.Columns.Count].CsName} = mn, {string.Join(", ", tref.Columns.Select((a, idx) => $"{tref.MiddleColumns[idx].CsName} = item.{a.CsName}"))} }}).ToArray();
                await ctx.AddRangeAsync(mn_{prop.Name});";
                            editFromFormEdit += $@"
                //关联 {tref.RefEntityType.Name}
                if ({mnNs} != null)
                {{
                    var {mnNs}_list = {mnNs}.ToList();
                    var oldlist = ctx.Set<{tref.RefMiddleEntityType.Name}>().Where(a => {string.Join(" && ", tref.Columns.Select((a, idx) => $"a.{tref.MiddleColumns[idx].CsName} == item.{a.CsName}"))}).ToList();
                    foreach (var olditem in oldlist)
                    {{
                        var idx = {mnNs}_list.FindIndex(a => a == olditem.{tref.MiddleColumns[tref.Columns.Count].CsName});
                        if (idx == -1) ctx.Remove(olditem);
                        else {mnNs}_list.RemoveAt(idx);
                    }}
                    var mn_{prop.Name} = {mnNs}_list.Select((mn, idx) => new {tref.RefMiddleEntityType.Name} {{ {tref.MiddleColumns[tref.Columns.Count].CsName} = mn, {string.Join(", ", tref.Columns.Select((a, idx) => $"{tref.MiddleColumns[idx].CsName} = item.{a.CsName}"))} }}).ToArray();
                    await ctx.AddRangeAsync(mn_{prop.Name});
                }}";
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

            delFromNew += $"{tb.Primarys[0].CsName}?.Select((a, idx) => new {entityType.Name} {{ ";
            foreach (var pk in tb.Primarys)
                delFromNew += $"{pk.CsName} = {pk.CsName}[idx], ";
            delFromNew = delFromNew.Remove(delFromNew.Length - 2) + " })";
            #endregion

            #region 拼接代码
            return $@"using {string.Join(";\r\nusing ", ns.Keys)};

namespace {_options.NameSpace}.Controllers
{{
    [Route(""{_options.ControllerRouteBase}[controller]"")]
    public class {entityType.Name}Controller : {_options.ControllerBase}
    {{
        IFreeSql fsql;
        public {entityType.Name}Controller(IFreeSql orm) {{
            fsql = orm;
        }}

        [HttpGet]
        async public Task<ActionResult> List([FromQuery] string key{listFromQuery}, [FromQuery] int limit = 20, [FromQuery] int page = 1)
        {{{listFromQueryMultiCombine}
            var select = fsql.Select<{entityType.Name}>(){listInclude}{(string.IsNullOrEmpty(listKeyWhere) ? "" : $"\r\n                .WhereIf(!string.IsNullOrEmpty(key), a => {listKeyWhere.Substring(4)})")}{listFromQuerySelect};
            var items = await select.Count(out var count).Page(page, limit).ToListAsync();
            ViewBag.items = items;
            ViewBag.count = count;
            return View();
        }}

        [HttpGet(""add"")]
        public ActionResult Edit() => View();

        [HttpGet(""edit"")]
        async public Task<ActionResult> Edit({string.Join(", ", tb.Primarys.Select(pk => $"[FromQuery] {pk.CsType.GetGenericName()} {pk.CsName}"))})
        {{
            var item = await fsql.Select<{entityType.Name}>(){editIncludeMany}.Where(a => {string.Join(" && ", tb.Primarys.Select(pk => $"a.{pk.CsName} == {pk.CsName}"))}).FirstAsync();
            if (item == null) return ApiResult.Failed.SetMessage(""记录不存在"");
            ViewBag.item = item;
            return View();
        }}

        /***************************************** POST *****************************************/

        [HttpPost(""add"")]
        [ValidateAntiForgeryToken]
        async public Task<ApiResult> _Add({string.Join(", ", tb.Columns.Values.Where(a => !a.Attribute.IsIgnore && !a.Attribute.IsIdentity && (!a.Attribute.IsPrimary || a.Attribute.IsPrimary && a.CsType.NullableTypeOrThis() != typeof(Guid))).Select(col => $"[FromForm] {col.CsType.GetGenericName()} {col.CsName}"))}{editFromForm})
        {{
            var item = new {entityType.Name}();
            {string.Join("\r\n            ", tb.Columns.Values.Where(a => !a.Attribute.IsIgnore && !a.Attribute.IsIdentity && (!a.Attribute.IsPrimary || a.Attribute.IsPrimary && a.CsType.NullableTypeOrThis() != typeof(Guid))).Select(col => $"item.{col.CsName} = {col.CsName};"))}
            using (var ctx = fsql.CreateDbContext())
            {{
                await ctx.AddAsync(item);{editFromFormAdd}
                await ctx.SaveChangesAsync();
            }}
            return ApiResult<object>.Success.SetData(item);
        }}

        [HttpPost(""edit"")]
        [ValidateAntiForgeryToken]
        async public Task<ApiResult> _Edit({string.Join(", ", tb.Columns.Values.Where(a => !a.Attribute.IsIgnore).Select(col => $"[FromForm] {col.CsType.GetGenericName()} {col.CsName}"))}{editFromForm})
        {{
            var item = new {entityType.Name}();
            {string.Join("\r\n            ", tb.Primarys.Select(col => $"item.{col.CsName} = {col.CsName};"))}
            using (var ctx = fsql.CreateDbContext())
            {{
                ctx.Attach(item);
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
            var affrows = await fsql.Delete<{entityType.Name}>().WhereDynamic(items).ExecuteAffrowsAsync();
            return ApiResult.Success.SetMessage($""更新成功，影响行数：{{affrows}}"");
        }}
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
            var tb = _fsql.CodeFirst.GetTableByEntity(entityType);
            if (tb == null) throw new Exception($"类型 {entityType.FullName} 错误，不能执行生成操作");

            #region THead Td
            var listTh = new StringBuilder();
            var listTd = new StringBuilder();
            listTd.Append($"\r\n								<td><input type=\"checkbox\" id=\"id\" name=\"id\" value=\"{string.Join(",", tb.Primarys.Select(pk => $"@item.{pk.CsName}"))}\" /></td>");

            var dicCol = new Dictionary<string, bool>();
            foreach (var col in tb.Primarys)
            {
                listTh.Append($"\r\n						<th scope=\"col\">{(col.Comment ?? col.CsName)}{(col.Attribute.IsIdentity ? "(自增)" : "")}</th>");
                listTd.Append($"\r\n								<td>@item.{col.CsName}</td>");
                dicCol.Add(col.CsName, true);
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
                        var tbref = _fsql.CodeFirst.GetTableByEntity(tref.RefEntityType);
                        var tbrefName = tbref.Columns.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault()?.CsName;
                        if (!string.IsNullOrEmpty(tbrefName)) tbrefName = $"?.{tbrefName}";
                        listTh.Append($"\r\n						<th scope=\"col\">{string.Join(",", tref.Columns.Select(a => a.Comment ?? a.CsName))}</th>");
                        listTd.Append($"\r\n								<td>[{string.Join(",", tref.Columns.Select(a => $"@item.{a.CsName}"))}] @item.{prop.Name}{tbrefName}</td>");
                        foreach (var col in tref.Columns) dicCol.Add(col.CsName, true);
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
            listTd.Append($"\r\n								<td><a href=\"./edit?{string.Join("&", tb.Primarys.Select(pk => $"{pk.CsName}=@item.{pk.CsName}"))}\">修改</a></td>");
            #endregion

            #region 多对一、多对多
            var selectCode = "";
            var fscCode = "";
            foreach (var prop in tb.Properties.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(prop.Name)) continue;
                var tref = tb.GetTableRef(prop.Name, false);
                if (tref == null) continue;

                var tbref = _fsql.CodeFirst.GetTableByEntity(tref.RefEntityType);
                var tbrefName = tbref.Columns.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault()?.CsName;
                if (!string.IsNullOrEmpty(tbrefName)) tbrefName = $".{tbrefName}";

                switch (tref.RefType)
                {
                    case TableRefType.ManyToOne:
                        selectCode += $"\r\n	var fk_{prop.Name}s = fsql.Select<{tref.RefEntityType.Name}>().ToList();";
                        fscCode += $"\r\n			{{ name: '{prop.Name}', field: '{string.Join(",", tref.Columns.Select(a => a.CsName))}', text: @Html.Json(fk_{prop.Name}s.Select(a => a{tbrefName})), value: @Html.Json(fk_{prop.Name}s.Select(a => {string.Join(" + \"|\" + ", tref.RefColumns.Select(a => "a." + a.CsName))})) }},";
                        break;
                    case TableRefType.ManyToMany:
                        selectCode += $"\r\n	var mn_{prop.Name} = fsql.Select<{tref.RefEntityType.Name}>().ToList();";
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
				<table id=""GridView1"" cellspacing=""0"" rules=""all"" border=""1"" style=""border-collapse:collapse;"" class=""table table-bordered table-hover"">
					<tr>
						<th scope=""col"" style=""width:2%;""><input type=""checkbox"" onclick=""$('#GridView1 tbody tr').each(function (idx, el) {{ var chk = $(el).find('td:first input[type=\'checkbox\']')[0]; if (chk) chk.checked = !chk.checked; }});"" /></th>
{listTh.ToString()}
						<th scope=""col"" style=""width:5%;"">&nbsp;</th>
					</tr>
					<tbody>
						@foreach({entityType.Name} item in ViewBag.items) {{
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
            var tb = _fsql.CodeFirst.GetTableByEntity(entityType);
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
                                <select name=""{col.CsName}""{(csType.GetCustomAttribute<FlagsAttribute>() != null ? $@" data-placeholder=""Select a {csType.Name}"" class=""form-control select2"" multiple>" : @"><option value="""">------ 请选择 ------</option>")}
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
                dicCol.Add(col.CsName, true);
            }

            var selectCode = "";
            foreach (var prop in tb.Properties.Values)
            {
                if (tb.ColumnsByCsIgnore.ContainsKey(prop.Name)) continue;
                var tref = tb.GetTableRef(prop.Name, false);
                if (tref == null) continue;

                var tbref = _fsql.CodeFirst.GetTableByEntity(tref.RefEntityType);
                var tbrefName = tbref.Columns.Values.Where(a => a.CsType == typeof(string)).FirstOrDefault()?.CsName;
                if (!string.IsNullOrEmpty(tbrefName)) tbrefName = $".{tbrefName}";

                switch (tref.RefType)
                {
                    case TableRefType.ManyToOne:
                        selectCode += $"\r\n	var fk_{prop.Name}s = fsql.Select<{tref.RefEntityType.Name}>().ToList();";
                        break;
                    case TableRefType.ManyToMany:
                        selectCode += $"\r\n	var mn_{prop.Name} = fsql.Select<{tref.RefEntityType.Name}>().ToList();";
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
                        foreach (var col in tref.Columns) dicCol.Add(col.CsName, true);
                        break;
                    case TableRefType.ManyToMany:
                        editTrMany.Append($@"
						<tr>
							<td>{prop.Name}</td>
							<td>
								<select name=""mn_{prop.Name}_{tref.RefColumns[0].CsName}"" data-placeholder=""Select a {tref.RefEntityType.Name}"" class=""form-control select2"" multiple>
									@foreach (var mn in mn_{prop.Name}) {{ <option value=""@mn.{tref.RefColumns[0].CsName}"">@mn{tbrefName}</option> }}
								</select>
							</td>
						</tr>");
                        editInitSelectUI.Append($@"item.mn_{prop.Name} = @Html.Json(item.{prop.Name});
			for (var a = 0; a < item.mn_{prop.Name}.length; a++) $(form.mn_{prop.Name}_{tref.RefColumns[0].CsName}).find('option[value=""{{0}}""]'.format(item.mn_{prop.Name}[a].{tref.RefColumns[0].CsName})).attr('selected', 'selected');");
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
	{entityType.Name} item = ViewBag.item;{selectCode}
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
