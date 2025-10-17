using System;
using UnityEngine;

namespace Game.Settings;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSettingsApplicator : MonoBehaviour
{
	private void Start()
	{
		if (base.gameObject.TryGetComponent<ParticleSystem>(out var component))
		{
			switch (Preferences.GraphicsParticleLevel)
			{
			case Preferences.ParticleLevel.Off:
				component.Stop();
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case Preferences.ParticleLevel.Low:
			case Preferences.ParticleLevel.Standard:
				break;
			}
		}
	}
}
