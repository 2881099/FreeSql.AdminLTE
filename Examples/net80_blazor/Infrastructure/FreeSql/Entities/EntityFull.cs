namespace FreeSql.AdminLTE;

/// <summary>
/// 实体基类
/// </summary>
public class EntityFull<TKey> : EntitySoftDelete<TKey> where TKey : struct
{
}

/// <summary>
/// 实体基类
/// </summary>
public class EntityFull : EntityFull<long>
{
}