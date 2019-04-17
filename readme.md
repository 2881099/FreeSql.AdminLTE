这是 [FreeSql](https://github.com/2881099/FreeSql) 衍生出来的 .NETCore MVC 中间件扩展包，基于 AdminLTE 前端框架动态产生 FreeSql 实体的增删查改界面（QQ群：4336577）。

> dotnet add package FreeSql.AdminLTE

## 更新日志

### v0.5.1.1

- 实现基本的查询列表、添加数据、修改数据功能；
- 实现 ManyToOne/ManyToMany 过滤查询功能；
- 实现 添加/修改数据时，ManyToMany 的联级保存；
- 实现 批量删除数据；

## 快速开始

```csharp

public void ConfigureServices(IServiceCollection services) {
	services.AddSingleton<IFreeSql>(fsql);
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
	app.UseFreeAdminLTE("/testadmin/",
		typeof(TestDemo01.Entitys.Song),
		typeof(TestDemo01.Entitys.Tag));
}
```

![image](https://user-images.githubusercontent.com/16286519/56229638-f3a79b80-60ac-11e9-8cf6-e58e95ab53c1.png)

![image](https://user-images.githubusercontent.com/16286519/56298417-ad157800-6164-11e9-86c1-6270f3989487.png)

## IFreeSql 核心定义

```csharp
var fsql = new FreeSql.FreeSqlBuilder()
    .UseConnectionString(FreeSql.DataType.Sqlite, @"Data Source=|DataDirectory|/dd2.db;Pooling=true;Max Pool Size=10")
    .UseAutoSyncStructure(true)
    .UseNoneCommandParameter(true)

    .UseMonitorCommand(cmd => Trace.WriteLine(cmd.CommandText))
    .Build();

public class Song {
    [Column(IsIdentity = true)]
    public int Id { get; set; }
    public string BigNumber { get; set; }

    [Column(IsVersion = true)] //乐观锁
    public long versionRow { get; set; }
}
public class Tag {
    [Column(IsIdentity = true)]
    public int Id { get; set; }

    public int? Parent_id { get; set; }
    public virtual Tag Parent { get; set; }

    public string Name { get; set; }

    public virtual ICollection<Tag> Tags { get; set; }
}

public class SongContext : DbContext {
    public DbSet<Song> Songs { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder builder) {
        builder.UseFreeSql(fsql);
    }
}
```
