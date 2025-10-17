using System;
using System.Collections;
using Enviro;
using Game;
using UnityEngine;

namespace Effects;

[RequireComponent(typeof(EnviroManager))]
public class EnviroSynchronizer : MonoBehaviour
{
	[Range(0f, 200f)]
	public float additionalFogOffset;

	private EnviroManager _enviro;

	private Coroutine _coroutine;

	private Camera _camera;

	private Vector3 _lastCameraPosition;

	private float _lastCameraTime;

	[SerializeField]
	private AnimationCurve cameraVelocityToReflectionPositionThreshold = AnimationCurve.Linear(0f, 5f, 100f, 500f);

	private void Awake()
	{
		_enviro = GetComponent<EnviroManager>();
	}

	private void OnEnable()
	{
		_coroutine = StartCoroutine(UpdateCoroutine());
	}

	private void OnDisable()
	{
		StopCoroutine(_coroutine);
	}

	private IEnumerator UpdateCoroutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.2f);
		yield return null;
		_enviro.Sky.SetupSkybox();
		DateTime startOfGame = TimeWeather.StartDateTime;
		DateTime lastDateTime = startOfGame;
		while (true)
		{
			GameDateTime now = TimeWeather.Now;
			DateTime dateTime = startOfGame.AddHours((float)(now.Day * 24) + now.Hours);
			_enviro.Time.SetDateTime(dateTime.Second, dateTime.Minute, dateTime.Hour, dateTime.Day, dateTime.Month, dateTime.Year);
			if ((lastDateTime - dateTime).TotalMinutes > 10.0)
			{
				_enviro.Reflections.RenderGlobalReflectionProbe(forced: true);
			}
			lastDateTime = dateTime;
			UpdateReflectionThreshold();
			UpdateGlobalFogHeight();
			yield return wait;
		}
	}

	private void UpdateReflectionThreshold()
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}
		if (!(_camera == null))
		{
			Vector3 position = _camera.transform.position;
			float unscaledTime = Time.unscaledTime;
			float num = unscaledTime - _lastCameraTime;
			if (num != 0f)
			{
				float time = Vector3.Distance(_lastCameraPosition, position) / num;
				_enviro.Reflections.Settings.globalReflectionsPositionTreshold = cameraVelocityToReflectionPositionThreshold.Evaluate(time);
				_lastCameraPosition = position;
				_lastCameraTime = unscaledTime;
			}
		}
	}

	private void UpdateGlobalFogHeight()
	{
		float globalFogHeight = CameraSelector.shared.CurrentCameraGroundPosition.y + additionalFogOffset;
		EnviroManager.instance.Fog.Settings.globalFogHeight = globalFogHeight;
	}
}
