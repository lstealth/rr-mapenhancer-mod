using System.Collections.Generic;
using System.Linq;
using Avatar;
using Game;
using KeyValue.Runtime;
using Network;
using Network.Client;
using UI.Builder;

namespace UI.PreferencesWindow;

public class CharacterSettingsBuilder
{
	private struct Option
	{
		public string Identifier { get; set; }

		public string Name { get; set; }

		public Option(string identifier, string name)
		{
			Identifier = identifier;
			Name = name;
		}
	}

	private static readonly List<Option> Hats = new List<Option>
	{
		new Option("", "No Hat"),
		new Option("kromer", "Kromer"),
		new Option("thompson", "Thompson")
	};

	private static readonly List<Option> Glasses = new List<Option>
	{
		new Option("", "No Glasses"),
		new Option("specs", "Glasses")
	};

	private static readonly List<Option> Bandana = new List<Option>
	{
		new Option("", "No Bandana"),
		new Option("red", "Red")
	};

	private static readonly List<Option> Gloves = new List<Option>
	{
		new Option("", "No Gloves"),
		new Option("fireman", "Gloves")
	};

	private static AvatarDescriptor _avatarDescriptor;

	public static void BuildCharacterPanel(UIPanelBuilder builder)
	{
		_avatarDescriptor = Preferences.AvatarDescriptor;
		if (Multiplayer.Client == null)
		{
			builder.AddField("Name", builder.AddInputField(Preferences.MultiplayerClientUsername, delegate(string newName)
			{
				Preferences.MultiplayerClientUsername = newName;
			}, "Character Name"));
		}
		else
		{
			builder.AddField("Name", Preferences.MultiplayerClientUsername);
		}
		AddDropdownFieldGender(builder);
		AddDropdownFieldSkinTone(builder);
		AddAccessoryDropdownField(builder, "Hat", "hat", Hats);
		AddAccessoryDropdownField(builder, "Glasses", "glasses", Glasses);
		AddAccessoryDropdownField(builder, "Bandana", "bandana", Bandana);
		AddAccessoryDropdownField(builder, "Gloves", "gloves", Gloves);
		builder.AddExpandingVerticalSpacer();
	}

	private static void AddDropdownFieldGender(UIPanelBuilder builder)
	{
		builder.AddField("Model", builder.AddDropdown(new List<string> { "Male", "Female" }, (_avatarDescriptor.Gender != Gender.Male) ? 1 : 0, delegate(int index)
		{
			_avatarDescriptor = new AvatarDescriptor((index != 0) ? Gender.Female : Gender.Male, _avatarDescriptor.SkinToneIndex, _avatarDescriptor.Accessories);
			Propagate();
		}));
	}

	private static void AddDropdownFieldSkinTone(UIPanelBuilder builder)
	{
		builder.AddField("Skin Tone", builder.AddColorDropdown(new List<string> { "#d4c3b0", "#3d260c" }, _avatarDescriptor.SkinToneIndex, delegate(int index)
		{
			_avatarDescriptor = new AvatarDescriptor(_avatarDescriptor.Gender, index, _avatarDescriptor.Accessories);
			Propagate();
		}));
	}

	private static void AddAccessoryDropdownField(UIPanelBuilder builder, string labelText, string accessoryIdentifier, List<Option> options)
	{
		string selectedOption = _avatarDescriptor.SelectedOptionForAccessory(accessoryIdentifier).StringValue;
		builder.AddField(labelText, builder.AddDropdown(options.Select((Option option) => option.Name).ToList(), options.FindIndex((Option o) => o.Identifier == selectedOption), delegate(int index)
		{
			SetAccessoryOption(accessoryIdentifier, options[index].Identifier);
		}));
	}

	private static void SetAccessoryOption(string accessoryIdentifier, string option)
	{
		_avatarDescriptor = _avatarDescriptor.SettingAccessory(accessoryIdentifier, string.IsNullOrEmpty(option) ? Value.Null() : Value.String(option));
		Propagate();
	}

	private static void Propagate()
	{
		Preferences.AvatarDescriptor = _avatarDescriptor;
		CameraSelector shared = CameraSelector.shared;
		if (!(shared == null))
		{
			shared.localAvatar.SetAvatarCustomization(_avatarDescriptor);
			ClientManager client = Multiplayer.Client;
			if (client != null)
			{
				client.SendCharacter();
			}
		}
	}
}
