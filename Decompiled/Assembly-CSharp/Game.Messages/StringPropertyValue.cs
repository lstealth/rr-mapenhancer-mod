using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct StringPropertyValue : IPropertyValue
{
	[Key(0)]
	public string Value;

	public StringPropertyValue(string value)
	{
		Value = value;
	}

	public override string ToString()
	{
		return "'" + Value + "'";
	}
}
