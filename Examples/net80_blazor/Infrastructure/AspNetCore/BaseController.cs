using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Collections;
using System.Reflection;

[ServiceFilter(typeof(CustomExceptionFilter)), EnableCors("cors_all")]
public partial class BaseController : Controller {

    public string Ip => this.Request.Headers["X-Real-IP"].FirstOrDefault() ?? this.Request.HttpContext.Connection.RemoteIpAddress.ToString();
	public IConfiguration Configuration => (IConfiguration) HttpContext.RequestServices.GetService(typeof(IConfiguration));

    public override void OnActionExecuting(ActionExecutingContext context) {

		var contoller = (context.ActionDescriptor as ControllerActionDescriptor).ControllerTypeInfo;
		var method = (context.ActionDescriptor as ControllerActionDescriptor).MethodInfo;
		var isAnonymous = method.GetCustomAttribute<AnonymousAttribute>() != null || contoller.GetCustomAttribute<AnonymousAttribute>() != null;
		var isMustLogin = method.GetCustomAttribute<MustLoginAttribute>() != null || contoller.GetCustomAttribute<MustLoginAttribute>() != null;
		var isAdmin = method.GetCustomAttribute<MustAdminAttribute>() != null || contoller.GetCustomAttribute<MustAdminAttribute>() != null;

		string token = Request.Headers["token"].FirstOrDefault() ?? Request.Query["token"].FirstOrDefault() ?? Request.Cookies["token"];
		if (string.IsNullOrEmpty(token) && isAdmin) token = Request.Cookies["admintoken"];
		if (!string.IsNullOrEmpty(token)) {
			try {
				//this.LoginUser = WebHelper.GetMemberByToken(token);
			} catch {
				context.Result = ApiResult.登陆TOKEN失效_请重新登陆;
				return;
			}
		}

		//if (isAdmin && this.LoginUser != null && this.LoginUser.Name != "plaisir001") this.LoginUser = null;

		//var test001 = 1;
		//if (isAnonymous) test001++;
		//else if (this.LoginUser != null) test001++;
		//else {
		//	context.Result = ApiResult.拒绝访问;
		//	return;
		//}

		base.OnActionExecuting(context);
	}
	public override void OnActionExecuted(ActionExecutedContext context) {
		base.OnActionExecuted(context);
	}

	#region 角色权限验证
	//public bool sysrole_check(string url) {
	//	url = url.ToLower();
	//	//Response.Write(url + "<br>");
	//	if (url == "/" || url.IndexOf("/default.aspx") == 0) return true;
	//	foreach(var role in this.LoginUser.Obj_sysroles) {
	//		//Response.Write(role.ToString());
	//		foreach(var dir in role.Obj_sysdirs) {
	//			//Response.Write("-----------------" + dir.ToString() + "<br>");
	//			string tmp = dir.Url;
	//			if (tmp.EndsWith("/")) tmp += "default.aspx";
	//			if (url.IndexOf(tmp) == 0) return true;
	//		}
	//	}
	//	return false;
	//}
	#endregion
}

#region MustLogin、Anonymous、MustAdmin
public partial class MustLoginAttribute : Attribute { }
public partial class AnonymousAttribute : Attribute { }
public partial class MustAdminAttribute : Attribute { }
#endregion

#region ApiResult
[JsonObject(MemberSerialization.OptIn)]
public partial class ApiResult : ContentResult {
	[JsonProperty("code")] public int Code { get; protected set; }
	[JsonProperty("message")] public string Message { get; protected set; }
	[JsonProperty("data")] public Hashtable Data { get; protected set; } = new Hashtable();

	public ApiResult() { }
	public ApiResult(int code) { this.SetCode(code); }
	public ApiResult(string message) { this.SetMessage(message); }
	public ApiResult(int code, string message, params object[] data) { this.SetCode(code).SetMessage(message).AppendData(data); }

	public ApiResult SetCode(int value) { this.Code = value;  return this; }
	public ApiResult SetMessage(string value) { this.Message = value;  return this; }
	public ApiResult SetData(params object[] value) {
		this.Data.Clear();
		return this.AppendData(value);
	}
	public ApiResult AppendData(params object[] value) {
		if (value == null || value.Length < 2 || value[0] == null) return this;
		for (int a = 0; a < value.Length; a += 2) {
			if (value[a] == null) continue;
			this.Data[value[a]] = a + 1 < value.Length ? value[a + 1] : null;
		}
		return this;
	}
	#region form 表单 target=iframe 提交回调处理
	private void Jsonp(ActionContext context) {
		string __callback = context.HttpContext.Request.HasFormContentType ? context.HttpContext.Request.Form["__callback"].ToString() : null;
		if (string.IsNullOrEmpty(__callback)) {
			this.ContentType = "text/json;charset=utf-8;";
			this.Content = JsonConvert.SerializeObject(this);
		}else {
			this.ContentType = "text/html;charset=utf-8";
			this.Content = $"<script>top.{__callback}({GlobalExtensions.Json(null, this)});</script>";
		}
	}
	public override void ExecuteResult(ActionContext context) {
		Jsonp(context);
		base.ExecuteResult(context);
	}
	public override Task ExecuteResultAsync(ActionContext context) {
		Jsonp(context);
		return base.ExecuteResultAsync(context);
	}
	#endregion

	public static ApiResult Success { get { return new ApiResult(0, "success"); } }
	public static ApiResult Failed { get { return new ApiResult(99, "fail"); } }
	public static ApiResult 参数格式错误 { get { return new ApiResult(97, "parameter format error"); } }
	public static ApiResult 拒绝访问 => new ApiResult(5001, "access denied");
    public static ApiResult 登陆TOKEN失效_请重新登陆 => new ApiResult(5009, "Invalid TOKEN");
}
[JsonObject(MemberSerialization.OptIn)]
public partial class ApiResult<T> : ApiResult
{
    [JsonProperty("data")] public new T Data { get; protected set; }

    public ApiResult() {{ }}
    public ApiResult(int code) => this.SetCode(code);
    public ApiResult(string message) => this.SetMessage(message);
    public ApiResult(int code, string message) => this.SetCode(code).SetMessage(message);

    new public ApiResult<T> SetCode(int value) {{ this.Code = value; return this; }}
    public ApiResult<T> SetCode(Enum value) {{ this.Code = Convert.ToInt32(value); this.Message = value.ToString(); return this; }}
    new public ApiResult<T> SetMessage(string value) {{ this.Message = value; return this; }}
    public ApiResult<T> SetData(T value) {{ this.Data = value; return this; }}

    new public static ApiResult<T> Success => new ApiResult<T>(0, "成功");
}
#endregion
