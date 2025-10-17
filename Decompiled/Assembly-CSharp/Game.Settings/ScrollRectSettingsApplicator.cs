using UnityEngine;
using UnityEngine.UI;

namespace Game.Settings;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectSettingsApplicator : MonoBehaviour
{
	private void Start()
	{
		if (base.gameObject.TryGetComponent<ScrollRect>(out var component))
		{
			ScrollRect scrollRect = component;
			scrollRect.scrollSensitivity = Application.platform switch
			{
				RuntimePlatform.WindowsPlayer => 20, 
				RuntimePlatform.WindowsEditor => 20, 
				RuntimePlatform.OSXPlayer => 2, 
				RuntimePlatform.OSXEditor => 2, 
				_ => 1, 
			};
		}
	}
}
