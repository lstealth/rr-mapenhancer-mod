using Audio;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers.Animation;
using Serilog;
using UnityEngine;

namespace Track;

public class SwitchStand : MonoBehaviour
{
	[Header("Animation")]
	public Animator animator;

	public AnimationClip animationClip;

	public float animationSpeed = 1f;

	[Tooltip("CTC switches shouldn't show manually thrown state (until we support that interaction).")]
	public bool supressThrownAnimation;

	[Header("Audio")]
	public AudioClip audioClip;

	private static readonly int Thrown = Animator.StringToHash("thrown");

	private TrackNode _node;

	private bool _wasThrown;

	private PlayableHandle _playable;

	private bool DisplayThrown
	{
		get
		{
			if (!_node.IsCTCSwitch)
			{
				return _node.isThrown;
			}
			return _node.CTCDisplayThrown;
		}
	}

	private float playableTargetSpeed => animationSpeed * (float)(DisplayThrown ? 1 : (-1));

	private void OnEnable()
	{
		Messenger.Default.Register<SwitchThrownDidChange>(this, HandleSwitchThrownDidChange);
		_wasThrown = DisplayThrown;
		if (_playable == null)
		{
			_playable = animator.PlayableGraphAdapter().AddPlayable(animationClip);
		}
		_playable.Time = (DisplayThrown ? animationClip.length : 0f);
		_playable.Speed = playableTargetSpeed;
		_playable.Play();
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		_playable?.Pause();
	}

	private void OnDestroy()
	{
		_playable?.Dispose();
		_playable = null;
	}

	public void Configure(TrackNode node)
	{
		_node = node;
	}

	private void HandleSwitchThrownDidChange(SwitchThrownDidChange evt)
	{
		if (!(evt.Node != _node) && DisplayThrown != _wasThrown)
		{
			_wasThrown = DisplayThrown;
			_playable.ClampTimeToClipBounds();
			_playable.Speed = playableTargetSpeed;
			Log.Debug("SwitchStand.Refresh: t = {playableTime}, s = {playableSpeed}, state = {playState}", _playable.Time, _playable.Speed, _playable.PlayState);
			if (audioClip != null)
			{
				IAudioSource audioSource = VirtualAudioSourcePool.Checkout("SwitchStand", audioClip, loop: false, AudioController.Group.PlayerAction, 5, base.transform, AudioDistance.Nearby);
				audioSource.minDistance = 1f;
				audioSource.maxDistance = 5000f;
				audioSource.Play();
				VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
			}
		}
	}
}
