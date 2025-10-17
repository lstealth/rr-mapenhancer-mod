using System;
using KeyValue.Runtime;
using UnityEngine;

namespace Avatar;

public class AvatarCustomization : MonoBehaviour
{
	[Serializable]
	public struct AvatarSet
	{
		public GameObject gameObject;

		public Renderer[] skinRenderers;

		public Material[] skinMaterials;

		public AccessoryReference[] accessories;
	}

	[Serializable]
	public struct AccessoryReference
	{
		public string identifier;

		public AccessoryOption[] options;
	}

	[Serializable]
	public struct AccessoryOption
	{
		public string identifier;

		public GameObject[] gameObjects;
	}

	[SerializeField]
	private AvatarSet maleAvatar;

	[SerializeField]
	private AvatarSet femaleAvatar;

	public GameObject AvatarGameObject { get; private set; }

	public AvatarAnimator Animator { get; private set; }

	public void Configure(AvatarDescriptor descriptor)
	{
		GetAvatarSet(Other(descriptor.Gender)).gameObject.SetActive(value: false);
		AvatarSet avatarSet = GetAvatarSet(descriptor.Gender);
		AvatarGameObject = avatarSet.gameObject;
		AvatarGameObject.SetActive(value: true);
		Animator = AvatarGameObject.GetComponentInChildren<AvatarAnimator>();
		Renderer[] skinRenderers = avatarSet.skinRenderers;
		for (int i = 0; i < skinRenderers.Length; i++)
		{
			skinRenderers[i].sharedMaterial = avatarSet.skinMaterials[descriptor.SkinToneIndex];
		}
		AccessoryReference[] accessories = avatarSet.accessories;
		for (int i = 0; i < accessories.Length; i++)
		{
			AccessoryReference accessoryReference = accessories[i];
			Value value = descriptor.SelectedOptionForAccessory(accessoryReference.identifier);
			AccessoryOption[] options = accessoryReference.options;
			for (int j = 0; j < options.Length; j++)
			{
				AccessoryOption accessoryOption = options[j];
				bool active = value.StringValue == accessoryOption.identifier;
				GameObject[] gameObjects = accessoryOption.gameObjects;
				for (int k = 0; k < gameObjects.Length; k++)
				{
					gameObjects[k].SetActive(active);
				}
			}
		}
	}

	private AvatarSet GetAvatarSet(Gender gender)
	{
		return gender switch
		{
			Gender.Male => maleAvatar, 
			Gender.Female => femaleAvatar, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static Gender Other(Gender gender)
	{
		return gender switch
		{
			Gender.Male => Gender.Female, 
			Gender.Female => Gender.Male, 
			_ => throw new ArgumentOutOfRangeException("gender", gender, null), 
		};
	}
}
