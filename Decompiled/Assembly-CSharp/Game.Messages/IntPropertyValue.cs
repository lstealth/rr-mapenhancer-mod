using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct IntPropertyValue : IPropertyValue
{
	[Key(0)]
	public int Value;

	public IntPropertyValue(int value)
	{
		Value = value;
	}

	public override string ToString()
	{
		return Value.ToString();
	}
}
