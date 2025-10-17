using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Helpers;
using TMPro;
using UnityEngine;

namespace UI;

public class CompassHUD : MonoBehaviour
{
	private class Record
	{
		public LocationIndicatorController.Descriptor Descriptor;

		public Callout Callout;

		public float LastX = -1f;

		public Record(LocationIndicatorController.Descriptor descriptor, Callout callout)
		{
			Descriptor = descriptor;
			Callout = callout;
		}
	}

	public TMP_Text label;

	public Callout calloutPrefab;

	private Camera _camera;

	private RectTransform _rectTransform;

	private RectTransform _labelRectTransform;

	private Vector2 _labelBasePosition;

	private float _lastAngle;

	private float _inactivityTimer;

	private bool _active;

	private readonly Dictionary<string, Record> _callouts = new Dictionary<string, Record>();

	private static readonly (float angle, string text)[] CompassDirections = new(float, string)[8]
	{
		(0f, "N"),
		(45f, "NE"),
		(90f, "E"),
		(135f, "SE"),
		(180f, "S"),
		(225f, "SW"),
		(270f, "W"),
		(315f, "NW")
	};

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
	}

	private void Start()
	{
		_labelRectTransform = label.GetComponent<RectTransform>();
		_labelBasePosition = _labelRectTransform.anchoredPosition;
	}

	private void OnEnable()
	{
		Messenger.Default.Register<UISettingDidChange>(this, delegate
		{
			UpdateActive();
		});
		UpdateActive();
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void UpdateActive()
	{
		_active = Preferences.ShowCompass;
		label.enabled = _active;
	}

	private void Update()
	{
		if (!_active || !MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			return;
		}
		Transform transform = _camera.transform;
		float cameraFieldOfView = Camera.VerticalToHorizontalFieldOfView(_camera.fieldOfView, _camera.aspect);
		float y = transform.eulerAngles.y;
		bool num = Mathf.Abs(y - _lastAngle) > 0.01f;
		_lastAngle = y;
		if (num)
		{
			_inactivityTimer = 0f;
		}
		else
		{
			_inactivityTimer += Time.deltaTime;
		}
		GetCompassTextAngle(y, out var text, out var deltaAngle);
		float f = NormalizedPositionForSignedDeltaAngle(deltaAngle);
		label.text = text;
		float num2 = Mathf.InverseLerp(1f, 0.85f, Mathf.Abs(f));
		float num3 = Mathf.InverseLerp(5f, 4f, _inactivityTimer);
		label.alpha = num3 * num2;
		float compassBoundsRadius = (float)Screen.width * 0.25f;
		_labelRectTransform.anchoredPosition = LabelPosition(f);
		foreach (Record value2 in _callouts.Values)
		{
			Callout callout = value2.Callout;
			LocationIndicatorController.Descriptor descriptor = value2.Descriptor;
			Vector3 vector = descriptor.GetTarget().GameToWorld() - transform.position;
			Vector3 forward = transform.forward;
			float a = Vector2.SignedAngle(new Vector2(vector.x, vector.z), new Vector2(forward.x, forward.z));
			float f2 = NormalizedPositionForSignedDeltaAngle(a);
			float value = Mathf.Sign(f2) * Mathf.Pow(f2, 4f);
			float z = Mathf.Lerp(90f, -90f, Mathf.InverseLerp(-1f, 1f, value));
			callout.directionalImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, z);
			callout.RectTransform.anchoredPosition = LabelPosition(f2);
		}
		Vector2 LabelPosition(float num4)
		{
			return _labelBasePosition + new Vector2(num4 * compassBoundsRadius, 0f);
		}
		float NormalizedPositionForSignedDeltaAngle(float num4)
		{
			return Mathf.Clamp(2f * num4 / cameraFieldOfView, -1f, 1f);
		}
	}

	private static void GetCompassTextAngle(float angle, out string text, out float deltaAngle)
	{
		text = null;
		deltaAngle = 360f;
		(float, string)[] compassDirections = CompassDirections;
		for (int i = 0; i < compassDirections.Length; i++)
		{
			(float, string) tuple = compassDirections[i];
			float item = tuple.Item1;
			string item2 = tuple.Item2;
			float num = Mathf.DeltaAngle(angle, item);
			if (!(Mathf.Abs(num) > Mathf.Abs(deltaAngle)))
			{
				deltaAngle = num;
				text = item2;
			}
		}
	}

	public void AddLocationIndicator(string token, LocationIndicatorController.Descriptor desc)
	{
		Callout callout = Object.Instantiate(calloutPrefab, _rectTransform, worldPositionStays: false);
		callout.SetTooltipInfo(new TooltipInfo("<font-weight=500>" + desc.Title + "</font-weight>", "<font-weight=400>" + desc.Subtitle + "</font-weight>"));
		_callouts[token] = new Record(desc, callout);
	}

	public void RemoveLocationIndicator(string token)
	{
		if (_callouts.TryGetValue(token, out var value))
		{
			Object.Destroy(value.Callout.gameObject);
			_callouts.Remove(token);
		}
	}
}
