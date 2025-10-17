using System;
using System.Collections.Generic;
using Helpers;
using KeyValue.Runtime;
using Model;
using UnityEngine;

namespace RollingStock;

public class CarColorController : MonoBehaviour
{
	private IDisposable _observer;

	public List<Color> palette = new List<Color>();

	public CarColorScheme Scheme { get; private set; }

	private static Color DecalColorLight => new Color(0.8f, 0.8f, 0.8f, 1f);

	private static Color DecalColorDark => new Color(0.1f, 0.1f, 0.1f, 1f);

	public event Action<CarColorScheme> ColorSchemeChanged;

	private void OnEnable()
	{
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		_observer = componentInParent.Observe("_colorScheme", UpdateForColorScheme);
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
	}

	private void UpdateForColorScheme(Value value)
	{
		Scheme = CarColorScheme.From(value);
		if (!Scheme.Base.HasValue && palette.Count > 0)
		{
			Scheme = new CarColorScheme(ColorFromPalette(), Scheme.DecalHex);
		}
		if (Scheme.Base.HasValue && !Scheme.Decal.HasValue)
		{
			Scheme = new CarColorScheme(Scheme.BaseHex, (Scheme.Base.Value.IsDark() ? DecalColorLight : DecalColorDark).HexString());
		}
		OnColorSchemeChanged(Scheme);
	}

	private string ColorFromPalette()
	{
		Car componentInParent = GetComponentInParent<Car>();
		return ColorFromPalette(palette, componentInParent).HexString();
	}

	private static Color ColorFromPalette(List<Color> palette, Car car)
	{
		if (car == null)
		{
			throw new ArgumentException("null car", "car");
		}
		if (car.ghost || palette.Count == 0)
		{
			return Color.grey;
		}
		int num = car.id.GetHashCode();
		if (num < 0)
		{
			num *= -1;
		}
		int index = num % palette.Count;
		return palette[index];
	}

	private void OnColorSchemeChanged(CarColorScheme scheme)
	{
		this.ColorSchemeChanged?.Invoke(scheme);
	}
}
