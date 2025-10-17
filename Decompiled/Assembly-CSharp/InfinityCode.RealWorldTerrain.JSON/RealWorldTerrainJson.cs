using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using InfinityCode.RealWorldTerrain.Utils;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.JSON;

public class RealWorldTerrainJson
{
	private enum Token
	{
		None = -1,
		Curly_Open,
		Curly_Close,
		Squared_Open,
		Squared_Close,
		Colon,
		Comma,
		String,
		Number,
		True,
		False,
		Null
	}

	public class AliasAttribute : Attribute
	{
		public readonly string[] aliases;

		public readonly bool ignoreFieldName;

		public AliasAttribute(bool ignoreFieldName, params string[] aliases)
		{
			if (aliases == null || aliases.Length == 0)
			{
				throw new Exception("You must use at least one alias.");
			}
			this.ignoreFieldName = ignoreFieldName;
			this.aliases = aliases;
		}

		public AliasAttribute(params string[] aliases)
			: this(ignoreFieldName: false, aliases)
		{
		}
	}

	private string json;

	private int index;

	private Token lookAheadToken = Token.None;

	private StringBuilder s;

	private int length;

	protected RealWorldTerrainJson(string json)
	{
		s = new StringBuilder();
		this.json = json;
		length = json.Length;
	}

	public static T Deserialize<T>(string json)
	{
		object obj = ParseDirect(json);
		if (obj is IDictionary)
		{
			return (T)DeserializeObject(typeof(T), obj as Dictionary<string, object>);
		}
		if (obj is IList)
		{
			return (T)DeserializeArray(typeof(T), obj as List<object>);
		}
		return (T)DeserializeValue(typeof(T), obj);
	}

