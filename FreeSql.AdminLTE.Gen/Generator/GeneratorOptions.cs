using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSql.AdminLTE
{
    public class GeneratorOptions
    {
        /// <summary>
        /// 控制器命名空间（默认：FreeSql.AdminLTE）
        /// </summary>
        public string NameSpace { get; set; } = "FreeSql.AdminLTE";

        /// <summary>
        /// 控制器请求路径前辍（默认：/AdminLTE/）
        /// </summary>
        public string ControllerRouteBase { get; set; } = "/adminlte/";

        /// <summary>
        /// 控制器基类（默认：Controller）
        /// </summary>
        public string ControllerBase { get; set; } = "Controller";
    }
}
