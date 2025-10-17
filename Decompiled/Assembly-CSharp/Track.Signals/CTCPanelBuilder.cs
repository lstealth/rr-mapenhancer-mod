using System;
using System.Collections.Generic;
using UnityEngine;

namespace Track.Signals;

public class CTCPanelBuilder : MonoBehaviour
{
	[Serializable]
	public struct Element
	{
		public CTCInterlocking interlocking;
	}

	public int startNumber = 1;

	[Tooltip("Interlockings, left to right.")]
	public List<Element> elements = new List<Element>();

	public Transform slotParent;

	public Transform blockLampParent;

	public float slotSpacing;

	public GameObject columnPrefab;

	public GameObject lampPrefab;
}
