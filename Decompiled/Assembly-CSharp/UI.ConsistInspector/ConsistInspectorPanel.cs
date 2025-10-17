using TMPro;
using UnityEngine;

namespace UI.ConsistInspector;

public class ConsistInspectorPanel : MonoBehaviour
{
	[SerializeField]
	private RectTransform panel;

	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private ConsistInspectorCell cellPrototype;

	[SerializeField]
	private RectTransform scrollContent;

	public static ConsistInspectorPanel Shared { get; private set; }

	private void Awake()
	{
		Shared = this;
		cellPrototype.gameObject.SetActive(value: false);
		Hide();
	}

	private void OnEnable()
	{
	}

	public void Present()
	{
		panel.gameObject.SetActive(value: true);
		Rebuild();
	}

	public void Hide()
	{
		panel.gameObject.SetActive(value: false);
	}

	private void Rebuild()
	{
		RemoveAllCells();
		titleLabel.text = "Not implemented";
	}

	private void RemoveAllCells()
	{
		for (int num = scrollContent.transform.childCount - 1; num >= 0; num--)
		{
			Transform child = scrollContent.transform.GetChild(num);
			if ((object)child.gameObject != cellPrototype.gameObject)
			{
				Object.DestroyImmediate(child.gameObject);
			}
		}
	}

	private ConsistInspectorCell InstantiateCell()
	{
		ConsistInspectorCell consistInspectorCell = Object.Instantiate(cellPrototype, scrollContent);
		consistInspectorCell.gameObject.SetActive(value: true);
		return consistInspectorCell;
	}

	public void OnReloadPressed()
	{
		Rebuild();
	}
}
