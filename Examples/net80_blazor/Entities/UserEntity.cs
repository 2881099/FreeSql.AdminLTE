using FreeSql.DataAnnotations;
using FreeSql.AdminLTE;

namespace net80_blazor.Entities
{
    public class UserEntity : EntityCreated
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// 昵称
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 登陆时间
        /// </summary>
        public DateTime LoginTime { get; set; }
    }
}
