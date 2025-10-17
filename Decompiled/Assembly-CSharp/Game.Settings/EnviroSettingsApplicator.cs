using Enviro;
using GalaSoft.MvvmLight.Messaging;
using UnityEngine;

namespace Game.Settings;

public class EnviroSettingsApplicator : MonoBehaviour
{
	[SerializeField]
	private EnviroManager manager;

	private void Awake()
	{
	}

	private void OnEnable()
	{
		Messenger.Default.Register<EnviroSettingChanged>(this, delegate
		{
			UpdateSetting();
		});
		UpdateSetting();
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void UpdateSetting()
	{
	}
}
