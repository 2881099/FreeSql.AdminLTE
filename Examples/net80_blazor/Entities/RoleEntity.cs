using FreeSql.DataAnnotations;
using FreeSql.AdminLTE;

namespace net80_blazor.Entities
{
    public class RoleEntity : Entity
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Label { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
    }

    public class RoleUserEntity
    {
        public long RoleId { get; set; }
        public long UserId { get; set; }

        [Navigate(nameof(RoleId))]
        public RoleEntity Role { get; set; }
        [Navigate(nameof(UserId))]
        public UserEntity User { get; set; }
    }
}
