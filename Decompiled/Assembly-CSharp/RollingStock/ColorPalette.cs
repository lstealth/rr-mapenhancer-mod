using System.Collections.Generic;
using UnityEngine;

namespace RollingStock;

[CreateAssetMenu(fileName = "Palette", menuName = "Train Game/Color Palette", order = 0)]
public class ColorPalette : ScriptableObject
{
	public List<Color> colors = new List<Color>();
}
