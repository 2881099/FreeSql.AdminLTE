using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Text;

public static class UtilConvertExtension
{
	#region ExpressionTree
	static int SetSetPropertyOrFieldValueSupportExpressionTreeFlag = 1;
	static ConcurrentDictionary<Type, ConcurrentDictionary<string, Action<object, string, object>>> _dicSetPropertyOrFieldValue = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Action<object, string, object>>>();
	public static void SetPropertyOrFieldValue(this Type entityType, object entity, string propertyName, object value)
	{
		if (entity == null) return;
		if (entityType == null) entityType = entity.GetType();

		if (SetSetPropertyOrFieldValueSupportExpressionTreeFlag == 0)
		{
			if (GetPropertiesDictIgnoreCase(entityType).TryGetValue(propertyName, out var prop))
			{
				prop.SetValue(entity, value, null);
				return;
			}
			if (GetFieldsDictIgnoreCase(entityType).TryGetValue(propertyName, out var field))
			{
				field.SetValue(entity, value);
				return;
			}
			throw new Exception($"The property({propertyName}) was not found in the type({entityType.DisplayCsharp()})");
		}

		Action<object, string, object> func = null;
		try
		{
			func = _dicSetPropertyOrFieldValue
				.GetOrAdd(entityType, et => new ConcurrentDictionary<string, Action<object, string, object>>())
				.GetOrAdd(propertyName, pn =>
				{
					var t = entityType;
					MemberInfo memberinfo = entityType.GetPropertyOrFieldIgnoreCase(pn);
					var parm1 = Expression.Parameter(typeof(object));
					var parm2 = Expression.Parameter(typeof(string));
					var parm3 = Expression.Parameter(typeof(object));
					var var1Parm = Expression.Variable(t);
					var exps = new List<Expression>(new Expression[] {
							Expression.Assign(var1Parm, Expression.TypeAs(parm1, t))
					});
					if (memberinfo != null)
					{
						exps.Add(
							Expression.Assign(
								Expression.MakeMemberAccess(var1Parm, memberinfo),
								Expression.Convert(
									parm3,
									memberinfo.GetPropertyOrFieldType()
								)
							)
						);
					}
					return Expression.Lambda<Action<object, string, object>>(Expression.Block(new[] { var1Parm }, exps), new[] { parm1, parm2, parm3 }).Compile();
				});
		}
		catch
		{
			System.Threading.Interlocked.Exchange(ref SetSetPropertyOrFieldValueSupportExpressionTreeFlag, 0);
			SetPropertyOrFieldValue(entityType, entity, propertyName, value);
			return;
		}
		func(entity, propertyName, value);
	}
	#endregion

	#region 常用缓存的反射方法


	static ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _dicGetPropertiesDictIgnoreCase = new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();
	public static Dictionary<string, PropertyInfo> GetPropertiesDictIgnoreCase(this Type that) => that == null ? null : _dicGetPropertiesDictIgnoreCase.GetOrAdd(that, tp =>
	{
		var props = that.GetProperties().GroupBy(p => p.DeclaringType).Reverse().SelectMany(p => p); //将基类的属性位置放在前面 #164
		var dict = new Dictionary<string, PropertyInfo>(StringComparer.CurrentCultureIgnoreCase);
		foreach (var prop in props)
		{
			if (dict.TryGetValue(prop.Name, out var existsProp))
			{
				if (existsProp.DeclaringType != prop) dict[prop.Name] = prop;
				continue;
			}
			dict.Add(prop.Name, prop);
		}
		return dict;
	});
	static ConcurrentDictionary<Type, Dictionary<string, FieldInfo>> _dicGetFieldsDictIgnoreCase = new ConcurrentDictionary<Type, Dictionary<string, FieldInfo>>();
	public static Dictionary<string, FieldInfo> GetFieldsDictIgnoreCase(this Type that) => that == null ? null : _dicGetFieldsDictIgnoreCase.GetOrAdd(that, tp =>
	{
		var fields = that.GetFields().GroupBy(p => p.DeclaringType).Reverse().SelectMany(p => p); //将基类的属性位置放在前面 #164
		var dict = new Dictionary<string, FieldInfo>(StringComparer.CurrentCultureIgnoreCase);
		foreach (var field in fields)
		{
			if (dict.ContainsKey(field.Name)) dict[field.Name] = field;
			else dict.Add(field.Name, field);
		}
		return dict;
	});
	public static MemberInfo GetPropertyOrFieldIgnoreCase(this Type that, string name)
	{
		if (GetPropertiesDictIgnoreCase(that).TryGetValue(name, out var prop)) return prop;
		if (GetFieldsDictIgnoreCase(that).TryGetValue(name, out var field)) return field;
		return null;
	}
	public static Type GetPropertyOrFieldType(this MemberInfo that)
	{
		if (that is PropertyInfo prop) return prop.PropertyType;
		if (that is FieldInfo field) return field.FieldType;
		return null;
	}
	#endregion

