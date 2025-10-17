using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Helpers;

public class Vector2Converter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(Vector2);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		return JsonConvert.DeserializeObject<Vector2>(serializer.Deserialize(reader).ToString());
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		Vector2 vector = (Vector2)value;
		writer.WriteStartObject();
		writer.WritePropertyName("x");
		writer.WriteValue(vector.x);
		writer.WritePropertyName("y");
		writer.WriteValue(vector.y);
		writer.WriteEndObject();
	}
}
