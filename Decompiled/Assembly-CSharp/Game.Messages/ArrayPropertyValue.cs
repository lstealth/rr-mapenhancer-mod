using System.Collections.Generic;
using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct ArrayPropertyValue : IPropertyValue
{
	[Key(0)]
	public List<IPropertyValue> Value;

	public ArrayPropertyValue(List<IPropertyValue> value)
	{
		Value = value;
	}

	public override string ToString()
	{
		return "[" + string.Join(", ", Value) + "]";
	}
}
