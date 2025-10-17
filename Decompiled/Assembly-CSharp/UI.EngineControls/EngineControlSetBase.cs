using System;
using System.Collections.Generic;
using Game.Messages;
using Model;
using Model.AI;
using Serilog;
using UnityEngine;

namespace UI.EngineControls;

public abstract class EngineControlSetBase : MonoBehaviour
{
	private bool _updatingControls;

	private bool _updatingEngine;

	protected BaseLocomotive Locomotive { get; private set; }

	protected abstract void UpdateControls();

	private void Update()
	{
		_updatingControls = true;
		try
		{
			UpdateControls();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception from UpdateControls");
		}
		_updatingControls = false;
	}

	public virtual OptionsDropdownConfiguration ConfigureOptionsDropdown()
	{
		return new OptionsDropdownConfiguration(new List<DropdownMenu.RowData>(), delegate
		{
		});
	}

	protected void ChangeValue(PropertyChange.Control control, float value)
	{
		if (!_updatingControls && !_updatingEngine)
		{
			BaseLocomotive locomotive = Locomotive;
			if (locomotive == null)
			{
				Debug.LogError("ChangeValue with no selected locomotive!");
			}
			else
			{
				locomotive.SendPropertyChange(control, value);
			}
		}
	}

	public virtual void OnOrdersDidChange(Orders orders)
	{
		TrainController shared = TrainController.Shared;
		if (shared == null)
		{
			return;
		}
		BaseLocomotive selectedLocomotive = shared.SelectedLocomotive;
		if (selectedLocomotive == Locomotive)
		{
			return;
		}
		Locomotive = selectedLocomotive;
		try
		{
			_updatingEngine = true;
			UpdateForLocomotive();
		}
		finally
		{
			_updatingEngine = false;
		}
	}

	protected virtual void UpdateForLocomotive()
	{
	}
}
