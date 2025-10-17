using System;
using System.Collections.Generic;
using System.Reflection;

namespace InfinityCode.RealWorldTerrain.Utils;

public static class RealWorldTerrainReflectionHelper
{
	private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

	public static bool CheckIfAnonymousType(Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (IsGenericType(type) && (type.Name.Contains("AnonymousType") || type.Name.Contains("AnonType")) && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")))
		{
			return (GetAttributes(type) & TypeAttributes.NotPublic) == 0;
		}
		return false;
	}

	public static TypeAttributes GetAttributes(Type type)
	{
		return type.Attributes;
	}

	public static IEnumerable<FieldInfo> GetFields(Type type, BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
	{
		return type.GetFields(bindingAttr);
	}

	public static Type[] GetGenericArguments(Type type)
	{
		return type.GetGenericArguments();
	}

	public static MemberInfo GetMember(Type type, string name)
	{
		MemberInfo[] member = type.GetMember(name);
		if (member.Length != 0)
		{
			return member[0];
		}
		return null;
	}

	public static IEnumerable<MemberInfo> GetMembers(Type type, BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
	{
		return type.GetMembers(bindingAttr);
	}

	public static MemberTypes GetMemberType(MemberInfo member)
	{
		return member.MemberType;
	}

	public static MethodInfo GetMethod(Type type, string name)
	{
		return type.GetMethod(name);
	}

	public static MethodInfo GetMethod(Type type, string name, Type[] types)
	{
		return type.GetMethod(name, types);
	}

	public static PropertyInfo[] GetProperties(Type type)
	{
		return type.GetProperties();
	}

	public static bool IsClass(Type type)
	{
		return type.IsClass;
	}

	public static bool IsGenericType(Type type)
	{
		return type.IsGenericType;
	}

	public static bool IsValueType(Type type)
	{
		return type.IsValueType;
	}
}
