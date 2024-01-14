using FreeSql.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FreeSql.AdminLTE;

/// <summary>
/// 修改接口
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IEntityModified<TKey> where TKey : struct
{
	/// <summary>
	/// 修改者Id
	/// </summary>
	long? ModifiedUserId { get; set; }
	/// <summary>
	/// 修改者
	/// </summary>
	string ModifiedUserName { get; set; }
	/// <summary>
	/// 修改时间
	/// </summary>
	DateTime? ModifiedTime { get; set; }
}

/// <summary>
/// 实体修改
/// </summary>
public class EntityModified<TKey> : EntityCreated<TKey>, IEntityModified<TKey> where TKey : struct
{
    /// <summary>
    /// 修改者Id
    /// </summary>
    [Description("修改者Id")]
    [Column(Position = -12, CanInsert = false)]
    [JsonProperty(Order = 10000)]
    [JsonPropertyOrder(10000)]
    public virtual long? ModifiedUserId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    [Description("修改者")]
    [Column(Position = -11, CanInsert = false), MaxLength(50)]
    [JsonProperty(Order = 10001)]
    [JsonPropertyOrder(10001)]
    public virtual string ModifiedUserName { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [Description("修改时间")]
    [JsonProperty(Order = 10002)]
    [JsonPropertyOrder(10002)]
    [Column(Position = -10, CanInsert = false, ServerTime = DateTimeKind.Local)]
    public virtual DateTime? ModifiedTime { get; set; }
}

/// <summary>
/// 实体修改
/// </summary>
public class EntityModified : EntityModified<long>
{
}