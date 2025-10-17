using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using InfinityCode.RealWorldTerrain.Utils;

namespace InfinityCode.RealWorldTerrain.JSON;

public class RealWorldTerrainJsonArray : RealWorldTerrainJsonItem
{
	private List<RealWorldTerrainJsonItem> _items;

	private int _count;

	public List<RealWorldTerrainJsonItem> items => _items;

	public int count => _count;

	public override RealWorldTerrainJsonItem this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
			{
				return null;
			}
			return _items[index];
		}
	}

	public override RealWorldTerrainJsonItem this[string key] => Get(key);

	public RealWorldTerrainJsonArray()
	{
		_items = new List<RealWorldTerrainJsonItem>();
	}

	public void Add(RealWorldTerrainJsonItem item)
	{
		_items.Add(item);
		_count++;
	}

	public void AddRange(RealWorldTerrainJsonArray collection)
	{
		if (collection != null)
		{
			_items.AddRange(collection._items);
			_count += collection._count;
		}
	}

	public void AddRange(RealWorldTerrainJsonItem collection)
	{
		AddRange(collection as RealWorldTerrainJsonArray);
	}

	public RealWorldTerrainJsonObject CreateObject()
	{
		RealWorldTerrainJsonObject realWorldTerrainJsonObject = new RealWorldTerrainJsonObject();
		Add(realWorldTerrainJsonObject);
		return realWorldTerrainJsonObject;
	}

	public override object Deserialize(Type type, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		if (_count == 0)
		{
			return null;
		}
		if (type.IsArray)
		{
			Type elementType = type.GetElementType();
			Array array = Array.CreateInstance(elementType, _count);
			if (_items[0] is RealWorldTerrainJsonObject)
			{
				IEnumerable<MemberInfo> members = RealWorldTerrainReflectionHelper.GetMembers(elementType, bindingFlags);
				for (int i = 0; i < _count; i++)
				{
					object value = (_items[i] as RealWorldTerrainJsonObject).Deserialize(elementType, members, bindingFlags);
					array.SetValue(value, i);
				}
			}
			else
			{
				for (int j = 0; j < _count; j++)
				{
					object value2 = _items[j].Deserialize(elementType, bindingFlags);
					array.SetValue(value2, j);
				}
			}
			return array;
		}
		if (RealWorldTerrainReflectionHelper.IsGenericType(type))
		{
			Type type2 = RealWorldTerrainReflectionHelper.GetGenericArguments(type)[0];
			object obj = Activator.CreateInstance(type);
			if (_items[0] is RealWorldTerrainJsonObject)
			{
				IEnumerable<MemberInfo> members2 = RealWorldTerrainReflectionHelper.GetMembers(type2, BindingFlags.Instance | BindingFlags.Public);
				for (int k = 0; k < _count; k++)
				{
					object obj2 = (_items[k] as RealWorldTerrainJsonObject).Deserialize(type2, members2);
					try
					{
						MethodInfo method = RealWorldTerrainReflectionHelper.GetMethod(type, "Add");
						if (method != null)
						{
							method.Invoke(obj, new object[1] { obj2 });
						}
					}
					catch
					{
					}
				}
			}
			else
			{
				for (int l = 0; l < _count; l++)
				{
					object obj4 = _items[l].Deserialize(type2);
					try
					{
						MethodInfo method2 = RealWorldTerrainReflectionHelper.GetMethod(type, "Add");
						if (method2 != null)
						{
							method2.Invoke(obj, new object[1] { obj4 });
						}
					}
					catch
					{
					}
				}
			}
			return obj;
		}
		return null;
	}

	private RealWorldTerrainJsonItem Get(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		if (key.StartsWith("//"))
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
		int result;
		if (key.Contains("/"))
		{
			int num = key.IndexOf("/");
			string text = key.Substring(0, num);
			string key2 = key.Substring(num + 1);
			if (text == "*")
			{
				RealWorldTerrainJsonArray realWorldTerrainJsonArray = new RealWorldTerrainJsonArray();
				for (int i = 0; i < _count; i++)
				{
					RealWorldTerrainJsonItem realWorldTerrainJsonItem = _items[i][key2];
					if (realWorldTerrainJsonItem != null)
					{
						realWorldTerrainJsonArray.Add(realWorldTerrainJsonItem);
					}
				}
				return realWorldTerrainJsonArray;
			}
			if (int.TryParse(text, out result))
			{
				if (result < 0 || result >= _count)
				{
					return null;
				}
				return _items[result][key2];
			}
		}
		if (key == "*")
		{
			return this;
		}
		if (int.TryParse(key, out result))
		{
			return this[result];
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
		for (int i = 0; i < _count; i++)
		{
			realWorldTerrainJsonItem = _items[i];
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
		return _items.GetEnumerator();
	}

	public static RealWorldTerrainJsonArray ParseArray(string json)
	{
		return RealWorldTerrainJson.Parse(json) as RealWorldTerrainJsonArray;
	}

	public override void ToJSON(StringBuilder b)
	{
		b.Append("[");
		for (int i = 0; i < _count; i++)
		{
			if (i != 0)
			{
				b.Append(",");
			}
			_items[i].ToJSON(b);
		}
		b.Append("]");
	}

	public override object Value(Type type)
	{
		if (RealWorldTerrainReflectionHelper.IsValueType(type))
		{
			return Activator.CreateInstance(type);
		}
		return null;
	}
}
