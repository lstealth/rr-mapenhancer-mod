using System;
using System.Collections;
using Helpers.Animation;
using UnityEngine;

namespace Track;

public class SemaphoreHeadController : MonoBehaviour
{
	public enum Aspect
	{
		Red,
		Yellow,
		Green
	}

	public AnimationClip semaphoreArmAnimationClip;

	public Animator animator;

	public MeshRenderer lampMeshRenderer;

	public int lampMaterialIndex;

	private IDisposable _observer;

	private PlayableHandle _playable;

	private int _clipPlayablePort;

	public float raiseSpeed = 0.25f;

	public float fallSpeed = 0.5f;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private Aspect? _pendingAspect;

	private Color _lastColor = Color.black;

	private float _lastParam = float.NegativeInfinity;

	private Coroutine _setAspectCoroutine;

	private void Awake()
	{
		_playable = animator.PlayableGraphAdapter().AddPlayable(semaphoreArmAnimationClip);
	}

	private void OnDestroy()
	{
		_playable?.Dispose();
		_playable = null;
	}

	private void OnEnable()
	{
		_playable.Play();
		if (_pendingAspect.HasValue)
		{
			SetAspect(_pendingAspect.Value);
			_pendingAspect = null;
		}
	}

	private void OnDisable()
	{
		_observer?.Dispose();
	}

	public void SetAspect(Aspect aspect)
	{
		if (_playable == null)
		{
			_pendingAspect = aspect;
			return;
		}
		if (_setAspectCoroutine != null)
		{
			StopCoroutine(_setAspectCoroutine);
		}
		_setAspectCoroutine = StartCoroutine(SetAspectCoroutine(aspect));
	}

	private IEnumerator SetAspectCoroutine(Aspect aspect)
	{
		if (_playable == null)
		{
			Debug.Log("Playable not initialized, aspect will not be set.", this);
			yield break;
		}
		bool keepRunning = true;
		while (keepRunning)
		{
			float time = _playable.Time;
			float num = TimeForAspect(aspect) * semaphoreArmAnimationClip.length;
			if (Mathf.Abs(time - num) < 0.01f)
			{
				_playable.Speed = 0f;
				keepRunning = false;
			}
			else if (time < num)
			{
				_playable.Speed = raiseSpeed;
			}
			else if (time > num)
			{
				_playable.Speed = 0f - fallSpeed;
			}
			ParameterForAnimationTime(_playable.Time / semaphoreArmAnimationClip.length, out var param, out var color);
			if (Mathf.Abs(param - _lastParam) > 0.001f || !color.Equals(_lastColor))
			{
				Material obj = lampMeshRenderer.materials[lampMaterialIndex];
				obj.color = color;
				obj.SetColor(value: Color.Lerp(Color.black, color * 9f, param), nameID: EmissionColor);
				_lastColor = color;
				_lastParam = param;
			}
			yield return null;
		}
	}

	private static void ParameterForAnimationTime(float t, out float param, out Color color)
	{
		float num = 0.125f;
		if (t < num * 1f)
		{
			color = Color.red;
			param = Mathf.InverseLerp(num, 0f, t);
		}
		else if (t < num * 3f)
		{
			color = Color.yellow;
			param = Mathf.InverseLerp(num, 0f, Mathf.Abs(t - num * 2f));
		}
		else
		{
			color = Color.green;
			param = Mathf.InverseLerp(num, 0f, Mathf.Abs(t - num * 4f));
		}
		param *= param;
	}

	private static float TimeForAspect(Aspect aspect)
	{
		return aspect switch
		{
			Aspect.Red => 0f, 
			Aspect.Yellow => 0.25f, 
			Aspect.Green => 0.5f, 
			_ => throw new ArgumentOutOfRangeException("aspect", aspect, null), 
		};
	}
}
