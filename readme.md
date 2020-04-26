这是 [FreeSql](https://github.com/2881099/FreeSql) 衍生出来的功能包，基于 AdminLTE 前端框架提高生产效率（QQ群：4336577）

| 项目 | 版本 |
| -- | -- |
| FreeSql.AdminLTE | netstandard2.0、net45 |
| FreeSql.AdminLTE.Tools | netcoreapp3.1 |
| FreeSql.AdminLTE.Preview | netcoreapp3.1 |

三个包产生的 AdminLTE 功能几乎一样，都是根据实体类、导航关系生成默认的繁琐的后台管理功能。

生成条件：

- 实体类的注释（请开启项目XML文档）；
- 实体类的导航属性配置（可生成繁琐的常用后台管理功能）。

# 1、FreeSql.AdminLTE.Preview

.NETCore MVC 中间件，基于 AdminLTE 前端框架动态产生指定 FreeSql 实体的增删查改的【预览管理功能】。

使用场景：开发环境的测试数据生产。

> dotnet add package FreeSql.AdminLTE.Preview

```csharp
public void ConfigureServices(IServiceCollection services)
{
	services.AddSingleton<IFreeSql>(fsql);
}

public void Configure(IApplicationBuilder app)
{
	app.UseFreeAdminLtePreview("/testadmin/",
		typeof(TestDemo01.Entitys.Song),
		typeof(TestDemo01.Entitys.Tag));
}
```

![image](https://user-images.githubusercontent.com/16286519/56229638-f3a79b80-60ac-11e9-8cf6-e58e95ab53c1.png)

![image](https://user-images.githubusercontent.com/16286519/56298417-ad157800-6164-11e9-86c1-6270f3989487.png)

# 2、FreeSql.AdminLTE

根据 FreeSql 实体类配置、导航关系配置，快速生成基于 MVC + Razor + AdminLTE 的后台管理系统的增删查改代码【支持二次开发】。

使用场景：asp.net/asp.netcore 后台管理系统快速生成，二次开发【自定义】。

> dotnet add package FreeSql.AdminLTE

```csharp
using (var gen = new FreeSql.AdminLTE.Generator(new GeneratorOptions()))
{
    gen.Build("d:/test/", new[] { typeof(TestDemo01.Entitys.Song) }, false);
}
```

提醒：提醒：提醒：

> 生成后的 Controller、Razor 代码依赖 FreeSql.DbContext 库，请手工添加

# 3、FreeSql.AdminLTE.Tools

对 FreeSql.AdminLTE 功能的工具命令化封装，命令行快速生成代码。

使用场景：asp.netcore 后台管理系统快速生成，二次开发。

> dotnet tool install -g FreeSql.AdminLTE.Tools

--- 

进入后台项目（可以是空项目、或已存在的项目），执行以下命令

> FreeSql.AdminLTE.Tools -Find MyTest\.Model\..+

| 命令行参数 | 说明 |
| -- | -- |
| -Find                | * 匹配实体类FullName的正则表达式                           |
| -ControllerNameSpace | 控制器命名空间（默认：FreeSql.AdminLTE）                   |
| -ControllerRouteBase | 控制器请求路径前辍（默认：/AdminLTE/）                     |
| -ControllerBase      | 控制器基类（默认：Controller）                             |
| -First               | 是否生成 ApiResult.cs、index.html、htm 静态资源（首次生成）|
| -Output              | 输出路径（默认：当前目录）                                 |

![image](https://user-images.githubusercontent.com/16286519/64080898-ca21a080-cd2b-11e9-92e0-fcda5058c5e7.png)