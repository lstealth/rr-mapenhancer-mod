using System.Collections;
using Helpers;
using Network.Messages;
using UnityEngine;

namespace Network.Client;

public class TimeSynchronizer : MonoBehaviour
{
	public GameClient Client;

	private long _tickOffset;

	private long _timeSyncSentAt;

	private const int NumPings = 3;

	private CircularBuffer<long> _offsets = new CircularBuffer<long>(3);

	public long Tick => NetworkTime.systemTick + _tickOffset;

	public void Synchronize()
	{
		StartCoroutine(SynchronizeLoop());
	}

	private IEnumerator SynchronizeLoop()
	{
		while (Client.IsConnectedOrConnecting)
		{
			_offsets.Clear();
			Send();
			yield return new WaitForSeconds(10f);
		}
	}

	private void Send()
	{
		_timeSyncSentAt = NetworkTime.systemTick;
		Client.SendNetworkMessage(new TimeSync(0L), Channel.Message);
	}

	public void DidReceiveServerTick(long serverTick)
	{
		long toAdd = (NetworkTime.systemTick - _timeSyncSentAt) / 2;
		_offsets.Enqueue(toAdd);
		long num = 0L;
		for (int i = 0; i < _offsets.Length; i++)
		{
			num += _offsets.Peek(i);
		}
		long num2 = num / _offsets.Length;
		_tickOffset = serverTick + num2 - NetworkTime.systemTick;
		if (!_offsets.IsFull)
		{
			Send();
		}
	}
}
