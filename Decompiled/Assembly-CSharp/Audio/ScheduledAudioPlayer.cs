using System;
using System.Collections;
using System.Collections.Generic;
using Game.Messages;
using Game.State;
using Helpers;
using Network;
using Serilog;
using UnityEngine;

namespace Audio;

public class ScheduledAudioPlayer : MonoBehaviour
{
	public AudioLibrary audioLibrary;

	private readonly Dictionary<string, Coroutine> _playingNotificationSounds = new Dictionary<string, Coroutine>();

	private static long Now => StateManager.Now;

	public static void HostPlaySoundAtPosition(string soundName, Vector3 gamePosition, AudioDistance distance, AudioController.Group group, int priority, float volume = 1f, float pitch = 1f)
	{
		StateManager.ApplyLocal(new PlaySoundAtPosition(Now, soundName, gamePosition, volume, pitch, (int)distance, group.Path, priority));
	}

	public void HandlePlaySound(PlaySoundAtPosition play)
	{
		long toTick = play.Tick + (Multiplayer.IsHost ? 0 : 300);
		float delaySeconds = Mathf.Clamp(NetworkTime.Elapsed(Now, toTick), 0f, 5f);
		StartCoroutine(PlaySoundAtPosition(delaySeconds, play));
	}

	private IEnumerator PlaySoundAtPosition(float delaySeconds, PlaySoundAtPosition play)
	{
		if (!audioLibrary.TryGetEntry(play.Name, out var entry))
		{
			throw new Exception("No such sound in library: " + play.Name);
		}
		if (delaySeconds > 0f)
		{
			yield return new WaitForSeconds(delaySeconds);
		}
		Transform transform = TrainController.Shared.transform;
		Vector3 offset = transform.InverseTransformPoint(WorldTransformer.GameToWorld(play.Position));
		AudioController.Group mixerGroup = new AudioController.Group(play.GroupPath);
		int priority = play.Priority;
		AudioDistance distance = (AudioDistance)play.Distance;
		IAudioSource audioSource = VirtualAudioSourcePool.Checkout(entry.name, entry.clip, loop: false, mixerGroup, priority, transform, distance, offset);
		audioSource.volume = play.Volume * entry.volumeMultiplier;
		audioSource.pitch = play.Pitch;
		audioSource.spatialBlend = 1f;
		if (distance == AudioDistance.HyperLocal)
		{
			audioSource.minDistance = 0.5f;
			audioSource.maxDistance = 3f;
		}
		audioSource.Play();
		VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
	}

	public static void HostPlaySoundNotification(string soundName, float volume = 1f, float pitch = 1f)
	{
		StateManager.ApplyLocal(new PlaySoundNotification(soundName, volume, pitch));
	}

	public static void PlaySoundLocal(string soundName, float volume = 1f, float pitch = 1f)
	{
		try
		{
			PlaySoundNotification play = new PlaySoundNotification(soundName, volume, pitch);
			StateManager.Shared.AudioPlayer.HandlePlaySound(play);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception playing sound {soundName} locally", soundName);
		}
	}

	public void HandlePlaySound(PlaySoundNotification play)
	{
		if (_playingNotificationSounds.TryGetValue(play.Name, out var _))
		{
			Log.Debug("PlaySound: Already playing {sound}", play.Name);
			return;
		}
		Log.Information("PlaySound: Play {sound}", play.Name);
		_playingNotificationSounds[play.Name] = StartCoroutine(PlaySoundCoroutine(play));
	}

	private IEnumerator PlaySoundCoroutine(PlaySoundNotification play)
	{
		string key = play.Name;
		float length;
		try
		{
			if (!audioLibrary.TryGetEntry(play.Name, out var entry))
			{
				throw new Exception("No such sound in library: " + play.Name);
			}
			Camera main = Camera.main;
			if (main == null)
			{
				throw new Exception("No camera");
			}
			Transform transform = TrainController.Shared.transform;
			Vector3 offset = transform.InverseTransformPoint(main.transform.position);
			AudioController.Group playerAction = AudioController.Group.PlayerAction;
			int priority = 5;
			AudioDistance cullDistance = AudioDistance.Local;
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout(entry.name, entry.clip, loop: false, playerAction, priority, transform, cullDistance, offset);
			audioSource.volume = play.Volume * entry.volumeMultiplier;
			audioSource.pitch = play.Pitch;
			audioSource.spatialBlend = 0f;
			audioSource.Play();
			length = entry.clip.length;
			VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "PlaySound: Exception playing sound");
			_playingNotificationSounds[key] = null;
			yield break;
		}
		yield return new WaitForSecondsRealtime(Mathf.Max(0f, length - 0.1f));
		_playingNotificationSounds.Remove(key);
	}
}
