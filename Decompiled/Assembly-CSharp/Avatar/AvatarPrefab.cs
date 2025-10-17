using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using UI.Map;
using UnityEngine;

namespace Avatar;

[RequireComponent(typeof(AvatarCustomization))]
public class AvatarPrefab : MonoBehaviour
{
	public GameObject lantern;

	public AvatarCustomization Customization { get; private set; }

	public AvatarAnimator Animator => Customization.Animator;

	public AvatarPickable Pickable { get; private set; }

	public Rigidbody Rigidbody { get; private set; }

	public MapIcon MapIcon { get; private set; }

	private void Awake()
	{
		Customization = GetComponent<AvatarCustomization>();
		Pickable = GetComponentInChildren<AvatarPickable>();
		Rigidbody = base.gameObject.AddKinematicRigidbody();
		MapIcon = GetComponentInChildren<MapIcon>();
		lantern.SetActive(value: false);
	}

	private void OnEnable()
	{
		Messenger.Default.Register<WorldDidMoveEvent>(this, WorldDidMove);
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void WorldDidMove(WorldDidMoveEvent evt)
	{
		Rigidbody.position += evt.Offset;
	}

	public void SetAvatarVisible(bool show)
	{
		Customization.AvatarGameObject.SetActive(show);
		Pickable.gameObject.SetActive(show);
	}
}
