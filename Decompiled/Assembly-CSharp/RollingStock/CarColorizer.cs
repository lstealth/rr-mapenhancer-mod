using UnityEngine;

namespace RollingStock;

public class CarColorizer : MonoBehaviour
{
	public Material[] targetMaterials;

	private static readonly int Color1 = Shader.PropertyToID("_BaseColor");

	private Color? _specifiedColor;

	private void OnEnable()
	{
		CarColorController componentInParent = GetComponentInParent<CarColorController>();
		if (componentInParent != null)
		{
			componentInParent.ColorSchemeChanged += ColorSchemeChanged;
			ColorSchemeChanged(componentInParent.Scheme);
		}
	}

	private void OnDisable()
	{
		CarColorController componentInParent = GetComponentInParent<CarColorController>();
		if (componentInParent != null)
		{
			componentInParent.ColorSchemeChanged -= ColorSchemeChanged;
		}
	}

	private void ColorSchemeChanged(CarColorScheme scheme)
	{
		_specifiedColor = scheme.Base;
		Replace();
	}

	private void Replace()
	{
		Color value = _specifiedColor ?? Color.gray;
		Material[] array = targetMaterials;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetColor(Color1, value);
		}
	}
}
