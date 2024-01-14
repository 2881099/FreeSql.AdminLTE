using FreeSql.DataAnnotations;
using System.ComponentModel;

namespace FreeSql.AdminLTE;

public interface IEntitySoftDelete
{
	/// <summary>
	/// 是否删除
	/// </summary>
	bool IsDeleted { get; set; }
}

/// <summary>
/// 实体删除
/// </summary>
public class EntitySoftDelete<TKey> : EntityModified<TKey>, IEntitySoftDelete where TKey : struct
{
    /// <summary>
    /// 是否删除
    /// </summary>
    [Description("是否删除")]
    [Column(Position = -9)]
    public virtual bool IsDeleted { get; set; } = false;
}

/// <summary>
/// 实体删除
/// </summary>
public class EntityDelete : EntitySoftDelete<long>
{
}