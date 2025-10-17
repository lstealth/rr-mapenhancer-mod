using TMPro;
using UnityEngine;

namespace UI.InputValidators;

[CreateAssetMenu(fileName = "InputValidator - Player Name.asset", menuName = "TextMeshPro/Input Validators/Player Name", order = 100)]
public class PlayerNameValidator : TMP_InputValidator
{
	public override char Validate(ref string text, ref int pos, char ch)
	{
		switch (ch)
		{
		case '<':
		case '>':
		case '[':
		case ']':
			return '\0';
		default:
			text += ch;
			pos++;
			return ch;
		}
	}
}
