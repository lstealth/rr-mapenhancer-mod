using GalaSoft.MvvmLight.Messaging;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Settings;

public class SoundSettingsApplicator : MonoBehaviour
{
	[SerializeField]
	private AudioMixer mixer;

	private float _defaultMaster;

	private float _defaultEngine;

	private float _defaultBell;

	private float _defaultWhistle;

	private float _defaultDynamo;

	private float _defaultEnvironment;

	private float _defaultCtcBell;

	private float _defaultWheels;

	private void Start()
	{
		Messenger.Default.Register<SoundVolumeChanged>(this, delegate
		{
			UpdateMixer();
		});
		_defaultMaster = GetNorm("VolMaster");
		_defaultEngine = GetNorm("VolEngine");
		_defaultBell = GetNorm("VolEngineBell");
		_defaultWhistle = GetNorm("VolEngineWhistle");
		_defaultDynamo = GetNorm("VolDynamo");
		_defaultEnvironment = GetNorm("VolEnvironment");
		_defaultCtcBell = GetNorm("VolCtcBell");
		_defaultWheels = GetNorm("VolWheels");
		UpdateMixer();
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void UpdateMixer()
	{
		SetNorm("VolMaster", Preferences.SoundVolumeMain * _defaultMaster);
		SetNorm("VolEngine", Preferences.SoundVolumeEngine * _defaultEngine);
		SetNorm("VolEngineBell", Preferences.SoundVolumeBell * _defaultBell);
		SetNorm("VolEngineWhistle", Preferences.SoundVolumeWhistle * _defaultWhistle);
		SetNorm("VolDynamo", Preferences.SoundVolumeDynamo * _defaultDynamo);
		SetNorm("VolEnvironment", Preferences.SoundVolumeEnvironment * _defaultEnvironment);
		SetNorm("VolCtcBell", Preferences.SoundVolumeCtcBell * _defaultCtcBell);
		SetNorm("VolWheels", Preferences.SoundVolumeWheels * _defaultWheels);
	}

	private float GetNorm(string n)
	{
		mixer.GetFloat(n, out var value);
		return AudioDbToNorm(value);
	}

	private void SetNorm(string n, float norm)
	{
		mixer.SetFloat(n, AudioNormToDb(norm));
	}

	private static float AudioNormToDb(float f)
	{
		return Mathf.Log10(Mathf.Max(f, 0.0001f)) * 20f;
	}

	private static float AudioDbToNorm(float db)
	{
		return Mathf.Pow(10f, db / 20f);
	}
}
