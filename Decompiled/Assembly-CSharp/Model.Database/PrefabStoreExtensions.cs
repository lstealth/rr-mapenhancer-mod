using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssetPack.Runtime;
using Helpers;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using UnityEngine;

namespace Model.Database;

public static class PrefabStoreExtensions
{
	public static Task<LoadedAssetReference<T>> LoadAssetAsync<T>(this IPrefabStore prefabStore, AbsoluteAssetReference assetReference, CancellationToken cancellationToken) where T : UnityEngine.Object
	{
		return prefabStore.LoadAssetAsync<T>(assetReference.AssetPackIdentifier, assetReference.AssetIdentifier, cancellationToken);
	}

	public static TypedContainerItem<CarDefinition> Random(this IPrefabStore prefabStore, CarTypeFilter carTypeFilter, IndustryContext.CarSizePreference sizePreference, System.Random rnd)
	{
		List<TypedContainerItem<CarDefinition>> list = (from p in prefabStore.AllCarDefinitionInfos.ToList().FindAll((TypedContainerItem<CarDefinition> p) => carTypeFilter.Matches(p.Definition.CarType))
			orderby p.Definition.WeightEmpty
			select p).ToList();
		if (list.Count == 0)
		{
			Debug.LogError($"Couldn't find car for condition: {carTypeFilter}");
			return null;
		}
		return list.RandomElementUsingNormalDistribution(sizePreference switch
		{
			IndustryContext.CarSizePreference.Small => 0.2f, 
			IndustryContext.CarSizePreference.Medium => 0.4f, 
			IndustryContext.CarSizePreference.Large => 0.6f, 
			IndustryContext.CarSizePreference.ExtraLarge => 0.8f, 
			_ => 0.5f, 
		}, rnd);
	}
}
