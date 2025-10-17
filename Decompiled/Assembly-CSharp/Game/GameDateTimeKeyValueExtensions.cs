using KeyValue.Runtime;

namespace Game;

public static class GameDateTimeKeyValueExtensions
{
	public static GameDateTime GameDateTime(this Value value, GameDateTime fallback)
	{
		return value.Type switch
		{
			ValueType.Int => new GameDateTime(value.IntValue), 
			ValueType.Float => new GameDateTime(value.FloatValue), 
			_ => fallback, 
		};
	}

	public static Value KeyValueValue(this GameDateTime dateTime)
	{
		return Value.Float((float)dateTime.TotalSeconds);
	}
}
