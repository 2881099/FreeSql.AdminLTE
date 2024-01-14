using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class StringExtension
{
    /// <summary>
    /// 判断字符串是否为Null、空
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsNull(this string s)
    {
        return string.IsNullOrWhiteSpace(s);
    }

    /// <summary>
    /// 与字符串进行比较，忽略大小写
    /// </summary>
    /// <param name="s"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool EqualsIgnoreCase(this string s, string value)
    {
        return s.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 首字母转小写
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string FirstCharToLower(this string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        string str = s.First().ToString().ToLower() + s.Substring(1);
        return str;
    }

    /// <summary>
    /// 首字母转大写
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string FirstCharToUpper(this string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        string str = s.First().ToString().ToUpper() + s.Substring(1);
        return str;
    }

    /// <summary>
    /// 转为Base64，UTF-8格式
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string ToBase64(this string s)
    {
        return s.ToBase64(Encoding.UTF8);
    }

    /// <summary>
    /// 转为Base64
    /// </summary>
    /// <param name="s"></param>
    /// <param name="encoding">编码</param>
    /// <returns></returns>
    public static string ToBase64(this string s, Encoding encoding)
    {
        if (s.IsNull()) return string.Empty;

        var bytes = encoding.GetBytes(s);
        return Convert.ToBase64String(bytes);
    }

	/// <summary>
	/// 转换为16进制
	/// </summary>
	/// <param name="bytes"></param>
	/// <param name="lowerCase">是否小写</param>
	/// <returns></returns>
	public static string ToHex(this byte[] bytes, bool lowerCase = true)
	{
		if (bytes == null)
			return null;

		var result = new StringBuilder();
		var format = lowerCase ? "x2" : "X2";
		for (var i = 0; i < bytes.Length; i++)
		{
			result.Append(bytes[i].ToString(format));
		}

		return result.ToString();
	}

	/// <summary>
	/// 16进制转字节数组
	/// </summary>
	/// <param name="s"></param>
	/// <returns></returns>
	public static byte[] HexToBytes(this string s)
	{
		if (s.IsNull())
			return null;
		var bytes = new byte[s.Length / 2];

		for (int x = 0; x < s.Length / 2; x++)
		{
			int i = (Convert.ToInt32(s.Substring(x * 2, 2), 16));
			bytes[x] = (byte)i;
		}

		return bytes;
	}

	public static string ToMask(this string s)
    {
        if (s.IsNull()) return string.Empty;

        return Regex.Replace(
            Regex.Replace(
            Regex.Replace(s, @"(\d{3})\d{4}(\d{4})", m => $"{m.Groups[1]?.Value}****{m.Groups[2]?.Value}"),
            "(?<=.{2})[^@]+(?=.{2}@)", "****"),
            @"(\d{1,3})\.\d{1,3}\.\d{1,3}\.\d{1,3}", m => $"{m.Groups[1]?.Value}.*.*.{m.Groups[4]?.Value}");
    }

	private static readonly string _chars = "0123456789";
	private static readonly char[] _constant = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
	/// <summary>
	/// 生成随机字符串，默认32位
	/// </summary>
	/// <param name="length">随机数长度</param>
	/// <returns></returns>
	public static string GenerateRandom(this string _, int length = 32)
	{
		var newRandom = new StringBuilder();
		var rd = new Random();
		for (int i = 0; i < length; i++)
		{
			newRandom.Append(_constant[rd.Next(_constant.Length)]);
		}
		return newRandom.ToString();
	}

	/// <summary>
	/// 生成随机6位数
	/// </summary>
	/// <param name="length"></param>
	/// <returns></returns>
	public static string GenerateRandomNumber(this string _, int length = 6)
	{
		var random = new Random();
		return new string(Enumerable.Repeat(_chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
	}
}