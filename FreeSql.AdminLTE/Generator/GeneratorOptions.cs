using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSql.AdminLTE
{
    public class GeneratorOptions
    {
        /// <summary>
        /// 控制器命名中间，默认为 FreeSql.AdminLTE
        /// </summary>
        public string NameSpace { get; set; } = "FreeSql.AdminLTE";

        /// <summary>
        /// 控制器请求路径前辍，默认为 /AdminLTE/
        /// </summary>
        public string ControllerRouteBase { get; set; } = "/AdminLTE/";

        /// <summary>
        /// 控制器基类，默认为 BaseController
        /// </summary>
        public string ControllerBase { get; set; } = "BaseController";
    }
}
