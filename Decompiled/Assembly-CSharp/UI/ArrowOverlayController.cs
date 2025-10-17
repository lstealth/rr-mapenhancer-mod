using System;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace UI;

public class ArrowOverlayController : MonoBehaviour
{
	private class ArrowInstance
	{
		public GameObject GameObject;

		public MaterialPropertyBlock MaterialPropertyBlock;

		public MeshRenderer[] MeshRenderers;

		public int Key;
	}

	private static ArrowOverlayController _shared;

	[SerializeField]
	private GameObject arrowPrefab;

	private readonly Dictionary<int, ArrowInstance> _activeArrows = new Dictionary<int, ArrowInstance>();

	private readonly Queue<ArrowInstance> _arrowPool = new Queue<ArrowInstance>();

	private int _nextKey = 1;

	public static ArrowOverlayController Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<ArrowOverlayController>();
			}
			return _shared;
		}
	}

	public int AddArrow(Vector3 position, Quaternion rotation, Color color, float scale, bool animated, bool dancing = false)
	{
		position = position.GameToWorld();
		if (!_arrowPool.TryDequeue(out var arrowInstance))
		{
			arrowInstance = CreateArrowInstance();
		}
		int num = _nextKey++;
		arrowInstance.Key = num;
		Transform transform = arrowInstance.GameObject.transform;
		transform.position = position;
		transform.rotation = rotation;
		arrowInstance.GameObject.SetActive(value: true);
		if (animated)
		{
			transform.localScale = Vector3.zero;
			LeanTween.scale(arrowInstance.GameObject, Vector3.one * scale, 0.25f).setEase(LeanTweenType.easeOutBounce).setOnComplete((Action)delegate
			{
				if (dancing)
				{
					StartDancingAnimation(arrowInstance.GameObject);
				}
			});
		}
		else
		{
			transform.localScale = Vector3.one * scale;
		}
		MaterialPropertyBlock materialPropertyBlock = arrowInstance.MaterialPropertyBlock;
		materialPropertyBlock.SetColor("_BaseColor", color);
		materialPropertyBlock.SetColor("_EmissionColor", color * 0.25f);
		MeshRenderer[] meshRenderers = arrowInstance.MeshRenderers;
		for (int num2 = 0; num2 < meshRenderers.Length; num2++)
		{
			meshRenderers[num2].SetPropertyBlock(materialPropertyBlock);
		}
		_activeArrows[num] = arrowInstance;
		return num;
	}

	private static void StartDancingAnimation(GameObject go)
	{
		LeanTween.rotateAroundLocal(go, Vector3.up, 360f, 2f).setLoopClamp();
		LeanTween.moveLocalY(go, go.transform.localPosition.y + 0.3f, 0.7f).setEase(LeanTweenType.easeInOutQuad).setLoopPingPong(-1);
	}

	public bool RemoveArrow(int key, bool animated)
	{
		if (!_activeArrows.TryGetValue(key, out var arrowInstance))
		{
			return false;
		}
		if (animated)
		{
			LeanTween.scale(arrowInstance.GameObject, Vector3.zero, 0.05f).setEase(LeanTweenType.easeInQuad).setOnComplete((Action<object>)delegate
			{
				RecycleArrow(key, arrowInstance);
			});
		}
		else
		{
			RecycleArrow(key, arrowInstance);
		}
		return true;
	}

	private void RecycleArrow(int key, ArrowInstance arrowInstance)
	{
		LeanTween.cancel(arrowInstance.GameObject);
		arrowInstance.GameObject.SetActive(value: false);
		_arrowPool.Enqueue(arrowInstance);
		_activeArrows.Remove(key);
	}

	public void RemoveArrows(IEnumerable<int> keys, bool animated)
	{
		foreach (int key in keys)
		{
			RemoveArrow(key, animated);
		}
	}

	public void ClearAll()
	{
		foreach (ArrowInstance value in _activeArrows.Values)
		{
			LeanTween.cancel(value.GameObject);
			value.GameObject.SetActive(value: false);
			_arrowPool.Enqueue(value);
		}
		_activeArrows.Clear();
	}

	private ArrowInstance CreateArrowInstance()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(arrowPrefab, base.transform);
		MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		MeshRenderer[] array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetPropertyBlock(materialPropertyBlock);
		}
		return new ArrowInstance
		{
			GameObject = gameObject,
			MaterialPropertyBlock = materialPropertyBlock,
			MeshRenderers = componentsInChildren
		};
	}
}
