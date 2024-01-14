using FreeSql.DataAnnotations;
using FreeSql.AdminLTE;
using System.Text.Json.Serialization;

namespace net80_blazor.Entities
{
    public class MenuEntity : EntityFull
    {
        /// <summary>
        /// 父级菜单
        /// </summary>
        public long ParentId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// 新窗口打开
        /// </summary>
        public bool TargetBlank { get; set; }
        /// <summary>
        /// 排序
        /// </summary>
        public int Sort { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public MenuEntityType Type { get; set; }

        [Navigate(nameof(ParentId)), JsonIgnore]
        public MenuEntity Parent { get; set; }
        [Navigate(nameof(MenuEntity.ParentId))]
        public List<MenuEntity> Childs { get; set; }
    }
    public enum MenuEntityType { 菜单, 按钮, 资源 }

}
