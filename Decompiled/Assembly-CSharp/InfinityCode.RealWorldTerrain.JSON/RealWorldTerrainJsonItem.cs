using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace InfinityCode.RealWorldTerrain.JSON;

public abstract class RealWorldTerrainJsonItem : IEnumerable<RealWorldTerrainJsonItem>, IEnumerable
{
	public abstract RealWorldTerrainJsonItem this[int index] { get; }

	public abstract RealWorldTerrainJsonItem this[string key] { get; }

	public virtual RealWorldTerrainJsonItem AppendObject(object obj)
	{
		throw new Exception("AppendObject is only allowed for RealWorldTerrainJsonObject.");
	}

	public T ChildValue<T>(string childName)
	{
		RealWorldTerrainJsonItem realWorldTerrainJsonItem = this[childName];
		if (realWorldTerrainJsonItem == null)
		{
			return default(T);
		}
		return realWorldTerrainJsonItem.Value<T>();
	}

	public T Deserialize<T>(BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
	{
		return (T)Deserialize(typeof(T), bindingFlags);
	}

	public abstract object Deserialize(Type type, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public);

	public abstract RealWorldTerrainJsonItem GetAll(string key);

	public abstract void ToJSON(StringBuilder b);

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual IEnumerator<RealWorldTerrainJsonItem> GetEnumerator()
	{
		return null;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		ToJSON(stringBuilder);
		return stringBuilder.ToString();
	}

	public abstract object Value(Type type);

	public virtual T Value<T>()
	{
		return (T)Value(typeof(T));
	}

	public T V<T>()
	{
		return Value<T>();
	}

	public T V<T>(string childName)
	{
		return ChildValue<T>(childName);
	}
}
