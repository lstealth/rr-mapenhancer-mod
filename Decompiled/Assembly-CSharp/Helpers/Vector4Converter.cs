using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Helpers;

public class Vector4Converter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(Vector4);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		return JsonConvert.DeserializeObject<Vector4>(serializer.Deserialize(reader).ToString());
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		Vector4 vector = (Vector4)value;
		writer.WriteStartObject();
		writer.WritePropertyName("x");
		writer.WriteValue(vector.x);
		writer.WritePropertyName("y");
		writer.WriteValue(vector.y);
		writer.WritePropertyName("z");
		writer.WriteValue(vector.z);
		writer.WritePropertyName("w");
		writer.WriteValue(vector.w);
		writer.WriteEndObject();
	}
}
