using System.Collections;
using System.Text;
using Game.Messages;
using Game.State;
using Helpers;
using Model;
using Model.Definition;
using Serilog;
using UnityEngine;

namespace RollingStock;

public class OilPointPickable : MonoBehaviour, IPickable
{
	[SerializeField]
	private HotboxEffect hotboxEffect;

	private Car _car;

	private string _cachedTooltipText;

	private float _cachedTooltipTextTime;

	private Coroutine _coroutine;

	private float _pendingOil;

	private bool _cachedTooFast;

	private int _cachedOilInt = -1;

	private bool _cachedHasHotbox;

	private float _displayOiled;

	private float _displayOiledTimeout;

	private static readonly string ClickToOil = "<sprite name=\"MouseLeft\"> Oil";

	private bool TooFast => _car.VelocityMphAbs > 5f;

	public float MaxPickDistance => 20f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo
	{
		get
		{
			string title = TooltipTitle();
			string text = TooltipText();
			return new TooltipInfo
			{
				Title = title,
				Text = text
			};
		}
	}

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private void Awake()
	{
		base.gameObject.layer = Layers.Clickable;
	}

	public void Configure(Car car, float axleSeparation, float diameter, int index)
	{
		_car = car;
		if (hotboxEffect != null)
		{
			hotboxEffect.Configure(car, axleSeparation, diameter, index);
		}
		BoxCollider[] components = GetComponents<BoxCollider>();
		if (components.Length == 2)
		{
			for (int i = 0; i < components.Length; i++)
			{
				BoxCollider obj = components[i];
				int num = ((i != 0) ? 1 : (-1));
				obj.center = new Vector3(num, diameter * 0.5f, 0f);
				obj.size = new Vector3(0.2f, diameter * 0.5f, axleSeparation + diameter * 0.5f);
			}
		}
		else
		{
			Log.Warning("OilPointPickable: Unexpected collider count {count}", components.Length);
		}
	}

	public void Activate(PickableActivateEvent evt)
	{
		if (!TooFast)
		{
			Log.Information("Activate OilPointPickable {car}", _car);
			_coroutine = StartCoroutine(OilCoroutine());
		}
	}

	public void Deactivate()
	{
		Log.Information("Deactivate OilPointPickable");
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
		Bank();
	}

	private string TooltipTitle()
	{
		string text = ((_car.Archetype == CarArchetype.LocomotiveSteam) ? "Oiling Points" : "Bearings");
		return _car.DisplayName + " " + text;
	}

	private string TooltipText()
	{
		int num = Mathf.RoundToInt(DisplayOiled() * 100f);
		bool tooFast = TooFast;
		bool hasHotbox = _car.HasHotbox;
		if (_cachedOilInt != num || _cachedTooFast != tooFast || _cachedHasHotbox != hasHotbox)
		{
			_cachedOilInt = num;
			_cachedTooFast = tooFast;
			_cachedHasHotbox = hasHotbox;
			StringBuilder stringBuilder = new StringBuilder(128);
			if (hasHotbox)
			{
				stringBuilder.AppendLine("<sprite name=\"Flame\"> Hotbox");
			}
			string text = $"{TextSprites.PiePercent(num, 100f)} {num}% Oiled";
			if (num < 10)
			{
				text = text.ColorRed();
			}
			else if (num < 25)
			{
				text = text.ColorOrange();
			}
			stringBuilder.Append(text);
			if (num < 100 && !tooFast)
			{
				stringBuilder.Append("\n" + ClickToOil);
			}
			_cachedTooltipText = stringBuilder.ToString();
		}
		return _cachedTooltipText;
	}

	private float DisplayOiled()
	{
		if (!(Time.unscaledTime < _displayOiledTimeout))
		{
			return _car.Oiled;
		}
		return _displayOiled;
	}

	private IEnumerator OilCoroutine()
	{
		while (_pendingOil + _car.Oiled < 0.999f)
		{
			float num = 0.02f;
			_pendingOil = Mathf.Clamp(_pendingOil + num, 0f, 1f - _car.Oiled);
			_displayOiled = _pendingOil + _car.Oiled;
			_displayOiledTimeout = Time.unscaledTime + 1f;
			yield return new WaitForSecondsRealtime(0.1f);
		}
		Bank();
		_coroutine = null;
	}

	private void Bank()
	{
		if (_pendingOil < 0.001f)
		{
			_pendingOil = 0f;
			return;
		}
		StateManager.ApplyLocal(new RequestOilCar(_car.id, _pendingOil));
		_pendingOil = 0f;
	}
}
