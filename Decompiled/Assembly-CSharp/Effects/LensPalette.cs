using System;
using UnityEngine;

namespace Effects;

[CreateAssetMenu(fileName = "Lens Palette", menuName = "Train Game/Effects/Lens Palette", order = 0)]
public class LensPalette : ScriptableObject
{
	[Serializable]
	public struct LensColorItem
	{
		public Color unlit;

		public Color lit;

		public float emissiveIntensity;
	}

	public LensColorItem red;

	public LensColorItem green;

	public LensColorItem yellow;

	public LensColorItem white;

	public Material emissiveMaterial;
}
