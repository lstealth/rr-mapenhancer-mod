using System;
using Audio;
using Audio.DynamicChuff;
using Helpers.Culling;
using RollingStock.Steam;
using UnityEngine;

namespace RollingStock;

public class Chuff : MonoBehaviour, IChuffProvider, ISteamLocomotiveSubcomponent, CullingManager.ICullingEventHandler
{
	[SerializeField]
	private ChuffProfile profile;

	[SerializeField]
	private ChuffFilter chuffFilter;

	private float _driverCircumference;

	private bool _movedLastFixedUpdate;

	private float _absVelocity;

	private float _tractiveEffort;

	private float _tractiveEffortReported;

	private CullingManager.Token _cullingToken;

	private float _absThrottle;

	[Range(0.1f, 10f)]
	[SerializeField]
	private float throttleResponsiveness = 1f;

	public IDynamicChuffDelegate Delegate { get; set; }

	public void Configure(float driverDiameter, float normalizedEngineSize)
	{
		_driverCircumference = driverDiameter * MathF.PI;
		chuffFilter.engineSize = normalizedEngineSize;
		chuffFilter.UpdateForEngineCharacteristics();
	}

	private void OnEnable()
	{
		_absThrottle = 0f;
		chuffFilter.engineThrottle = 0f;
		_cullingToken = CullingManager.Scenery.AddSphere(base.transform, 1f, this);
		_cullingToken.RegisterFixedUpdate(base.transform);
		if (chuffFilter.TryGetComponent<AudioSource>(out var component))
		{
			PrepareAudioSource(component);
			component.volume = 1f;
		}
	}

	private void OnDisable()
	{
		_cullingToken?.Dispose();
		_cullingToken = null;
	}

	private void FixedUpdate()
	{
		if (!_movedLastFixedUpdate)
		{
			_absVelocity = 0f;
		}
		_movedLastFixedUpdate = false;
		_tractiveEffort = Mathf.Lerp(_tractiveEffort, _tractiveEffortReported, Time.deltaTime);
		float engineSpeed = _absVelocity / _driverCircumference;
		chuffFilter.engineSpeed = engineSpeed;
		chuffFilter.engineNormalizedTE = _tractiveEffort;
		chuffFilter.engineThrottle = Mathf.Lerp(chuffFilter.engineThrottle, _absThrottle, Time.deltaTime * throttleResponsiveness);
		if (_absVelocity < 5f)
		{
			float nextChuffDelay = chuffFilter.GetNextChuffDelay();
			if (nextChuffDelay < 0.1f)
			{
				Delegate.ScheduleNextChuff(nextChuffDelay, 0.2f);
			}
		}
	}

	private void PrepareAudioSource(AudioSource source)
	{
		source.volume = 0f;
		source.priority = 10;
		source.outputAudioMixerGroup = AudioController.Group.LocomotiveChuff;
	}

	public void ApplyDistanceMoved(MovementInfo info, float driverVelocity, float absReverser, float absThrottle, float driverPhase)
	{
		_movedLastFixedUpdate = true;
		_absVelocity = Mathf.Abs(driverVelocity);
		chuffFilter.engineCutoff = absReverser;
		_absThrottle = absThrottle;
		_tractiveEffortReported = Mathf.Clamp01(info.TractiveEffort);
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		bool active = distanceBand < 2;
		chuffFilter.gameObject.SetActive(active);
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}
}
