using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using Model;
using Model.Definition;
using Model.Ops;
using Serilog;
using Track;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tags;

public class TagController : MonoBehaviour
{
	private static TagController _shared;

	[SerializeField]
	private TagCallout tagCalloutPrefab;

	private Coroutine _coroutine;

	private readonly Dictionary<string, TagCallout> _tags = new Dictionary<string, TagCallout>();

	private readonly Queue<TagCallout> _pool = new Queue<TagCallout>();

	public static TagController Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<TagController>();
			}
			return _shared;
		}
	}

	public bool TagsVisible => _coroutine != null;

	private TrainController TrainController => TrainController.Shared;

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	private void Update()
	{
		if (!GameInput.shared.InputToggleTags)
		{
			return;
		}
		if (_coroutine == null)
		{
			_coroutine = StartCoroutine(TickLoop());
			SendTagVisibilityDidChange();
			return;
		}
		foreach (string key in _tags.Keys)
		{
			Car car = TrainController.CarForId(key);
			RecycleTagForCar(car);
		}
		_tags.Clear();
		StopCoroutine(_coroutine);
		_coroutine = null;
		SendTagVisibilityDidChange();
	}

	private void SendTagVisibilityDidChange()
	{
		bool tagsVisible = TagsVisible;
		Messenger.Default.Send(new TagVisibilityDidChange
		{
			IsVisible = tagsVisible
		});
	}

	private TagCallout GetOrCreateTagForCar(Car car)
	{
		if (_pool.TryDequeue(out var result))
		{
			result.transform.SetParent(car.transform);
		}
		else
		{
			result = UnityEngine.Object.Instantiate(tagCalloutPrefab, car.transform);
		}
		result.callout.Title = car.DisplayName.NoParse();
		result.SetPosition(car.InitialTagCalloutPosition.GameToWorld(), immediate: true);
		car.TagCallout = result;
		return result;
	}

	private void RecycleTagForCar(Car car)
	{
		if (!(car == null))
		{
			TagCallout tagCallout = car.TagCallout;
			if (!(tagCallout == null))
			{
				tagCallout.locationIndicatorHoverArea.spanIds.Clear();
				tagCallout.locationIndicatorHoverArea.descriptors.Clear();
				tagCallout.transform.SetParent(base.transform);
				tagCallout.gameObject.SetActive(value: false);
				_pool.Enqueue(tagCallout);
				car.TagCallout = null;
			}
		}
	}

	private IEnumerator TickLoop()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		bool updateAllImmediately = true;
		while (true)
		{
			yield return Tick(updateAllImmediately);
			updateAllImmediately = false;
			yield return wait;
		}
	}

	private IEnumerator Tick(bool updateAllImmediately)
	{
		TrainController trainController = TrainController;
		Camera main = Camera.main;
		if (main == null || trainController == null)
		{
			yield break;
		}
		Vector3 vector = main.transform.GamePosition();
		try
		{
			Vector3 point = vector;
			HashSet<string> hashSet = trainController.CarIdsInRadius(point, 250f).ToHashSet();
			HashSet<string> hashSet2 = _tags.Keys.ToHashSet();
			IEnumerable<string> enumerable = hashSet.Except(hashSet2);
			IEnumerable<string> enumerable2 = hashSet2.Except(hashSet);
			foreach (string item in enumerable)
			{
				Car car = trainController.CarForId(item);
				if (car.Archetype != CarArchetype.Tender)
				{
					TagCallout orCreateTagForCar = GetOrCreateTagForCar(car);
					orCreateTagForCar.gameObject.SetActive(value: false);
					_tags[item] = orCreateTagForCar;
				}
			}
			foreach (string item2 in enumerable2)
			{
				Car car2 = trainController.CarForId(item2);
				if (car2 != null)
				{
					RecycleTagForCar(car2);
				}
				_tags.Remove(item2);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception from Tick");
		}
		yield return UpdateTags(vector, updateAllImmediately);
	}

	private IEnumerator UpdateTags(Vector3 cameraGamePosition, bool updateAllImmediately)
	{
		TrainController trainController = TrainController;
		OpsController opsController = OpsController.Shared;
		Graph graph = trainController.graph;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		List<(Car, TagCallout, float)> list = new List<(Car, TagCallout, float)>();
		foreach (var (carId, tagCallout2) in _tags)
		{
			if (!trainController.TryGetCarForId(carId, out var car))
			{
				tagCallout2.gameObject.SetActive(value: false);
				continue;
			}
			float item = Vector3.Distance(car.GetCenterPosition(graph), cameraGamePosition);
			list.Add((car, tagCallout2, item));
		}
		list.Sort(((Car Car, TagCallout Tag, float Distance) a, (Car Car, TagCallout Tag, float Distance) b) => a.Distance.CompareTo(b.Distance));
		foreach (var (car2, tagCallout3, _) in list)
		{
			try
			{
				UpdateTag(car2, tagCallout3, opsController);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception updating tag for car {car}", car2);
			}
			finally
			{
			}
			float num = Time.realtimeSinceStartup - realtimeSinceStartup;
			if (!updateAllImmediately && num > 0.0005f)
			{
				yield return null;
				realtimeSinceStartup = Time.realtimeSinceStartup;
			}
		}
	}

	private void UpdateTag(Car car, TagCallout tagCallout, OpsController opsController)
	{
		if (car == null)
		{
			return;
		}
		tagCallout.gameObject.SetActive(value: true);
		if (opsController != null && opsController.TryGetDestinationInfo(car, out var destinationName, out var isAtDestination, out var destinationPosition, out var destination))
		{
			tagCallout.callout.Text = destinationName;
			tagCallout.callout.Layout();
			tagCallout.locationIndicatorHoverArea.spanIds.Clear();
			TrackSpan[] spans = destination.Spans;
			foreach (TrackSpan trackSpan in spans)
			{
				tagCallout.locationIndicatorHoverArea.spanIds.Add(trackSpan.id);
			}
			tagCallout.locationIndicatorHoverArea.descriptors.Clear();
			tagCallout.locationIndicatorHoverArea.descriptors.Add(new LocationIndicatorController.Descriptor(destinationPosition, destinationName, null));
			Area area = opsController.AreaForCarPosition(destination);
			Color color = Color.gray;
			if (area != null)
			{
				color = area.tagColor;
			}
			if (isAtDestination)
			{
				color *= 0.5f;
			}
			ApplyImageColor(tagCallout, color);
		}
		else
		{
			if (car.TryGetTrainName(out var trainName))
			{
				tagCallout.callout.Text = trainName;
			}
			else
			{
				tagCallout.callout.Text = null;
			}
			tagCallout.callout.Layout();
			ApplyImageColor(tagCallout, Color.gray);
		}
	}

	private void ApplyImageColor(TagCallout tagCallout, Color color)
	{
		Image[] colorImages = tagCallout.colorImages;
		for (int i = 0; i < colorImages.Length; i++)
		{
			colorImages[i].color = color;
		}
	}
}
