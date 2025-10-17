using TMPro;
using UnityEngine;

namespace UI.PlayerList;

[RequireComponent(typeof(RectTransform))]
public class TrainCrewHeader : MonoBehaviour
{
	public TMP_Text nameLabel;

	public TMP_Text descriptionLabel;

	public string TrainCrewId { get; set; }
}
