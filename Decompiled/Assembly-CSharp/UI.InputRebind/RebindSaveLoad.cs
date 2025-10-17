using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.InputRebind;

public class RebindSaveLoad : MonoBehaviour
{
	public InputActionAsset actions;

	public void OnEnable()
	{
		string text = PlayerPrefs.GetString("rebinds");
		if (!string.IsNullOrEmpty(text))
		{
			actions.LoadBindingOverridesFromJson(text);
		}
	}

	public void OnDisable()
	{
		string value = actions.SaveBindingOverridesAsJson();
		PlayerPrefs.SetString("rebinds", value);
	}
}
