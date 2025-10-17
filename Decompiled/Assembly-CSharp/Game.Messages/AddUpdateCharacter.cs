using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Passenger)]
[MessagePackObject(false)]
public struct AddUpdateCharacter : ICharacterMessage, IGameMessage
{
	[Key(0)]
	public string Name { get; set; }

	[Key(1)]
	public Snapshot.CharacterCustomization Customization { get; set; }

	public AddUpdateCharacter(string name, Snapshot.CharacterCustomization customization)
	{
		Name = name;
		Customization = customization;
	}
}
