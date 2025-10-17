using TMPro;
using UnityEngine;

namespace UI.Console;

public interface IConsoleChildDelegate
{
	void Collapse();

	void Recycle(TMP_Text text);

	void HandleUserInput(string line);

	void CreateLabelIfNeeded(ConsoleLine line, Transform parent);

	void InputFieldFocusDidChange(bool focused);
}
