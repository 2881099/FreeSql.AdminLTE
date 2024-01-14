using Microsoft.AspNetCore.Mvc.Filters;

public class CustomExceptionFilter : Attribute, IExceptionFilter {
	private ILogger _logger = null;
	private IConfiguration _cfg = null;

	public CustomExceptionFilter (ILogger<CustomExceptionFilter> logger, IConfiguration cfg) {
		_logger = logger;
		_cfg = cfg;
	}

	public void OnException(ExceptionContext context) {
		//在这里记录错误日志，context.Exception 为异常对象
		context.Result = ApiResult.Failed.SetMessage(context.Exception.Message); //返回给调用方
		var innerLog = context.Exception.InnerException != null ? $" \r\n{context.Exception.InnerException.Message} \r\n{ context.Exception.InnerException.StackTrace}" : "";
		_logger.LogError($"=============错误：{context.Exception.Message} \r\n{context.Exception.StackTrace}{innerLog}");
		context.ExceptionHandled = true;
	}
}