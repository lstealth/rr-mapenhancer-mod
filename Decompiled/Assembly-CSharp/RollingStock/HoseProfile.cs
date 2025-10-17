using System.Collections.Generic;
using UnityEngine;

namespace RollingStock;

[CreateAssetMenu(fileName = "Hose", menuName = "Train Game/Hose Profile", order = 0)]
public class HoseProfile : ScriptableObject
{
	public AnimationCurve lengthCurve = AnimationCurve.Constant(0f, 1f, 0.52f);

	public float dampingAtPop = 1f;

	public float dampingAtRest = 0.9f;

	public float dampingRestSpeed = 1f;

	public float gravity = 200f;

	public float angleStart = 45f;

	public float angleEnd = 45f;

	[Header("Gladhand")]
	public GameObject gladhandPrefab;

	public Vector3 gladhandOffset = new Vector3(-0.026f, 0f, 0.093f);

	[Header("Spline")]
	[Range(0.0001f, 0.1f)]
	public float maxMagnitudeDelta = 0.1f;

	[Range(0f, 10f)]
	public float maxDegreesDelta = 0.1f;

	[Range(0f, 1f)]
	public float maxDegreesMove = 0.1f;

	public float propulsion;

	public float propulsionDecay = 0.9f;

	[Header("Audio")]
	[Tooltip("Played when disconnecting, volume modulated by pressure.")]
	public List<AudioClip> popClips;

	[Tooltip("Played when disconnecting, volume static. Should represent just the sound of the gladhand coming apart.")]
	public List<AudioClip> disconnectClips;

	[Tooltip("Played when connecting gladhands, volume static. Should represent just the sound of the gladhand connecting.")]
	public List<AudioClip> connectClips;
}
