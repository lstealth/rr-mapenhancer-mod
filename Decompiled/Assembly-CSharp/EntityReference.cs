using System;
using System.Linq;
using Game;
using Game.State;
using Model.Ops;
using UnityEngine;

public struct EntityReference
{
	public EntityType Type;

	public string Id;

	public EntityReference(EntityType type, string id)
	{
		Type = type;
		Id = id;
	}

	public EntityReference(SerializableEntityReference p)
	{
		Type = p.Type;
		Id = p.Id;
	}

	public EntityReference(PlayerId playerId)
		: this(EntityType.Player, playerId.String)
	{
	}

	public EntityReference(EntityType type, Vector4 v)
	{
		Type = type;
		Id = $"{Mathf.RoundToInt(v.x)},{Mathf.CeilToInt(v.y)},{Mathf.RoundToInt(v.z)},{Mathf.RoundToInt(v.w)}";
	}

	public bool TryParseVector4(out Vector4 vector)
	{
		string[] array = Id.Split(",");
		if (array.Length != 4)
		{
			vector = Vector4.zero;
			return false;
		}
		if (int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2) && int.TryParse(array[2], out var result3) && int.TryParse(array[3], out var result4))
		{
			vector = new Vector4(result, result2, result3, result4);
			return true;
		}
		vector = Vector4.zero;
		return false;
	}

	public string URI()
	{
		return Type switch
		{
			EntityType.Industry => "industry", 
			EntityType.Car => "car", 
			EntityType.Crew => "crew", 
			EntityType.PassengerStop => "passstop", 
			EntityType.Player => "player", 
			EntityType.Position => "pos", 
			EntityType.Timetable => "tt", 
			_ => throw new ArgumentOutOfRangeException($"Unrecognized type {Type}"), 
		} + ":" + Id;
	}

	public static string URI(EntityType et, string id)
	{
		return new EntityReference(et, id).URI();
	}

	public static bool TryParseURI(string link, out EntityReference r)
	{
		int num = link.IndexOf(':');
		if (num == -1)
		{
			r = default(EntityReference);
			return false;
		}
		string text = link.Substring(0, num);
		int num2 = num + 1;
		string id = link.Substring(num2, link.Length - num2);
		EntityType type;
		switch (text)
		{
		case "help":
			type = EntityType.Help;
			break;
		case "industry":
			type = EntityType.Industry;
			break;
		case "car":
			type = EntityType.Car;
			break;
		case "crew":
			type = EntityType.Crew;
			break;
		case "passstop":
			type = EntityType.PassengerStop;
			break;
		case "player":
			type = EntityType.Player;
			break;
		case "pos":
			type = EntityType.Position;
			break;
		case "tt":
			type = EntityType.Timetable;
			break;
		default:
			r = default(EntityReference);
			return false;
		}
		r = new EntityReference(type, id);
		return true;
	}

	public string Text()
	{
		string id = Id;
		return Type switch
		{
			EntityType.Industry => OpsController.Shared?.AllIndustries.FirstOrDefault((Industry i) => i.identifier == id)?.name ?? "Unknown Industry", 
			EntityType.Car => TrainController.Shared.CarForId(id)?.DisplayName ?? "Unknown Car", 
			EntityType.PassengerStop => PassengerStop.FindAll().FirstOrDefault((PassengerStop ps) => ps.identifier == id)?.DisplayName ?? "Unknown Passenger Stop", 
			EntityType.Player => StateManager.Shared.PlayersManager.PlayerForId(new PlayerId(id))?.Name ?? "Unknown Player", 
			EntityType.Timetable => "Timetable", 
			_ => "Unknown", 
		};
	}
}
