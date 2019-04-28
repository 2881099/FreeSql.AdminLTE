using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;

namespace TestDemo01.Entitys {
	public class Song {
		[Column(IsIdentity = true)]
		public int Id { get; set; }
		public DateTime? Create_time { get; set; }
		public bool? Is_deleted { get; set; }
		public string Title { get; set; }
		public string Url { get; set; }


		[Column(IsVersion = true)]
		public long versionRow { get; set; }
	}
	public class Song_tag {
		public int Song_id { get; set; }

		public int Tag_id { get; set; }
	}

	public class Tag {
		[Column(IsIdentity = true)]
		public int Id { get; set; }
		public int? Parent_id { get; set; }

		public decimal? Ddd { get; set; }
		public string Name { get; set; }
	}


	public class User {
		[Column(IsIdentity = true)]
		public int Id { get; set; }

		public string Name { get; set; }

		public ICollection<UserImage> UserImages { get; set; }
	}

	public class UserImage {
		[Column(IsIdentity = true)]
		public int Id { get; set; }

		public string Url { get; set; }

		public int User_id { get; set; }
		public virtual User User { get; set; }
	}
}
