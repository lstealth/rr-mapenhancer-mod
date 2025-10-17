using System.Collections.Generic;
using Game.Messages;
using KeyValue.Runtime;

namespace Avatar;

public readonly struct AvatarDescriptor
{
	public readonly Gender Gender;

	public readonly int SkinToneIndex;

	public readonly Dictionary<string, Value> Accessories;

	public static AvatarDescriptor Default => new AvatarDescriptor(Gender.Male, 0, new Dictionary<string, Value>
	{
		{
			"hat",
			Value.String("kromer")
		},
		{
			"bandana",
			Value.String("red")
		}
	});

	public AvatarDescriptor(Gender gender, int skinToneIndex, Dictionary<string, Value> accessories)
	{
		Gender = gender;
		SkinToneIndex = skinToneIndex;
		Accessories = accessories;
	}

	public static AvatarDescriptor From(Value value)
	{
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		return new AvatarDescriptor((!(dictionaryValue["gender"].StringValue == "m")) ? Gender.Female : Gender.Male, dictionaryValue["skinTone"].IntValue, new Dictionary<string, Value>(dictionaryValue["accessories"].DictionaryValue));
	}

	public Value ToValue()
	{
		return Value.Dictionary(new Dictionary<string, Value>
		{
			["gender"] = Value.String((Gender == Gender.Male) ? "m" : "f"),
			["skinTone"] = Value.Int(SkinToneIndex),
			["accessories"] = Value.Dictionary(Accessories)
		});
	}

	public Value SelectedOptionForAccessory(string accessoryIdentifier)
	{
		foreach (var (text2, result) in Accessories)
		{
			if (!(text2 != accessoryIdentifier))
			{
				return result;
			}
		}
		return Value.Null();
	}

	public static AvatarDescriptor From(Snapshot.CharacterCustomization customization)
	{
		return From(Value.Dictionary(PropertyValueConverter.SnapshotToRuntime(customization.Data)));
	}

	public Snapshot.CharacterCustomization ToCharacterCustomization()
	{
		return new Snapshot.CharacterCustomization(PropertyValueConverter.RuntimeToSnapshot(ToValue().DictionaryValue));
	}

	public AvatarDescriptor SettingAccessory(string accessoryIdentifier, Value accessoryOption)
	{
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>(Accessories);
		dictionary[accessoryIdentifier] = accessoryOption;
		return new AvatarDescriptor(Gender, SkinToneIndex, dictionary);
	}
}
