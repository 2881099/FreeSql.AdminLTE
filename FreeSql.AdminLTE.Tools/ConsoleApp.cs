using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Drawing;
using Console = Colorful.Console;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;

namespace FreeSql.AdminLTE.Tools
{
    public class ConsoleApp {

        public GeneratorOptions ArgsOptions = new GeneratorOptions();
        public string ArgsFind;
        public string ArgsOutput;
        public string ArgsCode;
        public bool ArgsFirst;

        public ConsoleApp(string[] args, ManualResetEvent wait) {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var gb2312 = Encoding.GetEncoding("GB2312");
            if (gb2312 != null)
            {
                try
                {
                    Console.OutputEncoding = gb2312;
                    Console.InputEncoding = gb2312;
                }
                catch { }
            }

            //var ntjson = Assembly.LoadFile(@"C:\Users\28810\Desktop\testfreesql\bin\Debug\netcoreapp2.2\publish\testfreesql.dll");

            //using (var gen = new Generator(new GeneratorOptions()))
            //{
            //    gen.TraceLog = log => Console.WriteFormatted(log + "\r\n", Color.DarkGray);
            //    gen.Build(ArgsOutput, new[] { typeof(ojbk.Entities.AuthRole) }, false);
            //}

            Console.WriteAscii(" FreeSql", Color.Violet);
            Console.WriteFormatted(@"
  # Github # {0}

", Color.SlateGray,
new Colorful.Formatter("https://github.com/2881099/FreeSql", Color.DeepSkyBlue));

            this.ArgsOutput = Directory.GetCurrentDirectory();
			string args0 = args[0].Trim().ToLower();
			if (args[0] == "?" || args0 == "--help" || args0 == "-help") {
                
                Console.WriteFormatted(@"
    {0}

  # 生成条件 #

    {1}
    {2}

  # 快速开始 #

    > {3} {4} MyTest\.Model\..+

        -Find                  * 匹配实体类FullName的正则表达式

        -ControllerNameSpace   控制器命名空间（默认：FreeSql.AdminLTE）
        -ControllerRouteBase   控制器请求路径前辍（默认：/AdminLTE/）
        -ControllerBase        控制器基类（默认：Controller）

        -Code                  生成前执行代码，解决FluentAPI设置的特性无法读取（gen.Orm）
        -First                 是否生成 ApiResult.cs、index.html、htm 静态资源（首次生成）
        -Output                输出路径（默认：当前目录）

  # 生成到其他目录 #

    > {3} {4} MyTest\.Model\..+ -Output d:/test

  # 生成BaseEntity实体类

    > {3} {4} MyTest\.Model\..+ -Code ""entityTypes.FirstOrDefault()?.Assembly.GetType(\""FreeSql.BaseEntity\"").GetMethod(\""Initialization\"").Invoke(null, new object[] { gen.Orm });""

", Color.SlateGray,
new Colorful.Formatter("基于 .NETCore 2.1 环境，在控制台当前目录的项目下，根据实体类生成 AdminLTE 后台管理功能的相关文件。", Color.SlateGray),
new Colorful.Formatter("1、实体类的注释（请开启项目XML文档）；", Color.SlateGray),
new Colorful.Formatter("2、实体类的导航属性配置（可生成繁琐的常用后台管理功能）。", Color.SlateGray),
new Colorful.Formatter("FreeSql.AdminLTE.Tools", Color.White),
new Colorful.Formatter("-Find", Color.ForestGreen)
);

				return;
			}
			for (int a = 0; a < args.Length; a++) {
                switch (args[a]) {
                    case "-Find":
                        ArgsFind = args[a + 1];
                        a++;
                        break;

                    case "-ControllerNameSpace":
                        ArgsOptions.ControllerNameSpace = args[a + 1];
                        a++;
                        break;
                    case "-ControllerRouteBase":
                        ArgsOptions.ControllerRouteBase = args[a + 1];
                        a++;
                        break;
                    case "-ControllerBase":
                        ArgsOptions.ControllerBase = args[a + 1];
                        a++;
                        break;

                    case "-Code":
                        ArgsCode = args[a + 1];
                        a++;
                        break;
                    case "-First":
                        ArgsFirst = true;
                        break;
                    case "-Output":
                        ArgsOutput = args[a + 1];
                        a++;
                        break;
                }
			}
            if (string.IsNullOrEmpty(ArgsFind))
                throw new ArgumentException("-Find 参数不能为空");
            Regex findExp = null;
            try
            {
                findExp = new Regex(ArgsFind, RegexOptions.Compiled);
            }
            catch
            {
                throw new ArgumentException($"-Find 参数值不是有效的正式表达式，当前值：{ArgsFind}");
            }

            Console.WriteFormatted($@"
-Find={ArgsFind}
-ControllerNameSpace={ArgsOptions.ControllerNameSpace}
-ControllerRouteBase={ArgsOptions.ControllerRouteBase}
-ControllerBase={ArgsOptions.ControllerBase}
-Code={ArgsCode}
-First={ArgsFirst}
-Output={ArgsOutput}

", Color.DarkGray);

            var dllFiles = new List<string>();
            Console.WriteFormatted("正在发布当前项目(dotnet publish -r linux-x64) …\r\n", Color.DarkGreen);
            var shellret = ShellRun(null, "dotnet publish -r linux-x64");
            if (!string.IsNullOrEmpty(shellret.err))
            {
                Console.WriteFormatted(shellret.err + "\r\n\r\n", Color.Red);
                return;
            }
            if (!string.IsNullOrEmpty(shellret.info))
            {
                Console.WriteFormatted(shellret.info + "\r\n\r\n", Color.DarkGray);
                if (int.TryParse(Regex.Match(shellret.info, @"(\d+) 个错误").Groups[1].Value, out var tryint) && tryint > 0)
                    return;
            }

            var lines = shellret.info.Split('\n');
            var publishDir = lines.Where(a => a.Contains(" -> ") && a.TrimEnd('/', '\\').EndsWith("publish")).Select(a => a.Substring(a.IndexOf(" -> ") + 4).Trim()).LastOrDefault();
            dllFiles.AddRange(lines.Where(a => a.Contains(" -> ") && a.Trim().EndsWith(".dll")).Select(a => publishDir + a.Trim().Split('/', '\\').LastOrDefault()));

            Console.WriteFormatted("正在创建临时项目 …\r\n", Color.DarkGreen);
            var tmpdir = Path.Combine(Path.GetTempPath(), "temp_freesql_adminlte_tools");
            Action<string, string> writeTmpFile = (path, content) =>
            {
                var filename = $"{tmpdir}/{path.TrimStart('/', '\\')}";
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    sw.Write(content);
                    sw.Close();
                }
                Console.WriteFormatted($"OUT -> {filename}\r\n", Color.DarkGray);
            };

            var currentCsproj = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj").FirstOrDefault();
            var currentVersion = "netcoreapp3.1";
            if (!string.IsNullOrEmpty(currentCsproj)) currentVersion = Regex.Match(File.ReadAllText(currentCsproj), @"netcoreapp\d+\.\d+")?.Groups[0].Value;
            if (string.IsNullOrEmpty(currentVersion)) currentVersion = "netcoreapp3.1";
            writeTmpFile("temp_freesql_adminlte_tools.csproj", $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{currentVersion}</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""FreeSql.Provider.Sqlite"" Version=""1.8.1"" />
    <PackageReference Include=""FreeSql.AdminLTE"" Version=""1.8.1"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""{currentCsproj}"" />
  </ItemGroup>
</Project>
");
            writeTmpFile("Program.cs", $@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace temp_freesql_adminlte_tools
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            var findRegexp = new Regex(@""{ArgsFind.Replace("\"", "\"\"")}"", RegexOptions.Compiled);
            var entityTypes = new List<Type>();
            foreach (var dllFile in new [] {{ @""{string.Join("\", @\"", dllFiles.Select(a => a.Replace("\"", "\"\"")))}"" }}) {{
                var assembly = Assembly.LoadFrom(dllFile);
                var alltypes = assembly.ExportedTypes;//.GetTypes();
                foreach (var type in alltypes)
                {{
                    if (type.IsClass && !type.IsInterface && !type.IsAbstract && !type.IsAnonymousType() && 
                        !type.IsEnum && !type.IsGenericType && !type.IsImport && 
                        !type.IsNested && !type.IsSealed && !type.IsValueType && type.IsVisible &&
                        findRegexp.IsMatch(type.FullName))
                    {{
                        Console.WriteLine($""READY -> {{type.FullName}}"");
                        entityTypes.Add(type);
                    }}
                }}
            }}

            using (var gen = new FreeSql.AdminLTE.Generator(new FreeSql.AdminLTE.GeneratorOptions
            {{
                ControllerBase = @""{ArgsOptions.ControllerBase.Replace("\"", "\"\"")}"",
                ControllerNameSpace = @""{ArgsOptions.ControllerNameSpace.Replace("\"", "\"\"")}"",
                ControllerRouteBase = @""{ArgsOptions.ControllerRouteBase.Replace("\"", "\"\"")}""
            }}))
            {{
                {(string.IsNullOrEmpty(ArgsCode) ? "" : $"{ArgsCode}")}
                gen.TraceLog = log => Console.WriteLine(log);
                gen.Build(@""{ArgsOutput.Replace("\"", "\"\"")}"", entityTypes.ToArray(), {(ArgsFirst ? "true" : "false")});
            }}
            Console.Write($""--freesql_adminlte_tools_success--"");
        }}
    }}
}}
");

            Console.WriteFormatted("\r\n正在运行生成程序 …\r\n", Color.DarkGreen);
            shellret = ShellRun(tmpdir, "dotnet run");
            if (!string.IsNullOrEmpty(shellret.err))
            {
                Console.WriteFormatted(shellret.err + "\r\n\r\n", Color.Red);
                return;
            }
            if (!string.IsNullOrEmpty(shellret.info))
            {
                if (!shellret.info.Trim().EndsWith("--freesql_adminlte_tools_success--"))
                    Console.WriteFormatted(shellret.info + "\r\n\r\n", Color.DarkGray);
                else
                {
                    var infolines = shellret.info.Trim().Split('\n');
                    foreach(var infoline in infolines)
                    {
                        if (infoline.TrimStart().StartsWith("READY -> "))
                            Console.WriteFormatted(infoline.Trim() + "\r\n", Color.Magenta);
                        else if (infoline.TrimStart().StartsWith("OUT -> "))
                            Console.WriteFormatted(infoline.Trim() + "\r\n", Color.DarkGray);
                        else if (string.IsNullOrEmpty(infoline.TrimStart()))
                            Console.WriteFormatted("\r\n", Color.DarkGray);
                    }
                }
            }

            GC.Collect();
			Console.WriteFormatted("\r\n[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "] The code files be maked in \"" + ArgsOutput + "\", please check.\r\n", Color.DarkGreen);
		}

		public static (string info, string warn, string err) ShellRun(string cddir, params string[] bat) {
			if (bat == null || bat.Any() == false) return ("", "", "");
            if (string.IsNullOrEmpty(cddir)) cddir = Directory.GetCurrentDirectory();
			var proc = new System.Diagnostics.Process();
			proc.StartInfo = new System.Diagnostics.ProcessStartInfo {
				CreateNoWindow = true,
				FileName = "cmd.exe",
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				WorkingDirectory = cddir
			};
			proc.Start();
			foreach (var cmd in bat)
				proc.StandardInput.WriteLine(cmd);
			proc.StandardInput.WriteLine("exit");
			var outStr = proc.StandardOutput.ReadToEnd();
			var errStr = proc.StandardError.ReadToEnd();
			proc.Close();
			var idx = outStr.IndexOf($">{bat[0]}");
			if (idx != -1) {
				idx = outStr.IndexOf("\n", idx);
				if (idx != -1) outStr = outStr.Substring(idx + 1);
			}
			idx = outStr.LastIndexOf(">exit");
			if (idx != -1) {
				idx = outStr.LastIndexOf("\n", idx);
				if (idx != -1) outStr = outStr.Remove(idx);
			}
			outStr = outStr.Trim();
			if (outStr == "") outStr = null;
			if (errStr == "") errStr = null;
			return (outStr, string.IsNullOrEmpty(outStr) ? null : errStr, string.IsNullOrEmpty(outStr) ? errStr : null);
		}
	}
}

