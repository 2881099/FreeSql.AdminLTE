
using System.ComponentModel;

namespace FreeSql.AdminLTE.Infrastructure.Files;

public enum FileSizeUnit
{
	/// <summary>
	/// 字节
	/// </summary>
	[Description("B")]
	Byte,

	/// <summary>
	/// K字节
	/// </summary>
	[Description("KB")]
	K,

	/// <summary>
	/// M字节
	/// </summary>
	[Description("MB")]
	M,

	/// <summary>
	/// G字节
	/// </summary>
	[Description("GB")]
	G
}
/// <summary>
/// 文件大小
/// </summary>
public struct FileSize
{
    /// <summary>
    /// 初始化文件大小
    /// </summary>
    /// <param name="size">文件大小</param>
    /// <param name="unit">文件大小单位</param>
    public FileSize(long size, FileSizeUnit unit = FileSizeUnit.Byte)
    {
        switch (unit)
        {
            case FileSizeUnit.K:
                Size = size * 1024; break;
            case FileSizeUnit.M:
                Size = size * 1024 * 1024; break;
            case FileSizeUnit.G:
                Size = size * 1024 * 1024 * 1024; break;
            default:
                Size = size; break;
        }
    }

    /// <summary>
    /// 文件字节长度
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// 获取文件大小，单位：字节
    /// </summary>
    public long GetSize()
    {
        return Size;
    }

    /// <summary>
    /// 获取文件大小，单位：K
    /// </summary>
    public double GetSizeByK()
    {
        return Math.Round(Size / 1024.0, 2);
    }

    /// <summary>
    /// 获取文件大小，单位：M
    /// </summary>
    public double GetSizeByM()
    {
        return Math.Round(Size / 1024.0 / 1024.0, 2);
    }

    /// <summary>
    /// 获取文件大小，单位：G
    /// </summary>
    public double GetSizeByG()
    {
        return Math.Round(Size / 1024.0 / 1024.0 / 1024.0, 2);
    }

    /// <summary>
    /// 输出描述
    /// </summary>
    public override string ToString()
    {
        if (Size >= 1024 * 1024 * 1024)
            return $"{GetSizeByG()} {FileSizeUnit.G.ToDescription()}";
        if (Size >= 1024 * 1024)
            return $"{GetSizeByM()} {FileSizeUnit.M.ToDescription()}";
        if (Size >= 1024)
            return $"{GetSizeByK()} {FileSizeUnit.K.ToDescription()}";
        return $"{Size} {FileSizeUnit.Byte.ToDescription()}";
    }
}