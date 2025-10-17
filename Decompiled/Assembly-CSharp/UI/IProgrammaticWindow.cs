using UI.Common;
using UnityEngine;

namespace UI;

public interface IProgrammaticWindow : IBuilderWindow
{
	string WindowIdentifier { get; }

	Vector2Int DefaultSize { get; }

	Window.Position DefaultPosition { get; }

	Window.Sizing Sizing { get; }
}