	private static object DeserializeValue(Type type, object obj)
	{
		if (obj == null)
		{
			return null;
		}
		try
		{
			return Convert.ChangeType(obj, type);
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);
		}
		return null;
	}

	private static object DeserializeArray(Type type, List<object> list)
	{
		if (list == null || list.Count == 0)
		{
			return null;
		}
		if (type.IsArray)
		{
			Type elementType = type.GetElementType();
			Array array = Array.CreateInstance(elementType, list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				object obj = list[i];
				object value = ((!(obj is IDictionary)) ? ((!(obj is IList)) ? DeserializeValue(elementType, obj) : DeserializeArray(elementType, obj as List<object>)) : DeserializeObject(elementType, obj as Dictionary<string, object>));
				array.SetValue(value, i);
			}
			return array;
		}
		if (RealWorldTerrainReflectionHelper.IsGenericType(type))
		{
			Type type2 = RealWorldTerrainReflectionHelper.GetGenericArguments(type)[0];
			object obj2 = Activator.CreateInstance(type);
			for (int j = 0; j < list.Count; j++)
			{
				object obj3 = list[j];
				object obj4 = ((obj3 is IDictionary) ? DeserializeObject(type2, obj3 as Dictionary<string, object>) : ((!(obj3 is IList)) ? DeserializeValue(type2, obj3) : DeserializeArray(type2, obj3 as List<object>)));
				try
				{
					MethodInfo method = RealWorldTerrainReflectionHelper.GetMethod(type, "Add");
					if (method != null)
					{
						method.Invoke(obj2, new object[1] { obj4 });
					}
				}
				catch
				{
				}
			}
			return obj2;
		}
		return null;
	}

	private static object DeserializeObject(Type type, Dictionary<string, object> table)
	{
		IEnumerable<MemberInfo> members = RealWorldTerrainReflectionHelper.GetMembers(type, BindingFlags.Instance | BindingFlags.Public);
		object obj = Activator.CreateInstance(type);
		foreach (MemberInfo item in members)
		{
			MemberTypes memberType = item.MemberType;
			if ((memberType != MemberTypes.Field && memberType != MemberTypes.Property) || (memberType == MemberTypes.Property && !((PropertyInfo)item).CanWrite))
			{
				continue;
			}
			object[] customAttributes = item.GetCustomAttributes(typeof(AliasAttribute), inherit: true);
			AliasAttribute aliasAttribute = ((customAttributes.Length != 0) ? (customAttributes[0] as AliasAttribute) : null);
			if ((aliasAttribute == null || !aliasAttribute.ignoreFieldName) && table.TryGetValue(item.Name, out var value))
			{
				DeserializeValue(memberType, item, value, obj);
			}
			else
			{
				if (aliasAttribute == null)
				{
					continue;
				}
				for (int i = 0; i < aliasAttribute.aliases.Length; i++)
				{
					if (table.TryGetValue(aliasAttribute.aliases[i], out value))
					{
						DeserializeValue(memberType, item, value, obj);
						break;
					}
				}
			}
		}
		return obj;
	}

	private static void DeserializeValue(MemberTypes memberType, MemberInfo member, object item, object v)
	{
		Type type = ((memberType == MemberTypes.Field) ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType);
		object value = ((type == typeof(object)) ? item : ((item is IDictionary) ? DeserializeObject(type, item as Dictionary<string, object>) : ((!(item is IList)) ? DeserializeValue(type, item) : DeserializeArray(type, item as List<object>))));
		if (memberType == MemberTypes.Field)
		{
			((FieldInfo)member).SetValue(v, value);
		}
		else
		{
			((PropertyInfo)member).SetValue(v, value, null);
		}
	}

	private Token LookAhead()
	{
		if (lookAheadToken != Token.None)
		{
			return lookAheadToken;
		}
		return lookAheadToken = NextTokenCore();
	}

	private Token NextToken()
	{
		Token result = ((lookAheadToken != Token.None) ? lookAheadToken : NextTokenCore());
		lookAheadToken = Token.None;
		return result;
	}

	private Token NextTokenCore()
	{
		char c;
		do
		{
			c = json[index];
			if (c == '/' && json[index + 1] == '/')
			{
				index += 2;
				do
				{
					c = json[index];
				}
				while (c != '\r' && c != '\n' && ++index < length);
			}
			switch (c)
			{
			case '\t':
			case '\n':
			case '\r':
			case ' ':
				continue;
			}
			break;
		}
		while (++index < length);
		if (index == length)
		{
			throw new Exception("Reached end of string unexpectedly");
		}
		c = json[index];
		index++;
		switch (c)
		{
		case '{':
			return Token.Curly_Open;
		case '}':
			return Token.Curly_Close;
		case '[':
			return Token.Squared_Open;
		case ']':
			return Token.Squared_Close;
		case ',':
			return Token.Comma;
		case '"':
			return Token.String;
		case '+':
		case '-':
		case '.':
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
			return Token.Number;
		case ':':
			return Token.Colon;
		case 'f':
			if (length - index >= 4 && json[index] == 'a' && json[index + 1] == 'l' && json[index + 2] == 's' && json[index + 3] == 'e')
			{
				index += 4;
				return Token.False;
			}
			break;
		case 't':
			if (length - index >= 3 && json[index] == 'r' && json[index + 1] == 'u' && json[index + 2] == 'e')
			{
				index += 3;
				return Token.True;
			}
			break;
		case 'n':
			if (length - index >= 3 && json[index] == 'u' && json[index + 1] == 'l' && json[index + 2] == 'l')
			{
				index += 3;
				return Token.Null;
			}
			break;
		}
		throw new Exception("Could not find token at index " + --index);
	}

	public static RealWorldTerrainJsonItem Parse(string json)
	{
		return new RealWorldTerrainJson(json).ParseValue();
	}

	public static object ParseDirect(string json)
	{
		return new RealWorldTerrainJson(json).ParseValueDirect();
	}

	private RealWorldTerrainJsonArray ParseArray()
	{
		RealWorldTerrainJsonArray realWorldTerrainJsonArray = new RealWorldTerrainJsonArray();
		lookAheadToken = Token.None;
		while (true)
		{
			switch (LookAhead())
			{
			case Token.Comma:
				lookAheadToken = Token.None;
				break;
			case Token.Squared_Close:
				lookAheadToken = Token.None;
				return realWorldTerrainJsonArray;
			default:
				realWorldTerrainJsonArray.Add(ParseValue());
				break;
			}
		}
	}

	private List<object> ParseArrayDirect()
	{
		List<object> list = new List<object>();
		lookAheadToken = Token.None;
		while (true)
		{
			switch (LookAhead())
			{
			case Token.Comma:
				lookAheadToken = Token.None;
				break;
			case Token.Squared_Close:
				lookAheadToken = Token.None;
				return list;
			default:
				list.Add(ParseValueDirect());
				break;
			}
		}
	}

	private object ParseNumber()
	{
		lookAheadToken = Token.None;
		index--;
		long num = 0L;
		bool flag = false;
		long num2 = 0L;
		long num3 = 0L;
		bool flag2 = false;
		for (; index < length; index++)
		{
			char c = json[index];
			if (c >= '0' && c <= '9')
			{
				num = num * 10 + (c - 48);
				num2 *= 10;
				continue;
			}
			switch (c)
			{
			case '.':
				num2 = 1L;
				continue;
			case '-':
				flag = true;
				continue;
			case '+':
				flag = false;
				continue;
			case 'E':
			case 'e':
				if (num2 == 0L)
				{
					num2 = 1L;
				}
				index++;
				num3 = 0L;
				for (; index < length; index++)
				{
					c = json[index];
					if (c >= '0' && c <= '9')
					{
						num3 = num3 * 10 + (c - 48);
						continue;
					}
					switch (c)
					{
					case '-':
						flag2 = true;
						continue;
					case '+':
						flag2 = false;
						continue;
					}
					break;
				}
				break;
			}
			break;
		}
		if (flag)
		{
			num = -num;
		}
		if (num2 != 0L)
		{
			double num4 = (double)num / (double)num2;
			if (num3 > 0)
			{
				num4 = ((!flag2) ? (num4 * Math.Pow(10.0, num3)) : (num4 / Math.Pow(10.0, num3)));
			}
			return num4;
		}
		return num;
	}

	private RealWorldTerrainJsonObject ParseObject()
	{
		RealWorldTerrainJsonObject realWorldTerrainJsonObject = new RealWorldTerrainJsonObject();
		lookAheadToken = Token.None;
		while (true)
		{
			switch (LookAhead())
			{
			case Token.Comma:
				lookAheadToken = Token.None;
				continue;
			case Token.Curly_Close:
				lookAheadToken = Token.None;
				return realWorldTerrainJsonObject;
			}
			string name = ParseString();
			if (NextToken() != Token.Colon)
			{
				throw new Exception("Expected colon at index " + index);
			}
			realWorldTerrainJsonObject.Add(name, ParseValue());
		}
	}

	private Dictionary<string, object> ParseObjectDirect()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		lookAheadToken = Token.None;
		while (true)
		{
			switch (LookAhead())
			{
			case Token.Comma:
				lookAheadToken = Token.None;
				continue;
			case Token.Curly_Close:
				lookAheadToken = Token.None;
				return dictionary;
			}
			string key = ParseString();
			if (NextToken() != Token.Colon)
			{
				throw new Exception("Expected colon at index " + index);
			}
			dictionary.Add(key, ParseValueDirect());
		}
	}

	private uint ParseSingleChar(char c1, uint multipliyer)
	{
		uint result = 0u;
		if (c1 >= '0' && c1 <= '9')
		{
			result = (uint)(c1 - 48) * multipliyer;
		}
		else if (c1 >= 'A' && c1 <= 'F')
		{
			result = (uint)(c1 - 65 + 10) * multipliyer;
		}
		else if (c1 >= 'a' && c1 <= 'f')
		{
			result = (uint)(c1 - 97 + 10) * multipliyer;
		}
		return result;
	}

	private string ParseString()
	{
		lookAheadToken = Token.None;
		s.Length = 0;
		int num = -1;
		int num2 = length;
		string text = json;
		while (index < num2)
		{
			switch (text[index++])
			{
			case '"':
				if (num != -1)
				{
					if (s.Length == 0)
					{
						return text.Substring(num, index - num - 1);
					}
					s.Append(text, num, index - num - 1);
				}
				return s.ToString();
			default:
				if (num == -1)
				{
					num = index - 1;
				}
				continue;
			case '\\':
				break;
			}
			if (index == num2)
			{
				break;
			}
			if (num != -1)
			{
				s.Append(text, num, index - num - 1);
				num = -1;
			}
			switch (text[index++])
			{
			case '"':
				s.Append('"');
				break;
			case '\\':
				s.Append('\\');
				break;
			case '/':
				s.Append('/');
				break;
			case 'b':
				s.Append('\b');
				break;
			case 'f':
				s.Append('\f');
				break;
			case 'n':
				s.Append('\n');
				break;
			case 'r':
				s.Append('\r');
				break;
			case 't':
				s.Append('\t');
				break;
			case 'u':
				if (num2 - index >= 4)
				{
					uint num3 = ParseUnicode(text[index], text[index + 1], text[index + 2], text[index + 3]);
					s.Append((char)num3);
					index += 4;
				}
				break;
			}
		}
		throw new Exception("Unexpectedly reached end of string");
	}

	private uint ParseUnicode(char c1, char c2, char c3, char c4)
	{
		uint num = ParseSingleChar(c1, 4096u);
		uint num2 = ParseSingleChar(c2, 256u);
		uint num3 = ParseSingleChar(c3, 16u);
		uint num4 = ParseSingleChar(c4, 1u);
		return num + num2 + num3 + num4;
	}

	private RealWorldTerrainJsonItem ParseValue()
	{
		switch (LookAhead())
		{
		case Token.Number:
		{
			object obj = ParseNumber();
			return new RealWorldTerrainJsonValue(obj, (!(obj is double)) ? RealWorldTerrainJsonValue.ValueType.LONG : RealWorldTerrainJsonValue.ValueType.DOUBLE);
		}
		case Token.String:
			return new RealWorldTerrainJsonValue(ParseString(), RealWorldTerrainJsonValue.ValueType.STRING);
		case Token.Curly_Open:
			return ParseObject();
		case Token.Squared_Open:
			return ParseArray();
		case Token.True:
			lookAheadToken = Token.None;
			return new RealWorldTerrainJsonValue(true, RealWorldTerrainJsonValue.ValueType.BOOLEAN);
		case Token.False:
			lookAheadToken = Token.None;
			return new RealWorldTerrainJsonValue(false, RealWorldTerrainJsonValue.ValueType.BOOLEAN);
		case Token.Null:
			lookAheadToken = Token.None;
			return new RealWorldTerrainJsonValue(null, RealWorldTerrainJsonValue.ValueType.NULL);
		default:
			throw new Exception("Unrecognized token at index" + index);
		}
	}

	private object ParseValueDirect()
	{
		switch (LookAhead())
		{
		case Token.Number:
			return ParseNumber();
		case Token.String:
			return ParseString();
		case Token.Curly_Open:
			return ParseObjectDirect();
		case Token.Squared_Open:
			return ParseArrayDirect();
		case Token.True:
			lookAheadToken = Token.None;
			return true;
		case Token.False:
			lookAheadToken = Token.None;
			return false;
		case Token.Null:
			lookAheadToken = Token.None;
			return null;
		default:
			throw new Exception("Unrecognized token at index" + index);
		}
	}

	public static RealWorldTerrainJsonItem Serialize(object obj, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		if (obj == null || obj is DBNull)
		{
			return new RealWorldTerrainJsonValue(obj, RealWorldTerrainJsonValue.ValueType.NULL);
		}
		if (obj is string || obj is bool || obj is int || obj is long || obj is short || obj is float || obj is double)
		{
			return new RealWorldTerrainJsonValue(obj);
		}
		if (obj is UnityEngine.Object && !(obj is Component) && !(obj is ScriptableObject))
		{
			return new RealWorldTerrainJsonValue((obj as UnityEngine.Object).GetInstanceID());
		}
		if (obj is IDictionary)
		{
			IDictionary obj2 = obj as IDictionary;
			RealWorldTerrainJsonObject realWorldTerrainJsonObject = new RealWorldTerrainJsonObject();
			ICollection keys = obj2.Keys;
			ICollection values = obj2.Values;
			IEnumerator enumerator = keys.GetEnumerator();
			IEnumerator enumerator2 = values.GetEnumerator();
			while (enumerator.MoveNext() && enumerator2.MoveNext())
			{
				object current = enumerator.Current;
				object current2 = enumerator2.Current;
				realWorldTerrainJsonObject.Add(current as string, Serialize(current2, bindingFlags));
			}
			return realWorldTerrainJsonObject;
		}
		if (obj is IEnumerable)
		{
			IEnumerable obj3 = (IEnumerable)obj;
			RealWorldTerrainJsonArray realWorldTerrainJsonArray = new RealWorldTerrainJsonArray();
			{
				foreach (object item in obj3)
				{
					realWorldTerrainJsonArray.Add(Serialize(item, bindingFlags));
				}
				return realWorldTerrainJsonArray;
			}
		}
		RealWorldTerrainJsonObject realWorldTerrainJsonObject2 = new RealWorldTerrainJsonObject();
		Type type = obj.GetType();
		if (RealWorldTerrainReflectionHelper.CheckIfAnonymousType(type))
		{
			bindingFlags |= BindingFlags.NonPublic;
		}
		foreach (FieldInfo field in RealWorldTerrainReflectionHelper.GetFields(type, bindingFlags))
		{
			string text = field.Name;
			if (field.Attributes == (FieldAttributes.Private | FieldAttributes.InitOnly))
			{
				int num = text.IndexOf('<') + 1;
				int num2 = text.IndexOf('>', num);
				text = ((num2 == -1 || num == -1) ? text.Trim('<', '>') : text.Substring(num, num2 - num));
			}
			realWorldTerrainJsonObject2.Add(text, Serialize(field.GetValue(obj)));
		}
		return realWorldTerrainJsonObject2;
	}
}
