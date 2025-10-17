using System.Collections.Generic;
using System.Text;
using Game;
using Game.Messages;
using TMPro;
using UnityEngine;

namespace Character;

public class CharacterPositionDebugger : MonoBehaviour
{
	[SerializeField]
	private TMP_Text text;

	private StringBuilder _stringBuilder = new StringBuilder();

	private UpdateCharacterPosition _local;

	private Dictionary<string, UpdateCharacterPosition> _remote = new Dictionary<string, UpdateCharacterPosition>();

	public static CharacterPositionDebugger Shared { get; private set; }

	private void Awake()
	{
		Shared = this;
	}

	private void OnEnable()
	{
		text.text = "";
	}

	private void UpdateText()
	{
		_stringBuilder.Clear();
		Add("Local", _local);
		foreach (var (key, update) in _remote)
		{
			Add(key, update);
		}
		this.text.text = _stringBuilder.ToString();
		void Add(string arg, UpdateCharacterPosition updateCharacterPosition2)
		{
			_stringBuilder.AppendLine($"{arg} {updateCharacterPosition2.Tick}::");
			_stringBuilder.AppendLine(string.Format("{0} {1:F1}, {2:F1}, {3}", updateCharacterPosition2.Position.RelativeToCarId ?? "<none>", updateCharacterPosition2.Position.Position, updateCharacterPosition2.Velocity, updateCharacterPosition2.Pose));
			_stringBuilder.AppendLine();
		}
	}

	public void DidSend(UpdateCharacterPosition update)
	{
		_local = update;
		UpdateText();
	}

	public void DidReceive(UpdateCharacterPosition message, IPlayer sender)
	{
		_remote[sender.PlayerId.String] = message;
		UpdateText();
	}
}
