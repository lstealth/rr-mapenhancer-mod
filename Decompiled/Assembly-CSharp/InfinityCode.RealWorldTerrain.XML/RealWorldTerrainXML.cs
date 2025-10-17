using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using InfinityCode.RealWorldTerrain.Utils;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.XML;

public class RealWorldTerrainXML : IEnumerable
{
	private XmlDocument _document;

	private XmlElement _element;

	public string name
	{
		get
		{
			if (_element == null)
			{
				return null;
			}
			return _element.Name;
		}
	}

	public int count
	{
		get
		{
			if (!hasChildNodes)
			{
				return 0;
			}
			return _element.ChildNodes.Count;
		}
	}

	public bool isNull
	{
		get
		{
			if (_document != null)
			{
				return _element == null;
			}
			return true;
		}
	}

	public XmlDocument document => _document;

	public XmlElement element => _element;

	public bool hasAttributes
	{
		get
		{
			if (_element != null)
			{
				return _element.Attributes.Count > 0;
			}
			return false;
		}
	}

	public bool hasChildNodes
	{
		get
		{
			if (_element != null)
			{
				return _element.HasChildNodes;
			}
			return false;
		}
	}

	public string outerXml
	{
		get
		{
			if (_element == null)
			{
				return null;
			}
			return _element.OuterXml;
		}
	}

	public RealWorldTerrainXML this[int index]
	{
		get
		{
			if (!hasChildNodes)
			{
				return new RealWorldTerrainXML();
			}
			if (index < 0 || index >= _element.ChildNodes.Count)
			{
				return new RealWorldTerrainXML();
			}
			return new RealWorldTerrainXML(_element.ChildNodes[index] as XmlElement);
		}
	}

	public RealWorldTerrainXML this[string childName]
	{
		get
		{
			if (!hasChildNodes)
			{
				return new RealWorldTerrainXML();
			}
			return new RealWorldTerrainXML(_element[childName]);
		}
	}

	public RealWorldTerrainXML()
	{
	}

	public RealWorldTerrainXML(string nodeName)
	{
		try
		{
			_document = new XmlDocument();
			_element = _document.CreateElement(nodeName);
			_document.AppendChild(_element);
		}
		catch (Exception)
		{
			_document = null;
			_element = null;
		}
	}

	public RealWorldTerrainXML(XmlElement xmlElement)
	{
		if (xmlElement != null)
		{
			_element = xmlElement;
			_document = _element.OwnerDocument;
		}
	}

	public string A(string attributeName)
	{
		return A<string>(attributeName);
	}

