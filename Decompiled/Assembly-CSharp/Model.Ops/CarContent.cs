using System;
using JetBrains.Annotations;
using Model.Ops.Definition;

namespace Model.Ops;

[Serializable]
public struct CarContent
{
	[CanBeNull]
	public Load load;

	public string description;

	public bool IsEmpty => load == null;

	public CarContent(Load load, string description)
	{
		this.load = load;
		this.description = description;
	}

	public static CarContent Empty()
	{
		return new CarContent(null, "Empty");
	}

	public override string ToString()
	{
		if (IsEmpty)
		{
			return "Empty";
		}
		string arg = (string.IsNullOrEmpty(description) ? load.description : description);
		return $"\"{arg}\" {load.units}";
	}

	public bool Equals(CarContent other)
	{
		return object.Equals(load, other.load);
	}

	public override bool Equals(object obj)
	{
		if (obj is CarContent other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (!(load != null))
		{
			return 0;
		}
		return load.GetHashCode();
	}
}
