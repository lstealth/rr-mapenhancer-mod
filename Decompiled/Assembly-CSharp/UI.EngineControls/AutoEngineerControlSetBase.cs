using System;
using System.Collections.Generic;
using System.Linq;
using Game.Messages;
using Game.State;
using Model.AI;
using TMPro;
using UI.Builder;
using UnityEngine;

namespace UI.EngineControls;

public abstract class AutoEngineerControlSetBase : EngineControlSetBase
{
	[SerializeField]
	private TMP_Text statusLabel;

	[SerializeField]
	private DropdownMenu contextualOrdersDropdown;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private AutoEngineerPersistence _persistence;

	protected AutoEngineerOrdersHelper OrdersHelper { get; private set; }

	private void OnDestroy()
	{
		ClearObservers();
	}

	private void ClearObservers()
	{
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
	}

	public override void OnOrdersDidChange(Orders orders)
	{
		base.OnOrdersDidChange(orders);
		if (!(base.Locomotive == null))
		{
			_persistence = new AutoEngineerPersistence(base.Locomotive.KeyValueObject);
			OrdersHelper = new AutoEngineerOrdersHelper(base.Locomotive, _persistence);
			ClearObservers();
			_observers.Add(_persistence.ObservePlannerStatusChanged(UpdateStatusLabel));
			_observers.Add(_persistence.ObserveContextualOrdersChanged(UpdateContextualOrders));
			UpdateStatusLabel();
			UpdateContextualOrders();
		}
	}

	private void UpdateStatusLabel()
	{
		string plannerStatus = _persistence.PlannerStatus;
		statusLabel.text = plannerStatus;
		statusLabel.rectTransform.Tooltip("Status", plannerStatus);
	}

	private void UpdateContextualOrders()
	{
		List<ContextualOrder> contextualOrders = _persistence.ContextualOrders;
		bool flag = contextualOrders.Any();
		contextualOrdersDropdown.gameObject.SetActive(flag);
		if (flag)
		{
			List<DropdownMenu.RowData> options = contextualOrders.Select(delegate(ContextualOrder co)
			{
				var (title, subtitle) = StringsForContextualOrder(co);
				return new DropdownMenu.RowData(DropdownMenu.CheckState.None, title, subtitle);
			}).ToList();
			contextualOrdersDropdown.Configure(options, delegate(int index)
			{
				ContextualOrder contextualOrder = contextualOrders[index];
				StateManager.ApplyLocal(new AutoEngineerContextualOrder(base.Locomotive.id, contextualOrder.Order, contextualOrder.Context));
			});
		}
	}

	private static (string text, string tooltip) StringsForContextualOrder(ContextualOrder co)
	{
		return co.Order switch
		{
			ContextualOrder.OrderValue.PassSignal => (text: "Pass Signal", tooltip: "Pass the signal at restricted speed"), 
			ContextualOrder.OrderValue.PassFlare => (text: "Pass Fusee", tooltip: "Pass fusee and continue"), 
			ContextualOrder.OrderValue.ResumeSpeed => (text: "Resume Speed", tooltip: "Discard speed restriction"), 
			ContextualOrder.OrderValue.BypassTimetable => (text: "Bypass Timetable", tooltip: "Depart before scheduled time"), 
			_ => (text: "(Error)", tooltip: ""), 
		};
	}
}
