using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using InfinityCode.RealWorldTerrain.Utils;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.JSON;

public class RealWorldTerrainJsonObject : RealWorldTerrainJsonItem
{
	private Dictionary<string, RealWorldTerrainJsonItem> _table;

	public Dictionary<string, RealWorldTerrainJsonItem> table => _table;

	public override RealWorldTerrainJsonItem this[string key] => Get(key);

	public override RealWorldTerrainJsonItem this[int index]
	{
		get
		{
			if (index < 0)
			{
				return null;
			}
			int num = 0;
			foreach (KeyValuePair<string, RealWorldTerrainJsonItem> item in _table)
			{
				if (num == index)
				{
					return item.Value;
				}
				num++;
			}
			return null;
		}
	}

	public RealWorldTerrainJsonObject()
	{
		_table = new Dictionary<string, RealWorldTerrainJsonItem>();
	}

	public void Add(string name, RealWorldTerrainJsonItem value)
	{
		_table[name] = value;
	}

	public void Add(string name, object value)
	{
		if (value is string || value is bool || value is int || value is long || value is short || value is float || value is double)
		{
			_table[name] = new RealWorldTerrainJsonValue(value);
		}
		else if (value is UnityEngine.Object)
		{
			_table[name] = new RealWorldTerrainJsonValue((value as UnityEngine.Object).GetInstanceID());
		}
		else
		{
			_table[name] = RealWorldTerrainJson.Serialize(value);
		}
	}

	public void Add(string name, object value, RealWorldTerrainJsonValue.ValueType valueType)
	{
		_table[name] = new RealWorldTerrainJsonValue(value, valueType);
	}

	public override RealWorldTerrainJsonItem AppendObject(object obj)
	{
		Combine(RealWorldTerrainJson.Serialize(obj));
		return this;
	}

	public void Combine(RealWorldTerrainJsonItem other, bool overwriteExistingValues = false)
	{
		foreach (KeyValuePair<string, RealWorldTerrainJsonItem> item in ((other as RealWorldTerrainJsonObject) ?? throw new Exception("Only RealWorldTerrainJsonObject is allowed to be combined.")).table)
		{
			if (overwriteExistingValues || !_table.ContainsKey(item.Key))
			{
				_table[item.Key] = item.Value;
			}
		}
	}

	public bool Contains(string key)
	{
		return _table.ContainsKey(key);
	}

	public RealWorldTerrainJsonArray CreateArray(string name)
	{
		RealWorldTerrainJsonArray realWorldTerrainJsonArray = new RealWorldTerrainJsonArray();
		Add(name, realWorldTerrainJsonArray);
		return realWorldTerrainJsonArray;
	}

	public RealWorldTerrainJsonObject CreateObject(string name)
	{
		RealWorldTerrainJsonObject realWorldTerrainJsonObject = new RealWorldTerrainJsonObject();
		Add(name, realWorldTerrainJsonObject);
		return realWorldTerrainJsonObject;
	}

	public override object Deserialize(Type type, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		IEnumerable<MemberInfo> members = RealWorldTerrainReflectionHelper.GetMembers(type, bindingFlags);
		return Deserialize(type, members, bindingFlags);
	}

	public object Deserialize(Type type, IEnumerable<MemberInfo> members, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		object obj = Activator.CreateInstance(type);
		DeserializeObject(obj, members, bindingFlags);
		return obj;
	}

	public void DeserializeObject(object obj, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		IEnumerable<MemberInfo> members = RealWorldTerrainReflectionHelper.GetMembers(obj.GetType(), bindingFlags);
		DeserializeObject(obj, members);
	}