	public T A<T>(string attributeName)
	{
		if (!hasAttributes)
		{
			return default(T);
		}
		XmlAttribute xmlAttribute = _element.Attributes[attributeName];
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
		T result = default(T);
		PropertyInfo[] properties = RealWorldTerrainReflectionHelper.GetProperties(typeFromHandle);
		Type type = typeFromHandle;
		if (properties.Length == 2 && string.Equals(properties[0].Name, "HasValue", StringComparison.InvariantCultureIgnoreCase))
		{
			type = properties[1].PropertyType;
		}
		MethodInfo method = RealWorldTerrainReflectionHelper.GetMethod(type, "Parse", new Type[2]
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
		method = RealWorldTerrainReflectionHelper.GetMethod(type, "Parse", new Type[1] { typeof(string) });
		if (method != null)
		{
			MethodInfo methodInfo = method;
			object[] parameters = new string[1] { value };
			return (T)methodInfo.Invoke(null, parameters);
		}
		return result;
	}

	public void A(string attributeName, object value)
	{
		if (_element != null)
		{
			_element.SetAttribute(attributeName, value.ToString());
		}
	}

	public void A(string attributeName, Color32 value)
	{
		A(attributeName, value.r.ToString("X2") + value.g.ToString("X2") + value.b.ToString("X2"));
	}

	public void AppendChild(XmlElement newChild)
	{
		if (_element != null && newChild != null)
		{
			if (_element.OwnerDocument != newChild.OwnerDocument)
			{
				newChild = _element.OwnerDocument.ImportNode(newChild, deep: true) as XmlElement;
			}
			_element.AppendChild(newChild);
		}
	}

	public void AppendChild(RealWorldTerrainXML newChild)
	{
		if (newChild != null)
		{
			AppendChild(newChild._element);
		}
	}

	public void AppendChilds(IEnumerable<XmlNode> list)
	{
		if (_element == null)
		{
			return;
		}
		foreach (XmlNode item in list)
		{
			_element.AppendChild(item);
		}
	}

	public void AppendChilds(IEnumerable<RealWorldTerrainXML> list)
	{
		if (_element == null)
		{
			return;
		}
		foreach (RealWorldTerrainXML item in list)
		{
			if (item._element != null)
			{
				_element.AppendChild(item._element);
			}
		}
	}

	public void AppendChilds(XmlNodeList list)
	{
		if (_element == null)
		{
			return;
		}
		foreach (XmlNode item in list)
		{
			_element.AppendChild(item);
		}
	}

	public void AppendChilds(RealWorldTerrainXMLList list)
	{
		if (_element == null)
		{
			return;
		}
		foreach (RealWorldTerrainXML item in list)
		{
			if (item._element != null)
			{
				_element.AppendChild(item._element);
			}
		}
	}

	public RealWorldTerrainXML Create(string nodeName)
	{
		if (_document == null || _element == null)
		{
			return new RealWorldTerrainXML();
		}
		XmlElement xmlElement = _document.CreateElement(nodeName);
		_element.AppendChild(xmlElement);
		return new RealWorldTerrainXML(xmlElement);
	}

	public RealWorldTerrainXML Create(string nodeName, bool value)
	{
		return Create(nodeName, value ? "True" : "False");
	}

	public RealWorldTerrainXML Create(string nodeName, Color32 value)
	{
		return Create(nodeName, value.r.ToString("X2") + value.g.ToString("X2") + value.b.ToString("X2"));
	}

	public RealWorldTerrainXML Create(string nodeName, float value)
	{
		return Create(nodeName, value.ToString());
	}

	public RealWorldTerrainXML Create(string nodeName, double value)
	{
		return Create(nodeName, value.ToString());
	}

	public RealWorldTerrainXML Create(string nodeName, int value)
	{
		return Create(nodeName, value.ToString());
	}

	public RealWorldTerrainXML Create(string nodeName, UnityEngine.Object value)
	{
		return Create(nodeName, (value != null) ? value.GetInstanceID() : 0);
	}

	public RealWorldTerrainXML Create(string nodeName, string value)
	{
		RealWorldTerrainXML realWorldTerrainXML = Create(nodeName);
		realWorldTerrainXML.SetChild(value);
		return realWorldTerrainXML;
	}

	public RealWorldTerrainXML Create(string nodeName, Vector2 value)
	{
		RealWorldTerrainXML realWorldTerrainXML = Create(nodeName);
		realWorldTerrainXML.Create("X", value.x);
		realWorldTerrainXML.Create("Y", value.y);
		return realWorldTerrainXML;
	}

	public RealWorldTerrainXML Create(string nodeName, Vector3 value)
	{
		RealWorldTerrainXML realWorldTerrainXML = Create(nodeName);
		realWorldTerrainXML.Create("X", value.x);
		realWorldTerrainXML.Create("Y", value.y);
		realWorldTerrainXML.Create("Z", value.z);
		return realWorldTerrainXML;
	}

	public RealWorldTerrainXML Find(string xpath, XmlNamespaceManager nsmgr = null)
	{
		if (!hasChildNodes)
		{
			return new RealWorldTerrainXML();
		}
		if (_element.SelectSingleNode(xpath, nsmgr) is XmlElement xmlElement)
		{
			return new RealWorldTerrainXML(xmlElement);
		}
		return new RealWorldTerrainXML();
	}

	public T Find<T>(string xpath, XmlNamespaceManager nsmgr = null)
	{
		if (!hasChildNodes)
		{
			return default(T);
		}
		return Get<T>(_element.SelectSingleNode(xpath, nsmgr) as XmlElement);
	}

	public RealWorldTerrainXMLList FindAll(string xpath, XmlNamespaceManager nsmgr = null)
	{
		if (!hasChildNodes)
		{
			return new RealWorldTerrainXMLList();
		}
		return new RealWorldTerrainXMLList(_element.SelectNodes(xpath, nsmgr));
	}

	public string Get(string childName)
	{
		return Get<string>(childName);
	}

	public T Get<T>(XmlElement el)
	{
		if (el == null)
		{
			return default(T);
		}
		string innerXml = el.InnerXml;
		if (string.IsNullOrEmpty(innerXml))
		{
			return default(T);
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(string))
		{
			return (T)Convert.ChangeType(innerXml, typeFromHandle);
		}
		if (typeFromHandle == typeof(Color) || typeFromHandle == typeof(Color32))
		{
			return (T)Convert.ChangeType(RealWorldTerrainUtils.HexToColor(innerXml), typeFromHandle);
		}
		if (typeFromHandle == typeof(Vector2))
		{
			return (T)Convert.ChangeType(new Vector2(Get<float>(el["X"]), Get<float>(el["Y"])), typeFromHandle);
		}
		if (typeFromHandle == typeof(Vector3))
		{
			return (T)Convert.ChangeType(new Vector3(Get<float>(el["X"]), Get<float>(el["Y"]), Get<float>(el["Z"])), typeFromHandle);
		}
		T val = default(T);
		PropertyInfo[] properties = RealWorldTerrainReflectionHelper.GetProperties(typeFromHandle);
		Type type = typeFromHandle;
		if (properties.Length == 2 && string.Equals(properties[0].Name, "HasValue", StringComparison.InvariantCultureIgnoreCase))
		{
			type = properties[1].PropertyType;
		}
		try
		{
			MethodInfo method = RealWorldTerrainReflectionHelper.GetMethod(type, "Parse", new Type[2]
			{
				typeof(string),
				typeof(IFormatProvider)
			});
			if (method != null)
			{
				return (T)method.Invoke(null, new object[2]
				{
					innerXml,
					RealWorldTerrainCultureInfo.numberFormat
				});
			}
			method = RealWorldTerrainReflectionHelper.GetMethod(type, "Parse", new Type[1] { typeof(string) });
			MethodInfo methodInfo = method;
			object[] parameters = new string[1] { innerXml };
			return (T)methodInfo.Invoke(null, parameters);
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);
			throw;
		}
	}

