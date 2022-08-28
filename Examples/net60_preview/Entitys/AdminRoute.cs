using FreeSql.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace net60_preview.Entitys
{
    /// <summary>
    /// 音乐
    /// </summary>
    public class AdminRoute
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? Create_time { get; set; }

        [Navigate(nameof(ParentId))]
        public List<AdminRoute> Childs { get; set; }

        /// <summary>
        /// 父节点
        /// </summary>
        public int ParentId { get; set; }
        [Navigate(nameof(ParentId))]
        public AdminRoute Parent { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 前端数据
        /// </summary>
        public string Extdata { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
