using System;
using System.Collections.Generic;
using Game.Messages;
using KeyValue.Runtime;
using Model.Definition.Components;
using RollingStock.ContinuousControls;
using UI.CarEditor;
using UnityEngine;

namespace RollingStock;

public class BrakeStandController : MonoBehaviour
{
	public enum BrakeStyle
	{
		TwentySix,
		Six
	}

	public BrakeStyle style;

	public RadialAnimatedControl trainBrakeControl;

	public RadialAnimatedControl locomotiveBrakeControl;

	public RadialAnimatedControl cutoutControl;

	private IDisposable _observer;

	private readonly HashSet<GameObject> _deactivated = new HashSet<GameObject>();

	private static readonly Dictionary<BrakeStyle, string> BrakeStyleStringMapping = new Dictionary<BrakeStyle, string>
	{
		{
			BrakeStyle.TwentySix,
			"26L"
		},
		{
			BrakeStyle.Six,
			"6ET"
		}
	};

	private static BrakeStyle DefaultBrakeStyle => BrakeStyle.Six;

	private void Awake()
	{
		if (trainBrakeControl != null)
		{
			trainBrakeControl.ControlComponentPurpose = ControlPurpose.TrainBrake;
		}
		if (locomotiveBrakeControl != null)
		{
			locomotiveBrakeControl.ControlComponentPurpose = ControlPurpose.LocomotiveBrake;
		}
		if (cutoutControl != null)
		{
			cutoutControl.ControlComponentPurpose = ControlPurpose.TrainBrakeCutOut;
		}
	}

	private void OnEnable()
	{
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		_observer = componentInParent.Observe(PropertyChange.KeyForControl(PropertyChange.Control.BrakeStyle), delegate(Value value)
		{
			string stringValue = value.StringValue;
			BrakeStyle newStyle = (string.IsNullOrWhiteSpace(stringValue) ? DefaultBrakeStyle : BrakeStyleForString(stringValue));
			StyleDidChange(newStyle);
		});
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
	}

	private void StyleDidChange(BrakeStyle newStyle)
	{
		if (DefinitionEditorModeController.IsEditing)
		{
			return;
		}
		if (newStyle == style)
		{
			foreach (GameObject item in _deactivated)
			{
				item.SetActive(value: true);
			}
			_deactivated.Clear();
			return;
		}
		foreach (Transform item2 in base.transform)
		{
			GameObject gameObject = item2.gameObject;
			gameObject.SetActive(value: false);
			_deactivated.Add(gameObject);
		}
	}

	public static string StringForBrakeStyle(BrakeStyle style)
	{
		if (BrakeStyleStringMapping.TryGetValue(style, out var value))
		{
			return value;
		}
		throw new ArgumentOutOfRangeException("style", style, null);
	}

	public static BrakeStyle BrakeStyleForString(string key)
	{
		foreach (var (result, text2) in BrakeStyleStringMapping)
		{
			if (text2 == key)
			{
				return result;
			}
		}
		throw new ArgumentOutOfRangeException("key", key, null);
	}
}
