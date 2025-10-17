using System.Collections.Generic;
using System.Linq;
using Core;
using Game;
using Game.State;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model.Ops.Definition;
using Network;
using Serilog;
using UI.Builder;
using UnityEngine;

namespace Model.Ops;

public class InterchangedIndustryLoader : IndustryComponent
{
	private Interchange _interchange;

	private bool? _hasInterchange;

	public Load load;

	[SerializeField]
	private Ledger.Category ledgerCategory;

	public override string DisplayName
	{
		get
		{
			Interchange interchange = Interchange;
			if (interchange == null)
			{
				return base.name;
			}
			return interchange.DisplayName + " to " + base.name;
		}
	}

	internal string InterchangeName
	{
		get
		{
			if (!(Interchange == null))
			{
				return Interchange.DisplayName;
			}
			return null;
		}
	}

	[CanBeNull]
	private Interchange Interchange
	{
		get
		{
			if (!_hasInterchange.HasValue)
			{
				_interchange = base.Industry.GetComponentInChildren<Interchange>();
				_hasInterchange = _interchange != null;
			}
			return _interchange;
		}
	}

	private string KeyBardoCars => "br-" + subIdentifier;

	private Hyperlink HyperlinkToThis => Hyperlink.To(base.Industry, base.name);

	public override bool WantsAutoDestination(AutoDestinationType type)
	{
		return type == AutoDestinationType.Empty;
	}

	public override void Service(IIndustryContext ctx)
	{
	}

	public override void OrderCars(IIndustryContext ctx)
	{
		Interchange componentInChildren = base.Industry.GetComponentInChildren<Interchange>();
		foreach (var item in EnumerateBardoCars())
		{
			var (carId, _) = item;
			if (!(item.returnTime > ctx.Now))
			{
				componentInChildren.OrderReturnFromBardo(carId);
			}
		}
	}

	public void ServeInterchange(IIndustryContext ctx, Interchange interchange)
	{
		StateManager shared = StateManager.Shared;
		List<IOpsCar> list = (from car in EnumerateCars(ctx, requireWaybill: true)
			where car.IsEmptyOrContains(load)
			select car).ToList();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		GameDateTime returnTime = ctx.Now.AddingDays(23f / 24f);
		foreach (IOpsCar item3 in list)
		{
			(float quantity, float capacity) tuple = item3.QuantityOfLoad(load);
			float item = tuple.quantity;
			float item2 = tuple.capacity;
			int num4 = Mathf.RoundToInt((item2 - item) * load.costPerUnit);
			if (num4 > 0)
			{
				if (!shared.CanAfford(num4 + num2))
				{
					num++;
					continue;
				}
				num2 += num4;
				num3++;
			}
			item3.Load(load, item2);
			item3.SetWaybill(null, this, "Full");
			ctx.MoveToBardo(item3);
			ScheduleReturnFromBardo(item3, returnTime);
		}
		if (num2 > 0)
		{
			base.Industry.ApplyToBalance(-num2, ledgerCategory, null, num3, quiet: true);
			Multiplayer.Broadcast(string.Format("Ordered {0} of {1} at {2} for {3:C0}. Expected return: {4}.", num3.Pluralize("car"), load.description, HyperlinkToThis, num2, 1.Pluralize("day")));
		}
		if (num > 0)
		{
			Multiplayer.Broadcast(string.Format("{0}: Insufficient funds to fill {1} of {2}.", HyperlinkToThis, num.Pluralize("car"), load.description));
		}
	}

	private void SetBardoCarsValue(string key, Value value)
	{
		IReadOnlyDictionary<string, Value> dictionaryValue = base.Industry.KeyValueObject[KeyBardoCars].DictionaryValue;
		if (dictionaryValue.TryGetValue(key, out var value2))
		{
			if (value2.Equals(value))
			{
				return;
			}
		}
		else if (value.IsNull)
		{
			return;
		}
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>(dictionaryValue);
		if (value.IsNull)
		{
			dictionary.Remove(key);
		}
		else
		{
			dictionary[key] = value;
		}
		base.Industry.KeyValueObject[KeyBardoCars] = (dictionary.Any() ? Value.Dictionary(dictionary) : Value.Null());
	}

	private void ScheduleReturnFromBardo(IOpsCar car, GameDateTime returnTime)
	{
		SetBardoCarsValue(car.Id, (int)returnTime.TotalSeconds);
	}

	public void InterchangeDidFillReturnFromBardoOrder(string carId)
	{
		SetBardoCarsValue(carId, Value.Null());
	}

	private IEnumerable<(string carId, GameDateTime returnTime)> EnumerateBardoCars()
	{
		Value value = base.Industry.KeyValueObject[KeyBardoCars];
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		foreach (KeyValuePair<string, Value> item2 in dictionaryValue)
		{
			item2.Deconstruct(out var key, out value);
			string item = key;
			Value value2 = value;
			yield return (carId: item, returnTime: new GameDateTime(value2.FloatValue));
		}
	}

	public override void EnsureConsistency()
	{
		if (!base.ProgressionDisabled && !base.Industry.ProgressionDisabled)
		{
			return;
		}
		TrainController shared = TrainController.Shared;
		OpsController shared2 = OpsController.Shared;
		foreach (var item3 in EnumerateBardoCars())
		{
			string item = item3.carId;
			GameDateTime item2 = item3.returnTime;
			if (shared.TryGetCarForId(item, out var car) && !(car.Bardo != base.Identifier))
			{
				Vector3 thisPosition = base.transform.position;
				InterchangedIndustryLoader interchangedIndustryLoader = (from iil in shared2.EnabledInterchanges.SelectMany((Interchange interchange) => interchange.Industry.GetComponentsInChildren<InterchangedIndustryLoader>())
					where iil != this
					orderby Vector3.Distance(iil.transform.position, thisPosition)
					select iil).FirstOrDefault();
				if (interchangedIndustryLoader == null)
				{
					Log.Warning("Couldn't find another InterchangedIndustryLoader to move car {car} to; clearing bardo.", car);
					car.Bardo = null;
				}
				else
				{
					Log.Information("Retargeting bardo car {car} to {other}", car, interchangedIndustryLoader);
					car.Bardo = interchangedIndustryLoader.Identifier;
					interchangedIndustryLoader.ScheduleReturnFromBardo(new OpsCarAdapter(car, shared2), item2);
				}
			}
		}
		base.Industry.KeyValueObject[KeyBardoCars] = null;
	}

	public override void BuildPanel(UIPanelBuilder builder)
	{
		builder.AddSection("Via Interchange: " + base.name + " - " + load.description, delegate(UIPanelBuilder uIPanelBuilder)
		{
			float nominalQuantityPerCarLoad = load.NominalQuantityPerCarLoad;
			uIPanelBuilder.AddField("Loads Car Types", carTypeFilter.ToString());
			uIPanelBuilder.AddField("Cost", $"{Mathf.RoundToInt(nominalQuantityPerCarLoad * load.costPerUnit):C0} per {load.QuantityString(nominalQuantityPerCarLoad)} car");
		});
	}
}
