using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DebugGUIGraphAttribute : Attribute
{
	public float min { get; private set; }

	public float max { get; private set; }

	public Color color { get; private set; }

	public int group { get; private set; }

	public bool autoScale { get; private set; }

	public DebugGUIGraphAttribute(float r = 1f, float g = 1f, float b = 1f, float min = 0f, float max = 1f, int group = 0, bool autoScale = true)
	{
		color = new Color(r, g, b, 0.9f);
		this.min = min;
		this.max = max;
		this.group = group;
		this.autoScale = autoScale;
	}
}
