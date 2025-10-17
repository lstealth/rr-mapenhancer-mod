using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Serilog;
using Track;
using UnityEngine;

namespace UI;

public class SegmentIndicatorController : MonoBehaviour
{
	private class IndicatorRecord
	{
		public HashSet<TrackSpan> spans;

		public List<GameObject> indicators;
	}

	public Material highlightMaterial;

	private readonly Dictionary<string, IndicatorRecord> _records = new Dictionary<string, IndicatorRecord>();

	public static SegmentIndicatorController Shared { get; private set; }

	private void Awake()
	{
		Shared = this;
	}

	public string Add(IEnumerable<string> spanIds)
	{
		List<TrackSpan> list = (from spanId in spanIds
			select TrainController.Shared.graph.SpanForId(spanId) into span
			where span != null
			select span).ToList();
		if (list.Count != spanIds.Count())
		{
			Log.Warning("One or more segment ids could not be resolved: {spanIds}", spanIds);
		}
		string text = Guid.NewGuid().ToString();
		Matrix4x4 matrix = Matrix4x4.Scale(new Vector3(1.2f, 1.2f, 1.2f));
		List<GameObject> indicators = list.Select(delegate(TrackSpan span)
		{
			GameObject obj = new GameObject("SpanIndicator");
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			obj.transform.localPosition = Vector3.up * 0.16f;
			obj.hideFlags = HideFlags.DontSave;
			Mesh mesh = span.BuildMesh(matrix);
			obj.AddComponent<MeshFilter>().mesh = mesh;
			obj.AddComponent<MeshRenderer>().material = highlightMaterial;
			obj.AddComponent<MeshDestroyer>().mesh = mesh;
			return obj;
		}).ToList();
		_records[text] = new IndicatorRecord
		{
			spans = new HashSet<TrackSpan>(list),
			indicators = indicators
		};
		return text;
	}

	public void Remove(string token)
	{
		if (_records.ContainsKey(token))
		{
			foreach (GameObject indicator in _records[token].indicators)
			{
				UnityEngine.Object.Destroy(indicator);
			}
		}
		_records.Remove(token);
	}
}
