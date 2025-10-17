using CorgiSpline;
using UnityEngine;

namespace Track;

[CreateAssetMenu(fileName = "TrackMeshProfile", menuName = "Railroader/Track Mesh Profile", order = 0)]
public class TrackMeshProfile : ScriptableObject
{
	public SwitchStand switchStandPrefab;

	public SwitchStand switchStandPrefabCTC;

	public GameObject bumperPrefab;

	public Material trackMaterial;

	public Mesh trackProfileMesh;

	[Header("Tunnel Prefabs")]
	public GameObject tunnelPortalPrefab;

	public SplineMeshBuilder_RepeatingMesh tunnelLinerPrefab;
}
