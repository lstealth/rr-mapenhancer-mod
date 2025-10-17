using System;
using Helpers;
using Model;
using UnityEngine;

namespace UI;

public class LocationIndicatorController : MonoBehaviour
{
	public struct Descriptor
	{
		public readonly string Title;

		public string Subtitle;

		private string _targetCarId;

		private Vector3 _targetGamePosition;

		public Descriptor(string carId, string title, string subtitle)
		{
			_targetCarId = carId;
			_targetGamePosition = Vector3.zero;
			Title = title;
			Subtitle = subtitle;
		}

		public Descriptor(Vector3 gamePosition, string title, string subtitle)
		{
			_targetCarId = null;
			_targetGamePosition = gamePosition;
			Title = title;
			Subtitle = subtitle;
		}

		public Vector3 GetTarget()
		{
			if (string.IsNullOrEmpty(_targetCarId))
			{
				return _targetGamePosition;
			}
			TrainController shared = TrainController.Shared;
			Car car = shared.CarForId(_targetCarId);
			if (car == null)
			{
				return Vector3.zero;
			}
			return car.GetCenterPosition(shared.graph);
		}
	}

	private static LocationIndicatorController _instance;

	public BillboardLocationIndicator indicatorPrefab;

	public Callout calloutPrefab;

	public Canvas canvas;

	private CompassHUD _compass;

	private Camera _camera;

	private Coroutine _contentUpdateCoroutine;

	public static LocationIndicatorController Shared => _instance;

	private CompassHUD Compass
	{
		get
		{
			if (_compass == null)
			{
				_compass = UnityEngine.Object.FindObjectOfType<CompassHUD>();
			}
			return _compass;
		}
	}

	public string Add(Descriptor desc)
	{
		string text = Guid.NewGuid().ToString();
		if (MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			desc.Subtitle = DirectionalText(_camera.transform, desc.GetTarget());
		}
		Compass.AddLocationIndicator(text, desc);
		return text;
	}

	public void Remove(string token)
	{
		Compass.RemoveLocationIndicator(token);
	}

	private void Awake()
	{
		_instance = this;
	}

	private static string DirectionalText(Transform from, Vector3 to)
	{
		Vector3 a = WorldTransformer.GameToWorld(to);
		Vector3 position = from.position;
		string text = Units.DistanceText(Vector3.Distance(a, position));
		string text2 = CardinalDirectionFromAngle(Mathf.Atan2(a.x - position.x, a.z - position.z) * 57.29578f);
		return text + " " + text2;
	}

	public static string CardinalDirectionFromAngle(float angle)
	{
		if (angle < 0f)
		{
			angle += 360f;
		}
		int num = (int)angle / 22;
		num = (num + 1) % 16;
		return (num / 2) switch
		{
			0 => "N", 
			1 => "NE", 
			2 => "E", 
			3 => "SE", 
			4 => "S", 
			5 => "SW", 
			6 => "W", 
			7 => "NW", 
			_ => $"{angle:F0}", 
		};
	}
}
