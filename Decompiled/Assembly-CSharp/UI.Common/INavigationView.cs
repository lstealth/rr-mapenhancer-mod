using UnityEngine;

namespace UI.Common;

public interface INavigationView
{
	RectTransform RectTransform { get; }

	void WillAppear();

	void WillDisappear();

	void DidPop();
}
