using TMPro;
using UnityEngine;

namespace UI.PlayerList;

[RequireComponent(typeof(RectTransform))]
public class PlayerRow : MonoBehaviour
{
	public TMP_Text nameLabel;

	public TMP_Text trailingLabel;
}
