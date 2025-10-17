using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using InfinityCode.RealWorldTerrain.Utils;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.JSON;

public class RealWorldTerrainJsonValue : RealWorldTerrainJsonItem
{
	public enum ValueType
	{
		DOUBLE,
		LONG,
		STRING,
		BOOLEAN,
		NULL
	}

	private ValueType _type;

	private object _value;

	public override RealWorldTerrainJsonItem this[string key] => null;

	public override RealWorldTerrainJsonItem this[int index] => null;

	public object value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value == null || value is DBNull)
			{
				_type = ValueType.NULL;
				_value = value;
				return;
			}
			if (value is string)
			{
				_type = ValueType.STRING;
				_value = value;
				return;
			}
			if (value is double)
			{
				_type = ValueType.DOUBLE;
				_value = (double)value;
				return;
			}
			if (value is float)
			{
				_type = ValueType.DOUBLE;
				_value = (double)(float)value;
				return;
			}
			if (value is bool)
			{
				_type = ValueType.BOOLEAN;
				_value = value;
				return;
			}
			if (value is long)
			{
				_type = ValueType.LONG;
				_value = value;
				return;
			}
			if (value is int || value is short || value is byte)
			{
				_type = ValueType.LONG;
				_value = Convert.ChangeType(value, typeof(long));
				return;
			}
			throw new Exception("Unknown type of value.");
		}
	}

	public ValueType type => _type;

	public RealWorldTerrainJsonValue(object value)
	{
		this.value = value;
	}

	public RealWorldTerrainJsonValue(object value, ValueType type)
	{
		_value = value;
		_type = type;
	}

	public override object Deserialize(Type type, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		return Value(type);
	}

	public override RealWorldTerrainJsonItem GetAll(string key)
	{
		return null;
	}

	public override void ToJSON(StringBuilder b)
	{
		if (_type == ValueType.STRING)
		{
			WriteString(b);
		}
		else if (_type == ValueType.NULL)
		{
			b.Append("null");
		}
		else if (_type == ValueType.BOOLEAN)
		{
			b.Append(((bool)_value) ? "true" : "false");
		}
		else if (_type == ValueType.DOUBLE)
		{
			b.Append(((double)value).ToString(RealWorldTerrainCultureInfo.cultureInfo));
		}
		else
		{
			b.Append(value);
		}
	}

	public override IEnumerator<RealWorldTerrainJsonItem> GetEnumerator()
	{
		yield return this;
	}

	public override string ToString()
	{
		if (type == ValueType.DOUBLE)
		{
			return ((double)value).ToString(RealWorldTerrainCultureInfo.cultureInfo);
		}
		return value.ToString();
	}

	public override object Value(Type t)
	{
		if (_type == ValueType.NULL || _value == null)
		{
			if (RealWorldTerrainReflectionHelper.IsValueType(t))
			{
				return Activator.CreateInstance(t);
			}
			return null;
		}
		if (t == typeof(string))
		{
			return Convert.ChangeType(_value, t);
		}
		if (_type == ValueType.BOOLEAN)
		{
			if (t == typeof(bool))
			{
				return Convert.ChangeType(_value, t);
			}
		}
		else if (_type == ValueType.DOUBLE)
		{
			if (t == typeof(double))
			{
				return Convert.ChangeType(_value, t, RealWorldTerrainCultureInfo.numberFormat);
			}
			if (t == typeof(float))
			{
				return Convert.ChangeType((double)_value, t, RealWorldTerrainCultureInfo.numberFormat);
			}
		}
		else
		{
			if (_type == ValueType.LONG)
			{
				if (t == typeof(long))
				{
					return Convert.ChangeType(_value, t);
				}
				try
				{
					return Convert.ChangeType((long)_value, t);
				}
				catch (Exception ex)
				{
					Debug.Log(ex.Message + "\n" + ex.StackTrace);
					return null;
				}
			}
			if (_type == ValueType.STRING)
			{
				MethodInfo method = RealWorldTerrainReflectionHelper.GetMethod(t, "Parse", new Type[2]
				{
					typeof(string),
					typeof(IFormatProvider)
				});
				if (method != null)
				{
					return method.Invoke(null, new object[2]
					{
						value,
						RealWorldTerrainCultureInfo.numberFormat
					});
				}
				method = RealWorldTerrainReflectionHelper.GetMethod(t, "Parse", new Type[1] { typeof(string) });
				return method.Invoke(null, new object[1] { value });
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		ToJSON(stringBuilder);
		throw new InvalidCastException(t.FullName + "\n" + stringBuilder);
	}

	private void WriteString(StringBuilder b)
	{
		b.Append('"');
		string text = value as string;
		int num = -1;
		int length = text.Length;
		for (int i = 0; i < length; i++)
		{
			char c = text[i];
			if (c >= ' ' && c < '\u0080' && c != '"' && c != '\\')
			{
				if (num == -1)
				{
					num = i;
				}
				continue;
			}
			if (num != -1)
			{
				b.Append(text, num, i - num);
				num = -1;
			}
			switch (c)
			{
			case '\t':
				b.Append("\\t");
				break;
			case '\r':
				b.Append("\\r");
				break;
			case '\n':
				b.Append("\\n");
				break;
			case '"':
			case '\\':
				b.Append('\\');
				b.Append(c);
				break;
			default:
			{
				b.Append("\\u");
				int num2 = c;
				b.Append(num2.ToString("X4", NumberFormatInfo.InvariantInfo));
				break;
			}
			}
		}
		if (num != -1)
		{
			b.Append(text, num, text.Length - num);
		}
		b.Append('"');
	}

	public static implicit operator string(RealWorldTerrainJsonValue val)
	{
		return val.ToString();
	}
}
