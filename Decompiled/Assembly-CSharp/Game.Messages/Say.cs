using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Passenger)]
[MessagePackObject(false)]
public struct Say : ICharacterMessage, IGameMessage
{
	public const int CharacterLimit = 512;

	[Key(0)]
	public string characterId { get; set; }

	[Key(1)]
	public string text { get; set; }

	public Say(string characterId, string text)
	{
		this.characterId = characterId;
		this.text = text;
	}
}
