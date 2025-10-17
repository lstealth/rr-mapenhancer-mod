using System.Collections.Generic;
using KeyValue.Runtime;
using UnityEngine;

namespace Avatar;

public class AvatarTester : MonoBehaviour
{
	public AvatarCustomization avatar;

	public Transform lookAt;

	public Gender gender;

	public int skinToneIndex;

	public string hat = "kromer";

	public string bandana = "red";

	public string glasses = "specs";

	private void OnValidate()
	{
		ResetAvatar();
	}

	private void OnAnimatorIK(int layerIndex)
	{
	}

	private void ResetAvatar()
	{
		AvatarDescriptor descriptor = new AvatarDescriptor(gender, skinToneIndex, new Dictionary<string, Value>
		{
			{
				"hat",
				Value.String(hat)
			},
			{
				"bandana",
				Value.String(bandana)
			},
			{
				"glasses",
				Value.String(glasses)
			}
		});
		avatar.Configure(descriptor);
	}
}
