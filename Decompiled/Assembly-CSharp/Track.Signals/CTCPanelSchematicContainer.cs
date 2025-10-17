using System.Collections.Generic;
using UnityEngine;

namespace Track.Signals;

public class CTCPanelSchematicContainer : MonoBehaviour
{
	public Canvas canvas;

	public float yOffset;

	private IEnumerable<CTCPanelSchematicTile> GetTiles()
	{
		foreach (Transform item in canvas.transform)
		{
			CTCPanelSchematicTile component = item.GetComponent<CTCPanelSchematicTile>();
			if (component != null)
			{
				yield return component;
			}
		}
	}
}
