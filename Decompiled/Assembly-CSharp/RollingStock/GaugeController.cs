using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollingStock;

public class GaugeController : MonoBehaviour
{
	[Serializable]
	public struct Entry
	{
		public string id;

		public GaugeBehaviour gauge;
	}

	[SerializeField]
	private List<Entry> entries;

	public void SetValue(string id, float value)
	{
		foreach (Entry entry in entries)
		{
			if (!(entry.id != id))
			{
				entry.gauge.Value = value;
				return;
			}
		}
		throw new ArgumentException("Gauge with id " + id + " not found");
	}

	public GaugeBehaviour GaugeForId(string id)
	{
		foreach (Entry entry in entries)
		{
			if (!(entry.id != id))
			{
				return entry.gauge;
			}
		}
		return null;
	}

	public void RenameId(string fromId, string toId)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			Entry value = entries[i];
			if (!(value.id != fromId))
			{
				value.id = toId;
				entries[i] = value;
				break;
			}
		}
	}
}
