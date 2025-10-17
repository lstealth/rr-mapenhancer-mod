using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Messages;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Map.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Track;
using UnityEngine;

namespace RollingStock;

public class BuilderPhotoController : MonoBehaviour
{
	[SerializeField]
	private Shader carShader;

	[SerializeField]
	private Shader windowShader;

	[SerializeField]
	private Camera photoCamera;

	[SerializeField]
	private TrackMarker marker;

	private void Awake()
	{
		foreach (Transform item in base.transform)
		{
			item.gameObject.SetActive(value: false);
		}
	}

	[ContextMenu("Prepare Scene")]
	public void PrepareScene()
	{
		CameraSelector.shared.JumpToPoint(base.transform.GamePosition(), base.transform.rotation, CameraSelector.CameraIdentifier.Strategy);
		MapManager.Instance.SetDensity(0f, 0f);
		StateManager.ApplyLocal(new SetTimeOfDay(43200f));
		foreach (Transform item in base.transform)
		{
			item.gameObject.SetActive(value: true);
		}
	}

	private IEnumerator Photograph(string id, Vector2Int outputSize, string outputPath)
	{
		Debug.Log("Photograph: " + id + " -----------------");
		TrainController tc = TrainController.Shared;
		IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
		Dictionary<string, Value> properties = new Dictionary<string, Value> { { "lettering.basic", "Atlantic Locomotive Works" } };
		List<CarDescriptor> list = new List<CarDescriptor>();
		TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.CarDefinitionInfoForIdentifier(id);
		list.Add(new CarDescriptor(typedContainerItem, new CarIdent("ALW", typedContainerItem.Definition.BaseRoadNumber), null, null, flipped: false, properties));
		if (typedContainerItem.Definition.TryGetTenderIdentifier(out var tenderIdentifier))
		{
			TypedContainerItem<CarDefinition> definitionInfo = prefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier);
			list.Add(new CarDescriptor(definitionInfo, new CarIdent("ALW", typedContainerItem.Definition.BaseRoadNumber + "T"), null, null, flipped: false, properties));
		}
		float num = list.Sum((CarDescriptor d) => d.DefinitionInfo.Definition.Length);
		Location loc = tc.graph.LocationByMoving(marker.Location.Value, num / 2f);
		tc.PlaceTrain(loc, list);
		while (tc.Cars.Count == 0)
		{
			yield return new WaitForSeconds(0.1f);
		}
		List<Car> cars = tc.Cars.ToList();
		while (cars.Any((Car car) => car.BodyTransform == null))
		{
			yield return new WaitForSeconds(0.1f);
		}
		yield return new WaitForSeconds(0.1f);
		foreach (Car item in cars)
		{
			item.ApplyBuilderPhotoMaterial(carShader, windowShader);
		}
		yield return null;
		RenderTexture temporary = RenderTexture.GetTemporary(outputSize.x, outputSize.y, 8, RenderTextureFormat.ARGB32);
		photoCamera.enabled = true;
		photoCamera.targetTexture = temporary;
		photoCamera.Render();
		photoCamera.targetTexture = null;
		photoCamera.enabled = false;
		Texture2D texture2D = RenderTextureToTexture2D(temporary);
		texture2D.name = "BuilderPhotoRendererTexture";
		byte[] bytes = texture2D.EncodeToPNG();
		File.WriteAllBytes(outputPath, bytes);
		Object.Destroy(texture2D);
		RenderTexture.ReleaseTemporary(temporary);
		foreach (Car item2 in cars)
		{
			tc.RemoveCar(item2.id);
		}
		yield return null;
	}

	private static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
	{
		int width = renderTexture.width;
		int height = renderTexture.height;
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = renderTexture;
		try
		{
			Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, recalculateMipMaps: false);
			texture2D.Apply(updateMipmaps: false);
			return texture2D;
		}
		finally
		{
			RenderTexture.active = active;
		}
	}
}
