using System.Collections.Generic;
using KeyValue.Runtime;
using Model.Definition;
using Model.Definition.Data;

namespace Model;

public struct CarDescriptor
{
	public readonly TypedContainerItem<CarDefinition> DefinitionInfo;

	public CarIdent Ident;

	public readonly string Bardo;

	public readonly string TrainCrewId;

	public bool Flipped;

	public readonly Dictionary<string, Value> Properties;

	public CarDescriptor(TypedContainerItem<CarDefinition> definitionInfo, CarIdent ident = default(CarIdent), string bardo = null, string trainCrewId = null, bool flipped = false, Dictionary<string, Value> properties = null)
	{
		DefinitionInfo = definitionInfo;
		Ident = ident;
		Bardo = bardo;
		TrainCrewId = trainCrewId;
		Flipped = flipped;
		Properties = ((properties == null) ? new Dictionary<string, Value>() : new Dictionary<string, Value>(properties));
	}

	public override string ToString()
	{
		return "CarDescriptor(" + DefinitionInfo.Identifier + ")";
	}
}
