using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Map.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using UnityEngine;
using UnityEngine.VFX;

public class CarPerformanceTestController : MonoBehaviour
{
	private int frames;

	private float duration;

	private float bestDuration = float.MaxValue;

	private float worstDuration;

	private StringBuilder _stringBuilder = new StringBuilder();

	private TrainController trainController;

	private IPrefabStore prefabStore;

	private void OnEnable()
	{
		MapManager.Instance.gameObject.SetActive(value: false);
		trainController = TrainController.Shared;
		prefabStore = trainController.PrefabStore;
		StartCoroutine(RunTestAll());
	}

	private void Update()
	{
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		frames++;
		duration += unscaledDeltaTime;
		if (unscaledDeltaTime < bestDuration)
		{
			bestDuration = unscaledDeltaTime;
		}
		if (unscaledDeltaTime > worstDuration)
		{
			worstDuration = unscaledDeltaTime;
		}
	}

	private void ClearState()
	{
		frames = 0;
		duration = 0f;
		bestDuration = float.MaxValue;
		worstDuration = 0f;
	}

	private IEnumerator RunTestAll()
	{
		_stringBuilder.Clear();
		yield return new WaitForSeconds(3f);
		CameraSelector.shared.ZoomToPoint(new Vector3(-100f, 0f, -100f));
		yield return new WaitForSeconds(1f);
		ClearState();
		yield return new WaitForSeconds(3f);
		float baselineMillis = AverageMillisecondsPerFrame();
		string[] passAndCaboose = new string[4] { "ne", "pb", "cb", "mw" };
		IEnumerable<string> enumerable = from d in prefabStore.AllDefinitionInfosOfType<CarDefinition>()
			where passAndCaboose.Any((string prefix) => d.Identifier.StartsWith(prefix))
			select d.Identifier;
		foreach (string item in enumerable)
		{
			yield return RunTest(item, baselineMillis);
			yield return new WaitForSeconds(0.5f);
		}
		Debug.Log("Report:\n" + _stringBuilder);
	}

	private IEnumerator RunTest(string identifier, float baselineMillis)
	{
		TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.CarDefinitionInfoForIdentifier(identifier);
		CarDescriptor descriptor = new CarDescriptor(typedContainerItem);
		HashSet<IDisposable> modelLoads = new HashSet<IDisposable>();
		int dim = 5;
		Vector3 p0 = new Vector3(-100f, 0f, -100f);
		float length = typedContainerItem.Definition.Length;
		List<Car> cars = new List<Car>();
		float xInc = length;
		float yInc = 10f;
		for (int i = 0; i < dim * dim; i++)
		{
			int num = i / dim;
			int num2 = i % dim;
			Car car = trainController.CreateCarRaw(descriptor, null, ghost: false, base.transform);
			modelLoads.Add(car.ModelLoadRetain("demo"));
			car.SetVisible(visible: true);
			Vector3 position = p0 + new Vector3((float)num * xInc, 0f, (float)num2 * yInc);
			car._mover.Move(base.transform.TransformPoint(position), Quaternion.Euler(0f, 75f, 0f), immediate: true);
			cars.Add(car);
		}
		yield return new WaitForSeconds(1f);
		VisualEffect[] componentsInChildren = base.transform.GetComponentsInChildren<VisualEffect>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			componentsInChildren[j].Stop();
		}
		List<string> fields = new List<string> { identifier };
		CollectStats(cars[0].BodyTransform, fields);
		Vector3 position2 = p0 + new Vector3((float)(dim - 1) * xInc, 0f, (float)(dim - 1) * yInc) * 0.5f;
		CameraSelector.shared.ZoomToPoint(position2);
		yield return new WaitForSeconds(3f);
		ClearState();
		yield return new WaitForSeconds(4f);
		fields.Add((AverageMillisecondsPerFrame() - baselineMillis).ToString("F1"));
		_stringBuilder.AppendLine(string.Join(",", fields));
		foreach (IDisposable item in modelLoads)
		{
			item.Dispose();
		}
		for (int num3 = base.transform.childCount - 1; num3 >= 0; num3--)
		{
			UnityEngine.Object.Destroy(base.transform.GetChild(num3).gameObject);
		}
	}

	private float AverageMillisecondsPerFrame()
	{
		return 1000f * duration / (float)frames;
	}

	private void CollectStats(Transform bodyTransform, List<string> fields)
	{
		if (!(bodyTransform == null))
		{
			List<Mesh> list = (from mf in bodyTransform.GetComponentsInChildren<MeshFilter>()
				select mf.sharedMesh into m
				where m != null
				select m).Distinct().ToList();
			fields.Add(list.Count.ToString());
		}
	}
}
