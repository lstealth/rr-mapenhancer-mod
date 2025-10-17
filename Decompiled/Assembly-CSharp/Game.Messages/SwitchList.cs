using System.Collections.Generic;
using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct SwitchList : IDocumentContent
{
	[MessagePackObject(false)]
	public struct Entry
	{
		[Key(0)]
		public string CarId;

		public Entry(string carId)
		{
			CarId = carId;
		}
	}

	[Key(0)]
	public List<Entry> Entries;

	public SwitchList(List<Entry> entries)
	{
		Entries = entries;
	}
}