	public T Get<T>(XmlElement el, T defaultValue)
	{
		if (el == null)
		{
			return defaultValue;
		}
		string innerXml = el.InnerXml;
		if (string.IsNullOrEmpty(innerXml))
		{
			return defaultValue;
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(string))
		{
			return (T)Convert.ChangeType(innerXml, typeFromHandle);
		}
		if (typeFromHandle == typeof(Color) || typeFromHandle == typeof(Color32))
		{
			return (T)Convert.ChangeType(RealWorldTerrainUtils.HexToColor(innerXml), typeFromHandle);
		}
		if (typeFromHandle == typeof(Vector2))
		{
			return (T)Convert.ChangeType(new Vector2(Get<float>(el["X"]), Get<float>(el["Y"])), typeFromHandle);
		}
		if (typeFromHandle == typeof(Vector3))
		{
			return (T)Convert.ChangeType(new Vector3(Get<float>(el["X"]), Get<float>(el["Y"]), Get<float>(el["Z"])), typeFromHandle);
		}
		T val = defaultValue;
		PropertyInfo[] properties = RealWorldTerrainReflectionHelper.GetProperties(typeFromHandle);
		Type type = typeFromHandle;
		if (properties.Length == 2 && string.Equals(properties[0].Name, "HasValue", StringComparison.InvariantCultureIgnoreCase))
		{
			type = properties[1].PropertyType;
		}
		try
		{
			MethodInfo method = RealWorldTerrainReflectionHelper.GetMethod(type, "Parse", new Type[2]
			{
				typeof(string),
				typeof(IFormatProvider)
			});
			if (method != null)
			{
				return (T)method.Invoke(null, new object[2]
				{
					innerXml,
					RealWorldTerrainCultureInfo.numberFormat
				});
			}
			method = RealWorldTerrainReflectionHelper.GetMethod(type, "Parse", new Type[1] { typeof(string) });
			MethodInfo methodInfo = method;
			object[] parameters = new string[1] { innerXml };
			return (T)methodInfo.Invoke(null, parameters);
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);
			throw;
		}
	}

	public T Get<T>(string childName)
	{
		if (!hasChildNodes)
		{
			return default(T);
		}
		return Get<T>(_element[childName]);
	}

	public T Get<T>(string childName, T defaultValue)
	{
		if (!hasChildNodes)
		{
			return defaultValue;
		}
		return Get(_element[childName], defaultValue);
	}

	public IEnumerator GetEnumerator()
	{
		for (int i = 0; i < count; i++)
		{
			yield return this[i];
		}
	}

	public Vector2 GetLatLng(string subNodeName)
	{
		RealWorldTerrainXML realWorldTerrainXML = this[subNodeName];
		return new Vector2(realWorldTerrainXML.Get<float>("lng"), realWorldTerrainXML.Get<float>("lat"));
	}

	public RealWorldTerrainXMLNamespaceManager GetNamespaceManager(string prefix = null)
	{
		RealWorldTerrainXMLNamespaceManager realWorldTerrainXMLNamespaceManager = new RealWorldTerrainXMLNamespaceManager(document.NameTable);
		if (prefix == null)
		{
			prefix = element.GetPrefixOfNamespace(element.NamespaceURI);
		}
		realWorldTerrainXMLNamespaceManager.AddNamespace(prefix, element.NamespaceURI);
		return realWorldTerrainXMLNamespaceManager;
	}

	public bool HasChild(string childName)
	{
		if (!hasChildNodes)
		{
			return false;
		}
		return _element[childName] != null;
	}

	public static Vector2 GetVector2FromNode(RealWorldTerrainXML node)
	{
		float x = node.Get<float>("lng");
		float y = node.Get<float>("lat");
		return new Vector2(x, y);
	}

	public static RealWorldTerrainXML Load(string xmlString)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xmlString);
			return new RealWorldTerrainXML(xmlDocument.DocumentElement);
		}
		catch
		{
			Debug.Log("Can not load XML from string:\n" + xmlString);
			return new RealWorldTerrainXML();
		}
	}

	public void Remove()
	{
		if (_element != null && _element.ParentNode != null)
		{
			_element.ParentNode.RemoveChild(_element);
		}
	}

	public void Remove(string childName)
	{
		if (hasChildNodes)
		{
			_element.RemoveChild(_element[childName]);
		}
	}

	public void Remove(int childIndex)
	{
		if (hasChildNodes && childIndex >= 0 && childIndex < _element.ChildNodes.Count)
		{
			_element.RemoveChild(_element.ChildNodes[childIndex]);
		}
	}

	private void SetChild(string value)
	{
		if (_element != null && _document != null)
		{
			_element.AppendChild(_document.CreateTextNode(value));
		}
	}

	public string Value()
	{
		return Value<string>();
	}

	public T Value<T>()
	{
		return Get<T>(_element);
	}
}
