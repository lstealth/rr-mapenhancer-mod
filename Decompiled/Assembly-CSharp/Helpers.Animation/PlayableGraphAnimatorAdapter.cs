using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Helpers.Animation;

[RequireComponent(typeof(Animator))]
public class PlayableGraphAnimatorAdapter : MonoBehaviour
{
	private Animator animator;

	private PlayableGraph _graph;

	private AnimationLayerMixerPlayable _mixer;

	private readonly List<int> _availablePorts = new List<int>();

	private void Awake()
	{
		animator = GetComponent<Animator>();
		PrepareGraphIfNeeded();
	}

	private void PrepareGraphIfNeeded()
	{
		if (!_graph.IsValid())
		{
			_graph = PlayableGraph.Create(base.name);
			_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
			_graph.Play();
			if ((object)animator == null)
			{
				animator = GetComponent<Animator>();
			}
			AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Animator", animator);
			_mixer = AnimationLayerMixerPlayable.Create(_graph);
			output.SetSourcePlayable(_mixer);
		}
	}

	private void OnDestroy()
	{
		if (_graph.IsValid())
		{
			_graph.Destroy();
		}
	}

	private void OnValidate()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}
	}

	public int AddPlayable(AnimationClip clip, out AnimationClipPlayable playable)
	{
		PrepareGraphIfNeeded();
		playable = AnimationClipPlayable.Create(_graph, clip);
		playable.Pause();
		int num;
		if (_availablePorts.Count > 0)
		{
			num = _availablePorts[0];
			_availablePorts.RemoveAt(0);
			_mixer.ConnectInput(num, playable, 0);
		}
		else
		{
			num = _mixer.AddInput(playable, 0, 1f);
		}
		return num;
	}

	public void Remove(int port)
	{
		_mixer.DisconnectInput(port);
		_availablePorts.Add(port);
	}

	public PlayableHandle AddPlayable(AnimationClip clip)
	{
		AnimationClipPlayable playable;
		int port = AddPlayable(clip, out playable);
		return new PlayableHandle(this, port, playable);
	}
}
