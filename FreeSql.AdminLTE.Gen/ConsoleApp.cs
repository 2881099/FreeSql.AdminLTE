using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Drawing;
using Console = Colorful.Console;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

namespace FreeSql.AdminLTE.Gen
{
    public class ConsoleApp {

        public GeneratorOptions ArgsOptions = new GeneratorOptions();
        public string ArgsFind;
        public string ArgsOutput;
        public bool ArgsDependent;

        public ConsoleApp(string[] args, ManualResetEvent wait) {

            //var astest = Assembly.LoadFrom(@"C:\Users\28810\Desktop\新建文件夹\src\ttt.db\bin\Debug\netstandard2.0\publish\ttt.db.dll");
            //var astype = astest.GetType("ttt.Model.CategoryInfo");

            this.ArgsOutput = Directory.GetCurrentDirectory();
			string args0 = args[0].Trim().ToLower();
			if (args[0] == "?" || args0 == "--help" || args0 == "-help") {
                
                Console.WriteAscii(" FreeSql", Color.Violet);
                Console.WriteFormatted(@"
  # Github # {0}

    {1}

  # 生成条件 #

    {2}
    {3}

  # 快速开始 #

    > {4} {5} MyTest\.Model\..+

        -Find                  * 匹配实体类FullName的正则表达式

        -ControllerNameSpace   控制器命名空间（默认：FreeSql.AdminLTE）
        -ControllerRouteBase   控制器请求路径前辍（默认：/AdminLTE/）
        -ControllerBase        控制器基类（默认：Controller）

        -Dependent             是否生成 ApiResult.cs、index.html、htm 静态资源（首次生成）
        -Output                输出路径（默认：当前目录）

  # 生成到其他目录 #

    > {4} {5} MyTest\.Model\..+ -Output d:/test

", Color.SlateGray,
new Colorful.Formatter("https://github.com/2881099/FreeSql", Color.DeepSkyBlue),
new Colorful.Formatter("基于 .NETCore 2.1 环境，在控制台当前目录的项目下，根据实体类生成 AdminLTE 后台管理功能的相关文件。", Color.SlateGray),
new Colorful.Formatter("1、实体类的注释（请开启项目XML文档）；", Color.SlateGray),
new Colorful.Formatter("2、实体类的导航属性配置（可生成繁琐的常用后台管理功能）。", Color.SlateGray),
new Colorful.Formatter("FreeSql.AdminLTE.Gen", Color.White),
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

                    case "-Dependent":
                        ArgsDependent = true;
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

            var dllFiles = new List<string>();

            Console.WriteFormatted("正在编译当前项目 …\r\n", Color.DarkGreen);
            var shellret = ShellRun(null, "dotnet publish");
            if (!string.IsNullOrEmpty(shellret.err))
            {
                Console.WriteFormatted(shellret.err + "\r\n\r\n", Color.Red);
                return;
            }
            if (!string.IsNullOrEmpty(shellret.warn)) Console.WriteFormatted(shellret.warn + "\r\n\r\n", Color.Yellow);
            if (!string.IsNullOrEmpty(shellret.info)) Console.WriteFormatted(shellret.info + "\r\n\r\n", Color.DarkGray);

            Console.WriteFormatted("正在加载程序集 …\r\n", Color.DarkGreen);
            var lines = shellret.info.Split('\n');
            var publishDir = lines.Where(a => a.Contains(" -> ") && a.TrimEnd('/', '\\').EndsWith("publish")).Select(a => a.Substring(a.indexOf(" -> ") + 4).Trim()).LastOrDefault();
            dllFiles.AddRange(lines.Where(a => a.Contains(" -> ") && a.Trim().EndsWith(".dll")).Select(a => publishDir + a.Trim().Split('/', '\\').LastOrDefault()));
            Console.WriteFormatted(string.Join("\r\n", dllFiles) + "\r\n", Color.DarkGray);

            foreach (var pubDllFile in Directory.GetFiles(publishDir, "*.dll"))
            {
                try
                {
                    Assembly.LoadFile(pubDllFile);
                    Console.WriteFormatted($"LOAD -> {pubDllFile}\r\n", Color.DarkGray);
                }
                catch (Exception ex)
                {
                    Console.WriteFormatted($"LOAD ERR -> {pubDllFile} {ex.Message}\r\n", Color.Yellow);
                }
            }

            var findRegexp = new Regex(ArgsFind, RegexOptions.Compiled);
            var entityTypes = new List<Type>();
            foreach (var dllFile in dllFiles)
            {
                var assembly = Assembly.LoadFrom(dllFile);
                var alltypes = assembly.ExportedTypes;//.GetTypes();
                foreach (var type in alltypes)
                {
                    if (type.IsClass && !type.IsInterface && !type.IsAbstract && !type.IsAnonymousType() && 
                        !type.IsEnum && !type.IsGenericType && !type.IsImport && 
                        !type.IsNested && !type.IsSealed && !type.IsValueType && type.IsVisible &&
                        findRegexp.IsMatch(type.FullName))
                    {
                        Console.WriteFormatted($"READY -> {type.FullName}\r\n", Color.Magenta);
                        entityTypes.Add(type);
                    }
                }
            }

            using (var gen = new Generator(ArgsOptions))
            {
                gen.TraceLog = log => Console.WriteFormatted(log + "\r\n", Color.DarkGray);
                gen.Build(ArgsOutput, entityTypes.ToArray(), ArgsDependent);
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

