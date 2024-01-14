using FreeSql;
using FreeSql.DataAnnotations;
using FreeSql.Internal.CommonProvider;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Web;

public static class GlobalExtensions {

	public static object Json(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper html, object obj) {
		string str = JsonConvert.SerializeObject(obj);
		if (!string.IsNullOrEmpty(str)) str = Regex.Replace(str, @"<(/?script[\s>])", "<\"+\"$1", RegexOptions.IgnoreCase);
		if (html == null) return str;
		return html.Raw(str);
	}

	/// <summary>
	/// 获取时间戳（秒），按1970-1-1
	/// </summary>
	/// <param name="time"></param>
	/// <returns></returns>
	public static long GetTimeSeconds(this DateTime time) {
		return (long)time.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
	}

	public static bool IsCoinbaseAddress(this string address) {
		if (string.IsNullOrEmpty(address)) return false;
		return Regex.IsMatch(address, @"0x\w{32,64}");
	}

	public static string HtmlLimit(this string str, int len)
	{
		if (str?.Length > len) return str?.Substring(0, len);
		return str;
	}
}
