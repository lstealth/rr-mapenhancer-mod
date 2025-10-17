using System.Runtime.InteropServices;
using MessagePack;

namespace Game.Messages;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[MessagePackObject(false)]
public struct NullPropertyValue : IPropertyValue
{
	public override string ToString()
	{
		return "null";
	}
}
