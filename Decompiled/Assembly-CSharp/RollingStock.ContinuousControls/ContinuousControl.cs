using System;
using UnityEngine;

namespace RollingStock.ContinuousControls;

public abstract class ContinuousControl : MonoBehaviour, IPickable
{
	public string displayName;

	protected float value;

	public Func<bool> CheckAuthorized = () => true;

	public Func<string> tooltipText = () => "";

	public float ChangeThreshold = 0.01f;

	public Func<float, float> OnCustomSnap;

	protected bool _isActive;

	private float _lastSentValue;

	private float _lastSentTime;

	protected const float ZeroThreshold = 0.001f;

	public float Value
	{
		get
		{
			return value;
		}
		set
		{
			if (!_isActive)
			{
				bool num = Mathf.Abs(this.value - value) > 0.001f;
				this.value = value;
				if (num)
				{
					ValueDidChange();
				}
			}
		}
	}

	public int Priority => 1;

	public float MaxPickDistance { get; set; } = 5f;

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	public TooltipInfo TooltipInfo => new TooltipInfo(displayName, AuthorizationAwareTooltipText());

	public event Action<float> OnValueChanged;

	public virtual void Activate(PickableActivateEvent evt)
	{
		_isActive = true;
		_lastSentValue = value;
	}

	public virtual void Deactivate()
	{
		_isActive = false;
		if (ShouldSendValue(deactivate: true))
		{
			SendValue();
		}
	}

	private string AuthorizationAwareTooltipText()
	{
		if (!CheckAuthorized())
		{
			return "<sprite name=\"MouseNo\"> N/A";
		}
		return tooltipText();
	}

	protected void UserChangedValue(bool force = false)
	{
		if (force || ShouldSendValue(deactivate: false))
		{
			SendValue();
		}
	}

	protected virtual void ValueDidChange()
	{
	}

	private bool ShouldSendValue(bool deactivate)
	{
		if (Time.realtimeSinceStartup - _lastSentTime > 1f)
		{
			return true;
		}
		float num = Mathf.Abs(value - _lastSentValue);
		bool flag = Mathf.Abs(value - 0f) < 0.001f || Mathf.Abs(value - 1f) < 0.001f;
		if (deactivate || flag)
		{
			return num >= 0.001f;
		}
		return num >= ChangeThreshold;
	}

	protected float Snap(float param)
	{
		if (OnCustomSnap != null)
		{
			return OnCustomSnap(param);
		}
		return param;
	}

	public void ConfigureSnap(int numberOfDiscreteValues)
	{
		OnCustomSnap = (float v) => Mathf.Round(v * (float)numberOfDiscreteValues) / (float)numberOfDiscreteValues;
	}

	private void SendValue()
	{
		_lastSentValue = value;
		_lastSentTime = Time.realtimeSinceStartup;
		this.OnValueChanged?.Invoke(value);
	}
}
