using MessagePack;

[MessagePackObject(false)]
public struct SerializableEntityReference
{
	[Key("type")]
	public EntityType Type;

	[Key("id")]
	public string Id;

	public SerializableEntityReference(EntityReference r)
	{
		Type = r.Type;
		Id = r.Id;
	}

	public SerializableEntityReference(EntityType type, string id)
	{
		Type = type;
		Id = id;
	}
}
