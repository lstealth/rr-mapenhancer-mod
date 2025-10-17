using System.Runtime.InteropServices;

namespace Network;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct StartSingleplayerSetup : INetworkSetup
{
}
