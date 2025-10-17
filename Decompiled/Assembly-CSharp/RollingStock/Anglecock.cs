using System.Linq;
using Audio;
using Game.Messages;
using Game.State;
using Model;
using RollingStock.ContinuousControls;
using UI;
using UnityEngine;

namespace RollingStock;

public class Anglecock : MonoBehaviour
{
	public enum GladhandClickAction
	{
		None,
		Connect,
		Disconnect
	}

	public ContinuousControl control;

	public AudioClip airFlowAudioClip;

	public Hose hose;

	private Car.End _carEnd;

	private string _carId;

	private IAudioSource _airFlowAudioSource;

	private float _flowDisplay;

	private bool _connectedDisplay;

	private MeshRenderer[] _meshRenderers;

	private Car _cachedCar;

	public float Flow
	{
		get
		{
			return _flowDisplay;
		}
		set
		{
			if (!(Mathf.Abs(value - _flowDisplay) < 0.01f))
			{
				_flowDisplay = value;
				UpdateFlowAudio();
			}
		}
	}

	public bool IsConnected
	{
		get
		{
			return _connectedDisplay;
		}
		set
		{
			if (_connectedDisplay != value)
			{
				_connectedDisplay = value;
				UpdateFlowAudio();
			}
		}
	}

	private Car Car
	{
		get
		{
			if (_cachedCar == null)
			{
				_cachedCar = GetComponentInParent<Car>();
			}
			return _cachedCar;
		}
	}

	public void Setup(Car.End carEnd, string carId)
	{
		_carEnd = carEnd;
		_carId = carId;
	}

	private void OnEnable()
	{
		control.OnValueChanged -= ControlDidChange;
		control.OnValueChanged += ControlDidChange;
		control.CheckAuthorized = () => StateManager.CheckAuthorizedToSendMessage(new PropertyChange(_carId, "f.anglecock", new FloatPropertyValue(0f)));
		control.MaxPickDistance = 15f;
	}

	private void OnDisable()
	{
		control.OnValueChanged -= ControlDidChange;
	}

	private void UpdateFlowAudio()
	{
		if (_flowDisplay > 0.1f && !IsConnected)
		{
			if (_airFlowAudioSource == null)
			{
				_airFlowAudioSource = VirtualAudioSourcePool.Checkout("AirFlow", airFlowAudioClip, loop: true, AudioController.Group.AirOpen, 11, base.transform, AudioDistance.Nearby);
				_airFlowAudioSource.volume = 0f;
				_airFlowAudioSource.minDistance = 2f;
				_airFlowAudioSource.maxDistance = 20f;
				_airFlowAudioSource.Play();
			}
			_airFlowAudioSource.volume = Mathf.InverseLerp(0f, 100f, _flowDisplay);
		}
		else if (_airFlowAudioSource != null)
		{
			VirtualAudioSourcePool.Return(_airFlowAudioSource);
			_airFlowAudioSource = null;
		}
	}

	private void OnDestroy()
	{
		VirtualAudioSourcePool.Return(_airFlowAudioSource);
	}

	private void ControlDidChange(float value)
	{
		Car car = Car;
		car.ApplyEndGearChange(car.EndToLogical(_carEnd), Car.EndGearStateKey.Anglecock, value);
	}

	public GladhandClickAction GladhandClickConnects()
	{
		Car car = Car;
		if (IsConnected)
		{
			return GladhandClickAction.Disconnect;
		}
		Car.LogicalEnd logicalEnd = car.EndToLogical(_carEnd);
		if ((bool)car.CoupledTo(logicalEnd))
		{
			return GladhandClickAction.Connect;
		}
		return GladhandClickAction.None;
	}

	public void GladhandClick()
	{
		GladhandClickAction gladhandClickAction = GladhandClickConnects();
		if (gladhandClickAction != GladhandClickAction.None)
		{
			Car car = Car;
			Car.LogicalEnd logicalEnd = car.EndToLogical(_carEnd);
			bool num = logicalEnd == Car.LogicalEnd.A;
			Car car2 = car.CoupledTo(logicalEnd) ?? car.AirConnectedTo(logicalEnd);
			bool flag = gladhandClickAction == GladhandClickAction.Connect;
			Car car4;
			Car car5;
			if (!num)
			{
				Car car3 = car2;
				car4 = car;
				car5 = car3;
			}
			else
			{
				Car car3 = car;
				car4 = car2;
				car5 = car3;
			}
			StateManager.ApplyLocal(new SetGladhandsConnected(car4.id, car5.id, flag));
			if (GameInput.SmartAirHelperModifier)
			{
				int num2 = (flag ? 1 : 0);
				car.ApplyEndGearChange(logicalEnd, Car.EndGearStateKey.Anglecock, num2);
				Car.LogicalEnd logicalEnd2 = ((logicalEnd == Car.LogicalEnd.A) ? Car.LogicalEnd.B : Car.LogicalEnd.A);
				car2.ApplyEndGearChange(logicalEnd2, Car.EndGearStateKey.Anglecock, num2);
			}
		}
	}

	public void SetVisible(bool visible)
	{
		if (_meshRenderers == null)
		{
			_meshRenderers = (from mr in GetComponentsInChildren<MeshRenderer>()
				where mr.enabled
				select mr).ToArray();
		}
		MeshRenderer[] meshRenderers = _meshRenderers;
		for (int num = 0; num < meshRenderers.Length; num++)
		{
			meshRenderers[num].enabled = visible;
		}
	}
}
