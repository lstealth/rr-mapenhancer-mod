using UnityEngine;
using UnityEngine.Audio;

namespace Audio;

public class AudioController : MonoBehaviour
{
	public sealed class Group
	{
		public static readonly Group Locomotive = new Group("Locomotive");

		public static readonly Group LocomotiveBell = new Group("Locomotive/Bell");

		public static readonly Group LocomotiveCylCock = new Group("Locomotive/CylCock");

		public static readonly Group LocomotiveChuff = new Group("Locomotive/Chuff");

		public static readonly Group LocomotiveWhistle = new Group("Locomotive/Whistle");

		public static readonly Group LocomotiveDynamo = new Group("Locomotive/Dynamo");

		public static readonly Group LocomotiveCompressor = new Group("Locomotive/Compressor");

		public static readonly Group Wheels = new Group("Wheels");

		public static readonly Group WheelsClack = new Group("Wheels/Clack");

		public static readonly Group WheelsRoll = new Group("Wheels/Roll");

		public static readonly Group WheelsSqueal = new Group("Wheels/Squeal");

		public static readonly Group AirHose = new Group("Air/Hose");

		public static readonly Group AirOpen = new Group("Air/Open");

		public static readonly Group AirPop = new Group("Air/Pop");

		public static readonly Group CouplerCouple = new Group("Coupler/Couple");

		public static readonly Group CouplerOpen = new Group("Coupler/Open");

		public static readonly Group CTC = new Group("CTC");

		public static readonly Group CTCBell = new Group("CTC/Bell");

		public static readonly Group PlayerAction = new Group("Player Action");

		public readonly string Path;

		public static implicit operator AudioMixerGroup(Group g)
		{
			return Shared.mixer.Group(g);
		}

		internal Group(string path)
		{
			Path = path;
		}
	}

	private static AudioController _instance;

	public AudioMixer mixer;

	public static AudioController Shared
	{
		get
		{
			if ((bool)_instance)
			{
				return _instance;
			}
			_instance = Object.FindObjectOfType(typeof(AudioController)) as AudioController;
			if (!_instance)
			{
				Debug.LogError("There needs to be one active AudioController script on a GameObject in your scene.");
			}
			return _instance;
		}
	}
}
