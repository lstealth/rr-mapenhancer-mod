using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Game.Scripting;

public static class ScriptVector3
{
	public static void AddVec3Type(Script script)
	{
		Table value = new Table(script)
		{
			["new"] = (Func<float, float, float, Table>)((float x, float y, float z) => New(script, x, y, z)),
			["sub"] = (Func<Table, Table, Table>)((Table a, Table b) => Sub(script, a, b)),
			["distance"] = new Func<Table, Table, float>(Distance),
			["magnitude"] = new Func<Table, float>(Magnitude)
		};
		script.Globals["vec3"] = value;
	}

	private static float Distance(Table a, Table b)
	{
		return Vector3.Distance(FromTable(a), FromTable(b));
	}

	private static Table New(Script script, float x, float y, float z)
	{
		Table table = DynValue.NewTable(script).Table;
		table["x"] = x;
		table["y"] = y;
		table["z"] = z;
		return table;
	}

	private static Table Sub(Script script, Table a, Table b)
	{
		Table table = DynValue.NewTable(script).Table;
		table["x"] = (double)a["x"] - (double)b["x"];
		table["y"] = (double)a["y"] - (double)b["y"];
		table["z"] = (double)a["z"] - (double)b["z"];
		return table;
	}

	private static float Magnitude(Table v)
	{
		return Vector3.Magnitude(FromTable(v));
	}

	public static Vector3 FromTable(Table v)
	{
		float x = (float)(double)v["x"];
		float y = (float)(double)v["y"];
		float z = (float)(double)v["z"];
		return new Vector3(x, y, z);
	}

	public static Dictionary<string, float> DictionaryRepresentation(Vector3 vec)
	{
		return new Dictionary<string, float>
		{
			{ "x", vec.x },
			{ "y", vec.y },
			{ "z", vec.z }
		};
	}
}
