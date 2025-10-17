using UnityEngine;

namespace Helpers;

public static class Layers
{
	public static int Default => LayerMask.NameToLayer("Default");

	public static int Terrain => LayerMask.NameToLayer("Terrain");

	public static int Water => LayerMask.NameToLayer("Water");

	public static int Track => LayerMask.NameToLayer("Track");

	public static int Character => LayerMask.NameToLayer("Character");

	public static int Clickable => LayerMask.NameToLayer("Clickable");

	public static int Map => LayerMask.NameToLayer("Map");

	public static int UI => LayerMask.NameToLayer("UI");

	public static int Ladder => LayerMask.NameToLayer("Ladder");
}
