using UnityEngine;

namespace UI.LazyScrollList;

public interface ILazyScrollListCell
{
	int ListIndex { get; }

	RectTransform RectTransform { get; }

	void Configure(int listIndex, object data);
}
