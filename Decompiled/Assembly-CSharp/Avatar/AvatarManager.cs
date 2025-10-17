using Game;
using UI.CompanyWindow;
using UnityEngine;

namespace Avatar;

public class AvatarManager : MonoBehaviour
{
	private static AvatarManager _instance;

	[SerializeField]
	private AvatarPrefab avatarPrefab;

	public static AvatarManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindObjectOfType<AvatarManager>();
			}
			return _instance;
		}
	}

	public AvatarPrefab AddAvatar(AvatarDescriptor avatarDescriptor, bool showMapIcon, PlayerId playerId, string playerName)
	{
		AvatarPrefab obj = Object.Instantiate(avatarPrefab, base.transform, worldPositionStays: false);
		obj.name = $"{playerId} {playerName}";
		obj.Customization.Configure(avatarDescriptor);
		obj.Pickable.TooltipInfo = new TooltipInfo(playerName, null);
		obj.Pickable.PlayerId = playerId;
		obj.MapIcon.gameObject.SetActive(showMapIcon);
		obj.MapIcon.OnClick = delegate
		{
			CompanyWindow.Shared.ShowPlayer(playerId.String);
		};
		return obj;
	}

	public RemoteAvatar AddRemote(PlayerId playerId, string playerName)
	{
		AvatarDescriptor avatarDescriptor = AvatarDescriptor.Default;
		AvatarPrefab avatarPrefab = AddAvatar(avatarDescriptor, showMapIcon: true, playerId, playerName);
		RemoteAvatar remoteAvatar = avatarPrefab.gameObject.AddComponent<RemoteAvatar>();
		remoteAvatar.avatar = avatarPrefab;
		remoteAvatar.name = $"{playerId} ${playerName}";
		return remoteAvatar;
	}

	public void RemoveAvatar(AvatarPrefab avatar)
	{
		Object.Destroy(avatar.gameObject);
	}

	public void RemoveAvatar(RemoteAvatar avatar)
	{
		Object.Destroy(avatar.gameObject);
	}

	public bool RemoteAvatarNear(Vector3 position)
	{
		foreach (Transform item in base.transform)
		{
			RemoteAvatar componentInChildren = item.GetComponentInChildren<RemoteAvatar>();
			if (!(componentInChildren == null) && Vector3.Distance(componentInChildren.transform.position, position) < 0.5f)
			{
				return true;
			}
		}
		return false;
	}
}
