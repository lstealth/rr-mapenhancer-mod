using System;
using System.Collections.Generic;
using System.Linq;
using KeyValue.Runtime;

namespace Game.State;

public struct PlayerProperties
{
	public HashSet<string> FavoriteEngineIds;

	public string SelectedCarId;

	private const string KeyFavoriteEngineIds = "favoriteEngineIds";

	private const string KeySelectedCarId = "selectedCarId";

	public PlayerProperties(Value obj)
	{
		FavoriteEngineIds = obj["favoriteEngineIds"].ArrayValue.Select((Value i) => i.StringValue).ToHashSet();
		SelectedCarId = obj["selectedCarId"];
	}

	public Value Value()
	{
		return KeyValue.Runtime.Value.Dictionary(new Dictionary<string, Value>
		{
			{
				"favoriteEngineIds",
				KeyValue.Runtime.Value.Array((IReadOnlyCollection<Value>)(object)((IEnumerable<string>)FavoriteEngineIds).Select((Func<string, Value>)((string id) => id)).ToArray())
			},
			{ "selectedCarId", SelectedCarId }
		});
	}
}
