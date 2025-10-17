using System.Runtime.InteropServices;
using MessagePack;

namespace Network.Messages;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[MessagePackObject(false)]
public struct RequestActive : INetworkMessage
{
}
