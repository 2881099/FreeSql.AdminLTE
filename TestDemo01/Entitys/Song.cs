using FreeSql.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TestDemo01.Entitys
{
    public class Song
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? Create_time { get; set; }

        /// <summary>
        /// 软删除
        /// </summary>
        public bool? Is_deleted { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Url { get; set; }

        [JsonIgnore] public virtual ICollection<Tag> Tags { get; set; }

        [Column(IsVersion = true)]
        public long versionRow { get; set; }
    }
    public class Song_tag
    {
        public int Song_id { get; set; }
        [JsonIgnore] public virtual Song Song { get; set; }

        public int Tag_id { get; set; }
        [JsonIgnore] public virtual Tag Tag { get; set; }
    }

    public class Tag
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 父id
        /// </summary>
        public int? Parent_id { get; set; }
        [JsonIgnore] public virtual Tag Parent { get; set; }

        /// <summary>
        /// 测试字段
        /// </summary>
        public decimal? Ddd { get; set; }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; }

        [JsonIgnore] public virtual ICollection<Song> Songs { get; set; }
        [JsonIgnore] public virtual ICollection<Tag> Tags { get; set; }
    }


    public class User
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Name { get; set; }

        [JsonIgnore] public ICollection<UserImage> UserImages { get; set; }
    }

    public class UserImage
    {
        [Column(IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// Url地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 用户id
        /// </summary>
        public int User_id { get; set; }
        [JsonIgnore] public virtual User User { get; set; }
    }
}
