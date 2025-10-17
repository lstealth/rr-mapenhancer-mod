using System.Collections.Generic;
using Game;
using Helpers;
using Model.Definition.Data;
using Model.Ops.Definition;
using Serilog;
using Track;
using UI.Builder;
using UnityEngine;

namespace Model.Ops;

public abstract class IndustryComponent : MonoBehaviour, IIndustryTrackDisplayable, IProgressionDisablable
{
	public struct PanelField
	{
		public readonly string Label;

		public readonly string Text;

		public readonly string Tooltip;

		public PanelField(string label, string text, string tooltip)
		{
			Label = label;
			Text = text;
			Tooltip = tooltip;
		}

		public static PanelField InStorage(Load load, float quantityInStorage, float effectiveStorage)
		{
			return new PanelField(load.description, TextSprites.PiePercent(quantityInStorage, effectiveStorage) + " " + load.units.QuantityString(quantityInStorage), "Does not include loads in cars on the line.");
		}
	}

	public string subIdentifier;

	private string _identifier;

	public TrackSpan[] trackSpans;

	public CarTypeFilter carTypeFilter = new CarTypeFilter("");

	[Tooltip("True if this component shares storage with others. Should be true except for captive service loaders.")]
	public bool sharedStorage = true;

	private Industry _cachedIndustry;

	public string Identifier
	{
		get
		{
			if (string.IsNullOrEmpty(_identifier))
			{
				_identifier = Industry.identifier + "." + subIdentifier;
			}
			return _identifier;
		}
	}

	public Industry Industry
	{
		get
		{
			if (_cachedIndustry == null)
			{
				_cachedIndustry = GetComponentInParent<Industry>();
			}
			return _cachedIndustry;
		}
	}

	public virtual bool IsVisible
	{
		get
		{
			if (trackSpans.Length != 0)
			{
				return !ProgressionDisabled;
			}
			return false;
		}
	}

	public bool ProgressionDisabled { get; set; }

	public virtual string DisplayName => base.name;

	public IEnumerable<TrackSpan> TrackSpans => trackSpans;

	public Vector3 CenterPoint
	{
		get
		{
			if (trackSpans.Length != 0)
			{
				return trackSpans[0].GetCenterPoint();
			}
			return WorldTransformer.WorldToGame(base.transform.position);
		}
	}

	private void Start()
	{
		ValidateIndustryComponent();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow * 0.8f;
		Gizmos.DrawCube(base.transform.position, Vector3.one * 2f);
	}

	public override string ToString()
	{
		return DisplayName + " (" + Identifier + ")";
	}

	protected virtual void ValidateIndustryComponent()
	{
		if (carTypeFilter.IsEmpty)
		{
			Log.Error("Industry component has no car types: {identifier}", Identifier);
		}
		if (trackSpans.Length == 0)
		{
			Log.Error("Industry component has no car spans: {identifier}", Identifier);
		}
	}

	public virtual void Initialize(IIndustryContext ctx, GameVersion fromVersion)
	{
	}

	protected static float RateToValue(float rate, float dt)
	{
		float timeMultiplier = TimeWeather.TimeMultiplier;
		if (timeMultiplier < 0.001f)
		{
			return 0f;
		}
		float num = 86400f / timeMultiplier;
		return dt / num * rate;
	}

	public virtual void CheckForCompleted(IIndustryContext ctx)
	{
		foreach (IOpsCar item in EnumerateCars(ctx))
		{
			Waybill? waybill = item.Waybill;
			if (waybill.HasValue)
			{
				Waybill valueOrDefault = waybill.GetValueOrDefault();
				if (valueOrDefault.Destination.Equals(this) && !valueOrDefault.Completed)
				{
					OnCompleteWaybill(ctx, item, valueOrDefault);
				}
			}
		}
	}

	public virtual bool WantsAutoDestination(AutoDestinationType type)
	{
		return false;
	}

	public abstract void Service(IIndustryContext ctx);

	public abstract void OrderCars(IIndustryContext ctx);

	public virtual void DailyReceivables(GameDateTime now, IIndustryContext ctx)
	{
	}

	public virtual void DailyPayables(GameDateTime now, IIndustryContext ctx)
	{
	}

	protected virtual void OnCompleteWaybill(IIndustryContext ctx, IOpsCar car, Waybill waybill)
	{
		ctx.PayWaybill(car, waybill);
		waybill.PaymentOnArrival = 0;
		waybill.Completed = true;
		float num = (TimeWeather.Now.TotalHours - waybill.Created.TotalHours) / 24f;
		car.SetWaybill(waybill, this, $"Paid Completed ({num:F1} days)");
		Industry.ReceivedCarCount++;
	}

	protected IEnumerable<IOpsCar> EnumerateCars(IIndustryContext ctx, bool requireWaybill = false)
	{
		foreach (IOpsCar item in ctx.CarsAtPosition())
		{
			if (!CarTypeMatches(item))
			{
				continue;
			}
			if (requireWaybill)
			{
				Waybill? waybill = item.Waybill;
				if (!waybill.HasValue || !waybill.Value.Destination.Equals(this))
				{
					continue;
				}
			}
			yield return item;
		}
	}

	private bool CarTypeMatches(IOpsCar car)
	{
		string carType = car.CarType;
		return carTypeFilter.Matches(carType);
	}

	public static implicit operator OpsCarPosition(IndustryComponent c)
	{
		return new OpsCarPosition(c.DisplayName, c.Identifier, c.trackSpans);
	}

	public virtual void BuildPanel(UIPanelBuilder builder)
	{
	}

	public virtual IEnumerable<PanelField> PanelFields(IndustryContext industryContext)
	{
		yield break;
	}

	public virtual void EnsureConsistency()
	{
	}

	public virtual bool AcceptsCarsWithLoad(Load load)
	{
		return true;
	}
}
