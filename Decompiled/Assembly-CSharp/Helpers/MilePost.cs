using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;

namespace Helpers;

[ExecuteInEditMode]
[RequireComponent(typeof(TextSynchronizer))]
[RequireComponent(typeof(TrackMarker))]
public class MilePost : MonoBehaviour
{
	public string prefix = "T";

	public bool origin;

	public float mileage;

	private TextSynchronizer _textSynchronizer;

	private static readonly Dictionary<string, MilePost> _originMilePosts = new Dictionary<string, MilePost>();

	private TrackMarker TrackMarker { get; set; }

	private MilePost Origin
	{
		get
		{
			if (!_originMilePosts.TryGetValue(prefix, out var value))
			{
				_originMilePosts.Add(prefix, value = FindOriginMilePost());
			}
			if (value == null)
			{
				Debug.LogWarning("Missing origin MilePost for prefix " + prefix + ".");
			}
			return value;
		}
	}

	private void Awake()
	{
		_textSynchronizer = GetComponent<TextSynchronizer>();
		TrackMarker = GetComponent<TrackMarker>();
	}

	private void OnEnable()
	{
		TrackMarker.OnLocationChanged += TrackMarkerOnOnLocationChanged;
	}

	private void OnDisable()
	{
		TrackMarker.OnLocationChanged -= TrackMarkerOnOnLocationChanged;
	}

	private void TrackMarkerOnOnLocationChanged()
	{
		UpdateMileage();
	}

	[ContextMenu("Update Mileage")]
	public void UpdateMileage()
	{
	}

	private MilePost FindOriginMilePost()
	{
		return Object.FindObjectsOfType<MilePost>().FirstOrDefault((MilePost mp) => mp.origin && mp.prefix == prefix);
	}
}
