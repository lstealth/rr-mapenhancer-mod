using Game.AccessControl;
using MessagePack;
using UnityEngine;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct BatchCarAirUpdate : IGameMessage
{
	[Key(0)]
	public readonly long Tick;

	[Key(1)]
	public readonly string[] CarIds;

	[Key(2)]
	public readonly byte[] BrakeLineValues;

	[Key(3)]
	public readonly byte[] BrakeReservoirValues;

	[Key(4)]
	public readonly byte[] BrakeCylinderValues;

	private const float MaxValue = 120f;

	public BatchCarAirUpdate(long tick, string[] carIds, byte[] brakeLineValues, byte[] brakeReservoirValues, byte[] brakeCylinderValues)
	{
		Tick = tick;
		CarIds = carIds;
		BrakeLineValues = brakeLineValues;
		BrakeReservoirValues = brakeReservoirValues;
		BrakeCylinderValues = brakeCylinderValues;
	}

	public static byte ValueToByte(float value)
	{
		return (byte)Mathf.RoundToInt(Mathf.Clamp01(value / 120f) * 255f);
	}

	public static float ByteToValue(byte value)
	{
		return 120f * ((float)(int)value / 255f);
	}
}
