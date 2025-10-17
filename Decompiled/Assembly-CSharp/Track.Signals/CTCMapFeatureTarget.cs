using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using Serilog;
using UnityEngine;

namespace Track.Signals;

public class CTCMapFeatureTarget : MonoBehaviour
{
	private bool _featureEnabled;

	private void OnEnable()
	{
		SetEnabled(en: true);
	}

	private void OnDisable()
	{
		SetEnabled(en: false);
	}

	private void SetEnabled(bool en)
	{
		if (Application.isPlaying && !StateManager.IsUnloading && _featureEnabled != en)
		{
			_featureEnabled = en;
			Log.Debug("Firing CTCFeatureChange from {name}", base.name);
			Messenger.Default.Send(default(CTCFeatureChange));
		}
	}
}
