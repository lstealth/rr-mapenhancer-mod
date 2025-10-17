using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct FloatPropertyValue : IPropertyValue
{
	[Key(0)]
	public float Value;

	public FloatPropertyValue(float value)
	{
		Value = value;
	}

	public override string ToString()
	{
		return Value.ToString("F3");
	}
}
