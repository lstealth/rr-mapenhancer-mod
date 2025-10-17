using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AssetPack.Runtime;
using Model.Definition;
using Model.Definition.Data;
using RollingStock;
using UnityEngine;

namespace Model.Database;

public interface IPrefabStore
{
	IEnumerable<TypedContainerItem<CarDefinition>> AllCarDefinitionInfos { get; }

	Task<Wheelset> TruckPrefabForId(string truckIdentifier);

	T DefinitionForIdentifier<T>(string definitionIdentifier, out ObjectMetadata metadata);

	IEnumerable<TypedContainerItem<TDefinition>> AllDefinitionInfosOfType<TDefinition>() where TDefinition : ObjectDefinition;

	TypedContainerItem<CarDefinition> CarDefinitionInfoForIdentifier(string identifier);

	Task<LoadedAssetReference<T>> LoadAssetAsync<T>(string assetPackIdentifier, string assetIdentifier, CancellationToken cancellationToken) where T : Object;

	string AssetPackIdentifierContainingDefinition(string definitionIdentifier);

	AbsoluteAssetReference ResolveAssetReference(string contextualDefinitionIdentifier, AssetReference assetReference);
}
