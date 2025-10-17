using System;
using TMPro;
using UnityEngine;

namespace Track.Signals;

[RequireComponent(typeof(TMP_Text))]
public class CTCPanelSignalLabel : MonoBehaviour
{
	public enum Direction
	{
		Left,
		Right
	}

	public string number;

	public Direction direction;

	[Range(1f, 3f)]
	public int heads = 1;

	private TMP_Text _text;

	private void OnValidate()
	{
		if (_text == null)
		{
			_text = GetComponent<TMP_Text>();
		}
		string text = heads switch
		{
			1 => "SigSingle", 
			2 => "SigDouble", 
			3 => "SigTriple", 
			_ => throw new ArgumentException(), 
		};
		string text2 = direction switch
		{
			Direction.Left => "<sprite name=\"" + text + "\" tint=1> " + number + "L", 
			Direction.Right => number + "R <rotate=-180><sprite name=\"" + text + "\" tint=1></rotate>", 
			_ => throw new ArgumentException(), 
		};
		_text.text = text2;
		base.name = "Signal Label " + number + ((direction == Direction.Left) ? "L" : "R");
	}
}
