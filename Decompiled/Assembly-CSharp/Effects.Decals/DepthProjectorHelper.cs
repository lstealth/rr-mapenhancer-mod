using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Effects.Decals;

[RequireComponent(typeof(DecalProjector))]
public class DepthProjectorHelper : MonoBehaviour
{
	private DecalProjector _decalProjector;

	private void Awake()
	{
		_decalProjector = GetComponent<DecalProjector>();
		_decalProjector.material = new Material(_decalProjector.material);
	}

	private void OnDestroy()
	{
		Object.Destroy(_decalProjector.material);
	}

	private void OnEnable()
	{
		Messenger.Default.Register<WorldDidMoveEvent>(this, WorldDidMove);
		UpdatePosition();
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void UpdatePosition()
	{
		Material material = _decalProjector.material;
		material.SetFloat("_DecalProjectorOriginY", base.transform.position.y);
		material.SetFloat("_DecalProjectorDepth", _decalProjector.size.z);
	}

	private void WorldDidMove(WorldDidMoveEvent obj)
	{
		UpdatePosition();
	}
}
