using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct BoolPropertyValue : IPropertyValue
{
	[Key(0)]
	public bool Value;

	public BoolPropertyValue(bool value)
	{
		Value = value;
	}

	public override string ToString()
	{
		return Value.ToString();
	}
}
