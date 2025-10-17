using System;
using UnityEngine;

namespace Helpers.Culling;

[DefaultExecutionOrder(-1000)]
[ExecuteInEditMode]
public class CullingManagerInitializer : MonoBehaviour
{
	private static CullingManagerInitializer _shared;

	private static readonly float[] HoseDistanceBands = new float[1] { 25f };

	private static readonly float[] BridgeDistanceBands = new float[1] { 1000f };

	private static readonly float[] SceneryDistanceBands = new float[3] { 100f, 1000f, 1500f };

	private static readonly float[] FlareDistanceBands = new float[2] { 25f, 1000f };

	[NonSerialized]
	public CullingManager Hose;

	[NonSerialized]
	public CullingManager Bridge;

	[NonSerialized]
	public CullingManager CTC;

	[NonSerialized]
	public CullingManager Signal;

	[NonSerialized]
	public CullingManager Scenery;

	[NonSerialized]
	public CullingManager Flare;

	public static CullingManagerInitializer Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<CullingManagerInitializer>();
			}
			return _shared;
		}
	}

	private void OnEnable()
	{
		base.transform.DestroyAllChildren();
		Hose = Create("Hose", HoseDistanceBands);
		Bridge = Create("Bridge", BridgeDistanceBands);
		CTC = Create("CTC", HoseDistanceBands);
		Signal = Create("Signal", BridgeDistanceBands);
		Scenery = Create("Scenery", SceneryDistanceBands);
		Flare = Create("Flare", FlareDistanceBands);
		CullingManager Create(string managerName, float[] distances)
		{
			GameObject obj = new GameObject(managerName);
			obj.hideFlags = HideFlags.DontSave;
			obj.transform.SetParent(base.transform);
			CullingManager cullingManager = obj.AddComponent<CullingManager>();
			cullingManager.Configure(managerName, distances);
			return cullingManager;
		}
	}

	private void OnDisable()
	{
		base.transform.DestroyAllChildren();
		Hose = null;
		Bridge = null;
		CTC = null;
		Signal = null;
		Scenery = null;
		Flare = null;
	}
}