	public void DeserializeObject(object obj, IEnumerable<MemberInfo> members, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		foreach (MemberInfo member in members)
		{
			MemberTypes memberType = member.MemberType;
			if ((memberType != MemberTypes.Field && memberType != MemberTypes.Property) || (memberType == MemberTypes.Property && !((PropertyInfo)member).CanWrite))
			{
				continue;
			}
			object[] customAttributes = member.GetCustomAttributes(typeof(RealWorldTerrainJson.AliasAttribute), inherit: true);
			RealWorldTerrainJson.AliasAttribute aliasAttribute = ((customAttributes.Length != 0) ? (customAttributes[0] as RealWorldTerrainJson.AliasAttribute) : null);
			if ((aliasAttribute == null || !aliasAttribute.ignoreFieldName) && _table.TryGetValue(member.Name, out var value))
			{
				Type type = ((memberType == MemberTypes.Field) ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType);
				if (memberType == MemberTypes.Field)
				{
					((FieldInfo)member).SetValue(obj, value.Deserialize(type, bindingFlags));
				}
				else
				{
					((PropertyInfo)member).SetValue(obj, value.Deserialize(type, bindingFlags), null);
				}
			}
			else
			{
				if (aliasAttribute == null)
				{
					continue;
				}
				for (int i = 0; i < aliasAttribute.aliases.Length; i++)
				{
					if (_table.TryGetValue(aliasAttribute.aliases[i], out value))
					{
						Type type2 = ((memberType == MemberTypes.Field) ? ((FieldInfo)member).FieldType : ((PropertyInfo)member).PropertyType);
						if (memberType == MemberTypes.Field)
						{
							((FieldInfo)member).SetValue(obj, value.Deserialize(type2, bindingFlags));
						}
						else
						{
							((PropertyInfo)member).SetValue(obj, value.Deserialize(type2, bindingFlags), null);
						}
						break;
					}
				}
			}
		}
	}

	private RealWorldTerrainJsonItem Get(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		if (key.Length > 2 && key[0] == '/' && key[1] == '/')
		{
			string text = key.Substring(2);
			if (string.IsNullOrEmpty(text) || text.StartsWith("//"))
			{
				return null;
			}
			return GetAll(text);
		}
		return GetThis(key);
	}

	private RealWorldTerrainJsonItem GetThis(string key)
	{
		int num = -1;
		for (int i = 0; i < key.Length; i++)
		{
			if (key[i] == '/')
			{
				num = i;
				break;
			}
		}
		RealWorldTerrainJsonItem value;
		if (num != -1)
		{
			string text = key.Substring(0, num);
			if (!string.IsNullOrEmpty(text) && _table.TryGetValue(text, out value))
			{
				string key2 = key.Substring(num + 1);
				return value[key2];
			}
			return null;
		}
		if (_table.TryGetValue(key, out value))
		{
			return value;
		}
		return null;
	}

	public override RealWorldTerrainJsonItem GetAll(string k)
	{
		RealWorldTerrainJsonItem realWorldTerrainJsonItem = GetThis(k);
		RealWorldTerrainJsonArray realWorldTerrainJsonArray = null;
		if (realWorldTerrainJsonItem != null)
		{
			realWorldTerrainJsonArray = new RealWorldTerrainJsonArray();
			realWorldTerrainJsonArray.Add(realWorldTerrainJsonItem);
		}
		Dictionary<string, RealWorldTerrainJsonItem>.Enumerator enumerator = _table.GetEnumerator();
		while (enumerator.MoveNext())
		{
			realWorldTerrainJsonItem = enumerator.Current.Value;
			if (realWorldTerrainJsonItem.GetAll(k) is RealWorldTerrainJsonArray collection)
			{
				if (realWorldTerrainJsonArray == null)
				{
					realWorldTerrainJsonArray = new RealWorldTerrainJsonArray();
				}
				realWorldTerrainJsonArray.AddRange(collection);
			}
		}
		return realWorldTerrainJsonArray;
	}

	public override IEnumerator<RealWorldTerrainJsonItem> GetEnumerator()
	{
		return _table.Values.GetEnumerator();
	}

	public static RealWorldTerrainJsonObject ParseObject(string json)
	{
		return RealWorldTerrainJson.Parse(json) as RealWorldTerrainJsonObject;
	}

	public RealWorldTerrainJsonItem Remove(string key)
	{
		if (_table.TryGetValue(key, out var value))
		{
			_table.Remove(key);
			return value;
		}
		return null;
	}

	public override void ToJSON(StringBuilder b)
	{
		b.Append("{");
		bool flag = false;
		foreach (KeyValuePair<string, RealWorldTerrainJsonItem> item in _table)
		{
			b.Append("\"").Append(item.Key).Append("\"")
				.Append(":");
			item.Value.ToJSON(b);
			b.Append(",");
			flag = true;
		}
		if (flag)
		{
			b.Remove(b.Length - 1, 1);
		}
		b.Append("}");
	}

	public override object Value(Type type)
	{
		if (RealWorldTerrainReflectionHelper.IsValueType(type))
		{
			return Activator.CreateInstance(type);
		}
		return Deserialize(type);
	}
}
