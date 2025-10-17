using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KeyValue.Runtime;
using UnityEngine;

namespace Track.Signals;

public class CTCTests : MonoBehaviour
{
	private Coroutine _runningTest;

	private Transform ctcTransform;

	private KeyValueObject ctcKeyValue;

	private TrainController TrainController => TrainController.Shared;

	private static IEnumerable<CTCInterlocking> Interlockings => UnityEngine.Object.FindObjectsOfType<CTCInterlocking>();

	private static IEnumerable<CTCSignal> Signals => UnityEngine.Object.FindObjectsOfType<CTCSignal>();

	private static IEnumerable<CTCBlock> Blocks => UnityEngine.Object.FindObjectsOfType<CTCBlock>();

	private IEnumerable<CTCPanelGroup> PanelGroups => ctcTransform.GetComponentsInChildren<CTCPanelGroup>();

	private void Start()
	{
		ctcTransform = GameObject.Find("CTC").transform;
		ctcKeyValue = ctcTransform.GetComponentInChildren<KeyValueObject>();
	}

	private void OnGUI()
	{
		float x = 20f;
		float num = 20f;
		float width = 80f;
		float num2 = 20f;
		if (GUI.Button(new Rect(x, num, width, num2), "Run Test"))
		{
			_runningTest = StartCoroutine(RunTest());
			RunTest();
		}
		num += num2;
		if (_runningTest != null && GUI.Button(new Rect(x, num, width, num2), "Stop Test"))
		{
			StopCoroutine(_runningTest);
			_runningTest = null;
		}
	}

	private IEnumerator RunTest()
	{
		yield return ResetAll();
		ctcKeyValue["mode"] = Value.Int(1);
		yield return null;
		yield return CoroutineUtils.RunThrowingIterator(TestBasic(), delegate(Exception ex)
		{
			if (ex != null)
			{
				Debug.LogError("Error during test:");
				Debug.LogException(ex);
			}
		});
		yield return null;
		yield return ResetAll();
		yield return null;
		_runningTest = null;
	}

	private IEnumerator ResetAll()
	{
		foreach (CTCBlock block in Blocks)
		{
			block.TestForceOccupied(occupied: false);
		}
		foreach (CTCInterlocking interlocking in Interlockings)
		{
			InterlockingWrapper interlockingWrapper = InterlockingById(interlocking.id);
			interlockingWrapper.SetSwitchAll(SwitchSetting.Normal);
			interlockingWrapper.SetSignal(SignalDirection.None);
			yield return interlockingWrapper.Code();
		}
	}

	private IEnumerator TestBasic()
	{
		InterlockingById("1w");
		InterlockingWrapper interlockingWrapper = InterlockingById("1e");
		InterlockingById("2w");
		interlockingWrapper.SetSwitch(SwitchSetting.Reversed);
		interlockingWrapper.SetSignal(SignalDirection.Right);
		yield return interlockingWrapper.Code();
		yield return null;
		AssertEqual(GetSignalAspect("1ee"), SignalAspect.Stop);
		AssertEqual(GetSignalAspect("1em"), SignalAspect.Stop);
		AssertEqual(GetSignalAspect("1es"), SignalAspect.Clear);
		SetBlockOccupied("1e", occupied: true);
		yield return null;
		SetBlockOccupied("im-a", occupied: true);
		yield return null;
		SetBlockOccupied("1e", occupied: false);
		yield return null;
		SetBlockOccupied("im-a", occupied: false);
		yield return null;
		AssertEqual(GetSignalAspect("1es"), SignalAspect.Stop);
		yield return null;
		Debug.Log("-- Test Complete --");
	}

	private IEnumerator TestDropSignal()
	{
		Debug.Log("-- Test Drop Signal: Start --");
		InterlockingById("1w");
		InterlockingWrapper interlockingWrapper = InterlockingById("1e");
		InterlockingById("2w");
		interlockingWrapper.SetSwitch(SwitchSetting.Reversed);
		interlockingWrapper.SetSignal(SignalDirection.Right);
		yield return interlockingWrapper.Code();
		yield return null;
		AssertEqual(GetSignalAspect("1ee"), SignalAspect.Stop);
		AssertEqual(GetSignalAspect("1em"), SignalAspect.Stop);
		AssertEqual(GetSignalAspect("1es"), SignalAspect.Clear);
		SetBlockOccupied("1e", occupied: true);
		SetBlockOccupied("im-a", occupied: true);
		yield return null;
		SetBlockOccupied("1e", occupied: false);
		SetBlockOccupied("im-a", occupied: false);
		yield return null;
		AssertEqual(GetSignalAspect("1es"), SignalAspect.Stop);
		Debug.Log("-- Test Drop Signal: Complete --");
	}

	private void Assert(bool condition)
	{
		if (condition)
		{
			return;
		}
		throw new Exception("Failed assertion");
	}

	private void AssertEqual(object value, object expected)
	{
		if (value.Equals(expected))
		{
			return;
		}
		throw new Exception($"Expected {value} to equal {expected}");
	}

	private SignalAspect GetSignalAspect(string signalId)
	{
		return Signals.First((CTCSignal sig) => sig.id == signalId).LastShownAspect;
	}

	private void SetBlockOccupied(string blockId, bool occupied)
	{
		Blocks.First((CTCBlock block) => block.id == blockId).TestForceOccupied(occupied);
	}

	private InterlockingWrapper InterlockingById(string id)
	{
		CTCPanelGroup[] panelGroups = PanelGroups.Where((CTCPanelGroup group) => group.interlockingId == id).ToArray();
		return new InterlockingWrapper
		{
			KeyValueObject = ctcKeyValue,
			PanelGroups = panelGroups
		};
	}
}
