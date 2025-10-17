using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using KeyValue.Runtime;
using Network;
using UnityEngine;

namespace Track.Signals.Panel;

public class CTCPanelMarkerManager : MonoBehaviour
{
	public List<CTCPanelSchematicFace> faces;

	private Camera _camera;

	private readonly Dictionary<string, CTCPanelMarker> _markers = new Dictionary<string, CTCPanelMarker>();

	private readonly Dictionary<string, IDisposable> _markerObservers = new Dictionary<string, IDisposable>();

	private KeyValueObject _keyValueObject;

	private IDisposable _keyObserver;

	[SerializeField]
	private CTCPanelMarker markerPrefab;

	private const string MarkerKeyPrefix = "marker-";

	private void OnEnable()
	{
		_keyValueObject = GetComponentInParent<KeyValueObject>();
		foreach (string item in _keyValueObject.Keys.Where((string k) => k.StartsWith("marker-")).Select(KeyToMarkerId))
		{
			AddUpdateMarkerFromObject(item);
		}
		_keyObserver = _keyValueObject.ObserveKeyChanges(delegate(string key, KeyChange change)
		{
			if (key.StartsWith("marker-"))
			{
				string id = KeyToMarkerId(key);
				if (!string.IsNullOrEmpty(id))
				{
					switch (change)
					{
					case KeyChange.Add:
						_markerObservers[id] = _keyValueObject.Observe(key, delegate(Value value2)
						{
							if (!value2.IsNull)
							{
								AddUpdateMarkerFromObject(id);
							}
						});
						break;
					case KeyChange.Remove:
					{
						HandleMarkerRemoved(id);
						if (_markerObservers.TryGetValue(id, out var value))
						{
							value.Dispose();
						}
						_markerObservers.Remove(id);
						break;
					}
					default:
						throw new ArgumentOutOfRangeException("change", change, null);
					}
				}
			}
		});
	}

	private void OnDisable()
	{
		_keyObserver.Dispose();
		_keyObserver = null;
		foreach (IDisposable value in _markerObservers.Values)
		{
			value.Dispose();
		}
		_markerObservers.Clear();
	}

	private static string KeyToMarkerId(string key)
	{
		return key.Remove(0, "marker-".Length);
	}

	private static string MarkerIdToKey(string id)
	{
		return "marker-" + id;
	}

	private void AddUpdateMarkerFromObject(string id)
	{
		Value value = _keyValueObject[MarkerIdToKey(id)];
		if (!_markers.TryGetValue(id, out var value2))
		{
			value2 = UnityEngine.Object.Instantiate(markerPrefab, faces[0].transform);
			value2.manager = this;
			_markers[id] = value2;
		}
		string stringValue = value["text"].StringValue;
		Vector2 logical = new Vector2(value["x"].FloatValue, value["y"].FloatValue);
		value2.Configure(id, stringValue);
		value2.Position(logical);
	}

	private void HandleMarkerRemoved(string id)
	{
		if (_markers.TryGetValue(id, out var value))
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
	}

	public void AddMarkerLeft()
	{
		AddMarker("???>", new Vector2(0.1f, 0.1f));
	}

	public void AddMarkerRight()
	{
		AddMarker("<???", new Vector2(2.9f, 0.1f));
	}

	private void AddMarker(string text, Vector2 position)
	{
		string id = NetworkTime.systemTick.ToString();
		_keyValueObject[MarkerIdToKey(id)] = Value.Dictionary(new Dictionary<string, Value>
		{
			{
				"text",
				Value.String(text)
			},
			{
				"x",
				Value.Float(position.x)
			},
			{
				"y",
				Value.Float(position.y)
			}
		});
	}

	public void RemoveMarker(string id)
	{
		_keyValueObject[MarkerIdToKey(id)] = Value.Null();
	}

	public void UpdateMarker(string id, string text)
	{
		UpdateMarkerDictionary(id, delegate(Dictionary<string, Value> dict)
		{
			dict["text"] = Value.String(text);
		});
	}

	public void SendPosition(string id, Vector2 position)
	{
		UpdateMarkerDictionary(id, delegate(Dictionary<string, Value> dict)
		{
			dict["x"] = Value.Float(position.x);
			dict["y"] = Value.Float(position.y);
		});
	}

	private void UpdateMarkerDictionary(string id, Action<Dictionary<string, Value>> action)
	{
		string key = MarkerIdToKey(id);
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>(_keyValueObject[key].DictionaryValue);
		action(dictionary);
		_keyValueObject[key] = Value.Dictionary(dictionary);
	}

	public CTCPanelSchematicFace FaceForMousePosition(Vector3 mousePosition, out Vector2 localPointOnFaceCanvas)
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}
		localPointOnFaceCanvas = Vector3.zero;
		if (!Physics.Raycast(_camera.ScreenPointToRay(mousePosition), out var hitInfo, 2f, 1 << Layers.Default))
		{
			return null;
		}
		CTCPanelSchematicFace component = hitInfo.transform.GetComponent<CTCPanelSchematicFace>();
		if (component != null)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(component.canvas.GetComponent<RectTransform>(), mousePosition, _camera, out localPointOnFaceCanvas);
		}
		return component;
	}

	public void RemoveAllMarkers()
	{
		foreach (string item in _markers.Keys.ToList())
		{
			RemoveMarker(item);
		}
	}
}
