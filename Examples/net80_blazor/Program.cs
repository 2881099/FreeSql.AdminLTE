using FreeSql;
using net80_blazor.Admin;
using net80_blazor.Entities;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using Yitter.IdGenerator;

var builder = WebApplication.CreateBuilder(args);

Func<IServiceProvider, IFreeSql> fsqlFactory = r =>
{
	IFreeSql fsql = new FreeSql.FreeSqlBuilder()
		.UseConnectionString(FreeSql.DataType.Sqlite, @"Data Source=freedb.db")
		.UseMonitorCommand(cmd => Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {cmd.CommandText}\r\n"))//����SQL���
		.UseNoneCommandParameter(true)
		.UseAutoSyncStructure(true) //�Զ�ͬ��ʵ��ṹ�����ݿ⣬FreeSql����ɨ����򼯣�ֻ��CRUDʱ�Ż����ɱ�
		.Build();

	YitIdHelper.SetIdGenerator(new IdGeneratorOptions(1) { WorkerIdBitLength = 6 });
	var serverTime = fsql.Ado.QuerySingle(() => DateTime.UtcNow);
	var timeOffset = DateTime.UtcNow.Subtract(serverTime);
	fsql.Aop.AuditValue += (_, e) =>
	{
		//���ݿ�ʱ��
		if ((e.Column.CsType == typeof(DateTime) || e.Column.CsType == typeof(DateTime?))
			&& e.Column.Attribute.ServerTime != DateTimeKind.Unspecified
			&& (e.Value == null || (DateTime)e.Value == default || (DateTime?)e.Value == default))
		{
			e.Value = (e.Column.Attribute.ServerTime == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now).Subtract(timeOffset);
		}

		//ѩ��Id
		if (e.Column.CsType == typeof(long)
			&& e.Property.GetCustomAttribute<SnowflakeAttribute>(false) != null
			&& (e.Value == null || (long)e.Value == default || (long?)e.Value == default))
		{
			e.Value = YitIdHelper.NextId();
		}

		//if (user == null || user.Id <= 0)
		//{
		//    return;
		//}

		//if (e.AuditValueType is AuditValueType.Insert or AuditValueType.InsertOrUpdate)
		//{
		//    switch (e.Property.Name)
		//    {
		//        case "CreatedUserId":
		//        case "OwnerId":
		//        case "MemberId":
		//            if (e.Value == null || (long)e.Value == default || (long?)e.Value == default)
		//            {
		//                e.Value = user.Id;
		//            }
		//            break;

		//        case "CreatedUserName":
		//            if (e.Value == null || ((string)e.Value).IsNull())
		//            {
		//                e.Value = user.UserName;
		//            }
		//            break;
		//    }
		//}

		//if (e.AuditValueType is AuditValueType.Update or AuditValueType.InsertOrUpdate)
		//{
		//    switch (e.Property.Name)
		//    {
		//        case "ModifiedUserId":
		//            e.Value = user.Id;
		//            break;

		//        case "ModifiedUserName":
		//            e.Value = user.UserName;
		//            break;
		//    }
		//}
	};
	if (fsql.Select<MenuEntity>().Any() == false)
	{
		var repo = fsql.GetRepository<MenuEntity>();
		repo.DbContextOptions.EnableCascadeSave = true;
		repo.Insert(new[]
		{
			new MenuEntity
			{
				Label = "ͨ�ù���",
				ParentId = 0,
				Path = "",
				Sort = 100,
				TargetBlank = false,
				Icon = "",
				Type = MenuEntityType.�˵�,
				Childs = new List<MenuEntity>
				{
					new MenuEntity
					{
						Label = "�˵�����",
						Path = "Admin/Menu",
						Sort = 101,
						TargetBlank = false,
						Icon = "",
						Type = MenuEntityType.�˵�,
					},
					new MenuEntity
					{
						Label = "��ɫ����",
						Path = "Admin/Role",
						Sort = 102,
						TargetBlank = false,
						Icon = "",
						Type = MenuEntityType.�˵�,
					},
					new MenuEntity
					{
						Label = "�û�����",
						Path = "Admin/User",
						Sort = 103,
						TargetBlank = false,
						Icon = "",
						Type = MenuEntityType.�˵�,
					}
				}
			},
		});
	}

	return fsql;
};
builder.Services.AddSingleton(fsqlFactory);
builder.Services.AddScoped<UnitOfWorkManager>();
builder.Services.AddFreeRepository(null, typeof(Program).Assembly);

builder.Services.AddControllersWithViews()
	//.AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); })
	.AddNewtonsoftJson(options =>
	{
		//options.SerializerSettings.Converters.Add(new StringEnumConverter());
		//options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
		options.SerializerSettings.Converters.Add(new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
		//options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
		//���������һ������,��ʹ���շ���ʽ��key
		options.SerializerSettings.ContractResolver = new DefaultContractResolver();
	});
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<CustomExceptionFilter>();
builder.Services.AddCors(options => options.AddPolicy("cors_all", builder => builder
	.AllowAnyHeader()
	.AllowAnyMethod()
	.AllowAnyOrigin()
));

var app = builder.Build();
var applicationLifeTime = app.Services.GetService<IHostApplicationLifetime>();

app.Use(async (context, next) =>
{
	TransactionalAttribute.SetServiceProvider(context.RequestServices);
	await next();
});

app.UseCors("cors_all");
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseDefaultFiles().UseStaticFiles();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
