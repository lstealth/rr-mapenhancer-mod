using System.Linq;
using Audio;
using Model;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RollingStock;

public class Coupler : MonoBehaviour
{
	public const float Inset = -0.276f;

	public Car car;

	public Car.End end;

	public CouplerPickable pickable;

	public AudioClip audioClipClose;

	public AudioClip audioClipOpen;

	public AnimationClip openCloseAnimationClip;

	public Animator animator;

	private bool? _isOpen;

	private TrainController _controller;

	private PlayableGraph _playableGraph;

	private AnimationClipPlayable _openClosePlayable;

	private MeshRenderer[] _meshRenderers;

	public AudioClip slackInClip;

	public AudioClip slackOutClip;

	private void Awake()
	{
		_controller = TrainController.Shared;
		pickable.activate = delegate
		{
			car.HandleCouplerClick(this);
		};
	}

	private void OnEnable()
	{
		_openClosePlayable = AnimationPlayableUtilities.PlayClip(animator, openCloseAnimationClip, out _playableGraph);
		_openClosePlayable.Play();
	}

	private void OnDestroy()
	{
		_playableGraph.Destroy();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = ((end == Car.End.F) ? Color.green : Color.red);
		Gizmos.DrawWireCube(base.transform.position, Vector3.one * 0.2f);
	}

	public void SetOpen(bool open)
	{
		bool flag = !_isOpen.HasValue;
		pickable.isOpen = open;
		if (_isOpen != open)
		{
			_isOpen = open;
			_openClosePlayable.SetSpeed(open ? (-20) : 10);
			_openClosePlayable.SetTime(open ? openCloseAnimationClip.length : 0f);
			if (!flag && car != null && car.EndGearA.Coupler == this && base.gameObject.activeSelf)
			{
				AudioClip clip = (open ? audioClipOpen : audioClipClose);
				AudioController.Group mixerGroup = (open ? AudioController.Group.CouplerOpen : AudioController.Group.CouplerCouple);
				IAudioSource audioSource = VirtualAudioSourcePool.Checkout("CouplerSetOpen", clip, loop: false, mixerGroup, 5, base.transform, AudioDistance.Nearby);
				audioSource.Play();
				VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
			}
		}
	}

	private void PlayOneShot(AudioClip clip, float volume)
	{
		if (base.gameObject.activeSelf)
		{
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout("CouplerOneShot", clip, loop: false, AudioController.Group.CouplerCouple, 30, base.transform, AudioDistance.Local);
			audioSource.volume = volume;
			audioSource.pitch = Random.Range(0.8f, 1.2f);
			audioSource.Play();
			VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
		}
	}

	public void SlackIn(float slackDiffNormalized)
	{
		PlayOneShot(slackInClip, slackDiffNormalized);
	}

	public void SlackOut(float slackDiffNormalized)
	{
		PlayOneShot(slackOutClip, slackDiffNormalized);
	}

	public void SetVisible(bool visible)
	{
		if (_meshRenderers == null)
		{
			_meshRenderers = (from mr in GetComponentsInChildren<MeshRenderer>()
				where mr.enabled
				select mr).ToArray();
		}
		MeshRenderer[] meshRenderers = _meshRenderers;
		for (int num = 0; num < meshRenderers.Length; num++)
		{
			meshRenderers[num].enabled = visible;
		}
	}
}