	#region 类型转换
	internal static string ToInvariantCultureToString(this object obj) => obj is string objstr ? objstr : string.Format(CultureInfo.InvariantCulture, @"{0}", obj);
	public static void MapSetListValue(this object[] list, Dictionary<string, Func<object[], object>> valueHandlers)
	{
		if (list == null) return;
		for (int idx = list.Length - 2, c = 0; idx >= 0 && c < 2; idx -= 2)
		{
			if (valueHandlers.TryGetValue(list[idx]?.ToString(), out var tryFunc))
			{
				c++;
				var value = list[idx + 1] as object[];
				if (value != null) list[idx + 1] = tryFunc(value);
			}
		}
	}
	public static T MapToClass<T>(this object[] list, Encoding encoding)
	{
		if (list == null) return default(T);
		if (list.Length % 2 != 0) throw new ArgumentException(nameof(list));
		var ttype = typeof(T);
		var ret = (T)ttype.CreateInstanceGetDefaultValue();
		for (var a = 0; a < list.Length; a += 2)
		{
			var name = list[a].ToString().Replace("-", "_");
			var prop = ttype.GetPropertyOrFieldIgnoreCase(name);
			if (prop == null) continue; // throw new ArgumentException($"{typeof(T).DisplayCsharp()} undefined Property {list[a]}");
			if (list[a + 1] == null) continue;
			ttype.SetPropertyOrFieldValue(ret, prop.Name, prop.GetPropertyOrFieldType().FromObject(list[a + 1], encoding));
		}
		return ret;
	}
	public static Dictionary<string, T> MapToHash<T>(this object[] list, Encoding encoding)
	{
		if (list == null) return null;
		if (list.Length % 2 != 0) throw new ArgumentException($"Array {nameof(list)} length is not even");
		var dic = new Dictionary<string, T>();
		for (var a = 0; a < list.Length; a += 2)
		{
			var key = list[a].ToInvariantCultureToString();
			if (dic.ContainsKey(key)) continue;
			var val = list[a + 1];
			if (val == null) dic.Add(key, default(T));
			else dic.Add(key, val is T conval ? conval : (T)typeof(T).FromObject(val, encoding));
		}
		return dic;
	}
	public static List<KeyValuePair<string, T>> MapToKvList<T>(this object[] list, Encoding encoding)
	{
		if (list == null) return null;
		if (list.Length % 2 != 0) throw new ArgumentException($"Array {nameof(list)} length is not even");
		var ret = new List<KeyValuePair<string, T>>();
		for (var a = 0; a < list.Length; a += 2)
		{
			var key = list[a].ToInvariantCultureToString();
			var val = list[a + 1];
			if (val == null) ret.Add(new KeyValuePair<string, T>(key, default(T)));
			else ret.Add(new KeyValuePair<string, T>(key, val is T conval ? conval : (T)typeof(T).FromObject(val, encoding)));
		}
		return ret;
	}
	public static List<T> MapToList<T>(this object[] list, Func<object, object, T> selector)
	{
		if (list == null) return null;
		if (list.Length % 2 != 0) throw new ArgumentException($"Array {nameof(list)} length is not even");
		var ret = new List<T>();
		for (var a = 0; a < list.Length; a += 2)
		{
			var selval = selector(list[a], list[a + 1]);
			if (selval != null) ret.Add(selval);
		}
		return ret;
	}
	internal static T ConvertTo<T>(this object value) => (T)typeof(T).FromObject(value);
	static ConcurrentDictionary<Type, Func<string, object>> _dicFromObject = new ConcurrentDictionary<Type, Func<string, object>>();
	public static object FromObject(this Type targetType, object value, Encoding encoding = null)
	{
		if (targetType == typeof(object)) return value;
		if (encoding == null) encoding = Encoding.UTF8;
		var valueIsNull = value == null;
		var valueType = valueIsNull ? typeof(string) : value.GetType();
		if (valueType == targetType) return value;
		if (valueType == typeof(byte[])) //byte[] -> guid
		{
			if (targetType == typeof(Guid))
			{
				var bytes = value as byte[];
				return Guid.TryParse(BitConverter.ToString(bytes, 0, Math.Min(bytes.Length, 36)).Replace("-", ""), out var tryguid) ? tryguid : Guid.Empty;
			}
			if (targetType == typeof(Guid?))
			{
				var bytes = value as byte[];
				return Guid.TryParse(BitConverter.ToString(bytes, 0, Math.Min(bytes.Length, 36)).Replace("-", ""), out var tryguid) ? (Guid?)tryguid : null;
			}
		}
		if (targetType == typeof(byte[])) //guid -> byte[]
		{
			if (valueIsNull) return null;
			if (valueType == typeof(Guid) || valueType == typeof(Guid?))
			{
				var bytes = new byte[16];
				var guidN = ((Guid)value).ToString("N");
				for (var a = 0; a < guidN.Length; a += 2)
					bytes[a / 2] = byte.Parse($"{guidN[a]}{guidN[a + 1]}", NumberStyles.HexNumber);
				return bytes;
			}
			return encoding.GetBytes(value.ToInvariantCultureToString());
		}
		else if (targetType.IsArray)
		{
			if (value is Array valueArr)
			{
				var targetElementType = targetType.GetElementType();
				var sourceArrLen = valueArr.Length;
				var target = Array.CreateInstance(targetElementType, sourceArrLen);
				for (var a = 0; a < sourceArrLen; a++) target.SetValue(targetElementType.FromObject(valueArr.GetValue(a), encoding), a);
				return target;
			}
			//if (value is IList valueList)
			//{
			//    var targetElementType = targetType.GetElementType();
			//    var sourceArrLen = valueList.Count;
			//    var target = Array.CreateInstance(targetElementType, sourceArrLen);
			//    for (var a = 0; a < sourceArrLen; a++) target.SetValue(targetElementType.FromObject(valueList[a], encoding), a);
			//    return target;
			//}
		}
		var func = _dicFromObject.GetOrAdd(targetType, tt =>
		{
			if (tt == typeof(object)) return vs => vs;
			if (tt == typeof(string)) return vs => vs;
			if (tt == typeof(char[])) return vs => vs == null ? null : vs.ToCharArray();
			if (tt == typeof(char)) return vs => vs == null ? default(char) : vs.ToCharArray(0, 1).FirstOrDefault();
			if (tt == typeof(bool)) return vs =>
			{
				if (vs == null) return false;
				switch (vs.ToLower())
				{
					case "true":
					case "1":
						return true;
				}
				return false;
			};
			if (tt == typeof(bool?)) return vs =>
			{
				if (vs == null) return false;
				switch (vs.ToLower())
				{
					case "true":
					case "1":
						return true;
					case "false":
					case "0":
						return false;
				}
				return null;
			};
			if (tt == typeof(byte)) return vs => vs == null ? 0 : (byte.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(byte?)) return vs => vs == null ? null : (byte.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (byte?)tryval : null);
			if (tt == typeof(decimal)) return vs => vs == null ? 0 : (decimal.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(decimal?)) return vs => vs == null ? null : (decimal.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (decimal?)tryval : null);
			if (tt == typeof(double)) return vs => vs == null ? 0 : (double.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(double?)) return vs => vs == null ? null : (double.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (double?)tryval : null);
			if (tt == typeof(float)) return vs => vs == null ? 0 : (float.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(float?)) return vs => vs == null ? null : (float.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (float?)tryval : null);
			if (tt == typeof(int)) return vs => vs == null ? 0 : (int.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(int?)) return vs => vs == null ? null : (int.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (int?)tryval : null);
			if (tt == typeof(long)) return vs => vs == null ? 0 : (long.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(long?)) return vs => vs == null ? null : (long.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (long?)tryval : null);
			if (tt == typeof(sbyte)) return vs => vs == null ? 0 : (sbyte.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(sbyte?)) return vs => vs == null ? null : (sbyte.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (sbyte?)tryval : null);
			if (tt == typeof(short)) return vs => vs == null ? 0 : (short.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(short?)) return vs => vs == null ? null : (short.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (short?)tryval : null);
			if (tt == typeof(uint)) return vs => vs == null ? 0 : (uint.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(uint?)) return vs => vs == null ? null : (uint.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (uint?)tryval : null);
			if (tt == typeof(ulong)) return vs => vs == null ? 0 : (ulong.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(ulong?)) return vs => vs == null ? null : (ulong.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (ulong?)tryval : null);
			if (tt == typeof(ushort)) return vs => vs == null ? 0 : (ushort.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(ushort?)) return vs => vs == null ? null : (ushort.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (ushort?)tryval : null);
			if (tt == typeof(DateTime)) return vs => vs == null ? DateTime.MinValue : (DateTime.TryParse(vs, out var tryval) ? tryval : DateTime.MinValue);
			if (tt == typeof(DateTime?)) return vs => vs == null ? null : (DateTime.TryParse(vs, out var tryval) ? (DateTime?)tryval : null);
			if (tt == typeof(DateTimeOffset)) return vs => vs == null ? DateTimeOffset.MinValue : (DateTimeOffset.TryParse(vs, out var tryval) ? tryval : DateTimeOffset.MinValue);
			if (tt == typeof(DateTimeOffset?)) return vs => vs == null ? null : (DateTimeOffset.TryParse(vs, out var tryval) ? (DateTimeOffset?)tryval : null);
			if (tt == typeof(TimeSpan)) return vs => vs == null ? TimeSpan.Zero : (TimeSpan.TryParse(vs, out var tryval) ? tryval : TimeSpan.Zero);
			if (tt == typeof(TimeSpan?)) return vs => vs == null ? null : (TimeSpan.TryParse(vs, out var tryval) ? (TimeSpan?)tryval : null);
			if (tt == typeof(Guid)) return vs => vs == null ? Guid.Empty : (Guid.TryParse(vs, out var tryval) ? tryval : Guid.Empty);
			if (tt == typeof(Guid?)) return vs => vs == null ? null : (Guid.TryParse(vs, out var tryval) ? (Guid?)tryval : null);
			if (tt == typeof(BigInteger)) return vs => vs == null ? 0 : (BigInteger.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? tryval : 0);
			if (tt == typeof(BigInteger?)) return vs => vs == null ? null : (BigInteger.TryParse(vs, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var tryval) ? (BigInteger?)tryval : null);
			if (tt.NullableTypeOrThis().IsEnum)
			{
				var tttype = tt.NullableTypeOrThis();
				var ttdefval = tt.CreateInstanceGetDefaultValue();
				return vs =>
				{
					if (string.IsNullOrWhiteSpace(vs)) return ttdefval;
					return Enum.Parse(tttype, vs, true);
				};
			}
			var localTargetType = targetType;
			var localValueType = valueType;
			return vs =>
			{
				if (vs == null) return null;
				throw new NotSupportedException($"convert failed {localValueType.DisplayCsharp()} -> {localTargetType.DisplayCsharp()}");
			};
		});
		var valueStr = valueIsNull ? null : (valueType == typeof(byte[]) ? encoding.GetString(value as byte[]) : value.ToInvariantCultureToString());
		return func(valueStr);
	}
	#endregion
}