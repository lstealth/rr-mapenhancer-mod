using System;
using System.Reflection;
using System.Xml;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

public static class RealWorldTerrainXMLExt
{
	public static T GetAttribute<T>(XmlNode node, string name)
	{
		XmlAttribute xmlAttribute = node.Attributes[name];
		if (xmlAttribute == null)
		{
			return default(T);
		}
		string value = xmlAttribute.Value;
		if (string.IsNullOrEmpty(value))
		{
			return default(T);
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(string))
		{
			return (T)Convert.ChangeType(value, typeFromHandle);
		}
		T val = default(T);
		PropertyInfo[] properties = typeFromHandle.GetProperties();
		Type type = typeFromHandle;
		if (properties.Length == 2 && string.Equals(properties[0].Name, "HasValue", StringComparison.InvariantCultureIgnoreCase))
		{
			type = properties[1].PropertyType;
		}
		try
		{
			MethodInfo method = type.GetMethod("Parse", new Type[2]
			{
				typeof(string),
				typeof(IFormatProvider)
			});
			if (method != null)
			{
				return (T)method.Invoke(null, new object[2]
				{
					value,
					RealWorldTerrainCultureInfo.numberFormat
				});
			}
			method = type.GetMethod("Parse", new Type[1] { typeof(string) });
			MethodInfo methodInfo = method;
			object[] parameters = new string[1] { value };
			return (T)methodInfo.Invoke(null, parameters);
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);
			throw;
		}
	}
}
