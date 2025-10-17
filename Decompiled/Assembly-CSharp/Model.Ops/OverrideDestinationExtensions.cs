using System;
using System.Collections.Generic;
using Game.State;
using JetBrains.Annotations;
using KeyValue.Runtime;

namespace Model.Ops;

public static class OverrideDestinationExtensions
{
	public static bool IsWriteAuthorized(this OverrideDestination od, Car car)
	{
		return StateManager.CheckAuthorizedToChangeProperty(car.id, od.Key());
	}

	public static string Key(this OverrideDestination od)
	{
		if (od == OverrideDestination.Repair)
		{
			return "ops.repair-dest";
		}
		throw new ArgumentOutOfRangeException("od", od, null);
	}

	public static bool HasOverrideDestination(this Car car, OverrideDestination type)
	{
		string key = type.Key();
		return !car.KeyValueObject[key].IsNull;
	}

	[ContractAnnotation("=> true, result: notnull; => false, result: null")]
	public static bool TryGetOverrideDestination(this Car car, OverrideDestination type, IOpsCarPositionResolver resolver, out (OpsCarPosition position, string tag)? result)
	{
		string key = type.Key();
		Value value = car.KeyValueObject[key];
		if (value.IsNull)
		{
			result = null;
			return false;
		}
		try
		{
			if (value.Type == KeyValue.Runtime.ValueType.String)
			{
				OpsCarPosition item = resolver.ResolveOpsCarPosition(value.StringValue);
				result = (item, null);
				return true;
			}
			Value value2 = value["id"];
			OpsCarPosition item2 = resolver.ResolveOpsCarPosition(value2);
			string stringValue = value["tag"].StringValue;
			result = (item2, string.IsNullOrEmpty(stringValue) ? null : stringValue);
			return true;
		}
		catch
		{
			result = null;
			return false;
		}
	}

	public static void SetOverrideDestination(this Car car, OverrideDestination type, (OpsCarPosition position, string tag)? tuple)
	{
		string key = type.Key();
		car.KeyValueObject[key] = ((!tuple.HasValue) ? Value.Null() : Value.Dictionary(new Dictionary<string, Value>
		{
			{
				"id",
				tuple.Value.position.Identifier
			},
			{
				"tag",
				tuple.Value.tag
			}
		}));
	}
}
