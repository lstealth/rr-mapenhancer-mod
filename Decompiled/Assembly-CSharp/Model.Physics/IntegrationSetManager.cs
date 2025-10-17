using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Messages;
using Game.State;
using Network;
using Network.Client;
using Serilog;
using Track;
using UnityEngine;
using UnityEngine.Pool;

namespace Model.Physics;

public class IntegrationSetManager : IEnumerable<IntegrationSet>, IEnumerable
{
	public Func<uint, IReadOnlyCollection<Car>, IntegrationSet> CreateIntegrationSet;

	private readonly Dictionary<uint, IntegrationSet> _integrationSets = new Dictionary<uint, IntegrationSet>();

	private uint _idCursor;

	private readonly List<uint> _integrationSetsToRemove = new List<uint>(8);

	private readonly HashSet<uint> _deltaAdded = new HashSet<uint>();

	private readonly HashSet<uint> _deltaRemoved = new HashSet<uint>();

	private readonly HashSet<uint> _deltaChanged = new HashSet<uint>();

	private HashSet<IntegrationSet> _needsBatchPositionUpdate = new HashSet<IntegrationSet>();

	public IEnumerator<IntegrationSet> GetEnumerator()
	{
		return _integrationSets.Values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _integrationSets.Values.GetEnumerator();
	}

	public void SendDelta()
	{
		IDisposable disposable = StateManager.TransactionScope();
		try
		{
			_needsBatchPositionUpdate.Clear();
			CleanupDeltas();
			foreach (uint item in _deltaRemoved)
			{
				Send(new CarSetRemove(item));
			}
			foreach (uint item2 in _deltaAdded)
			{
				IntegrationSet integrationSet = _integrationSets[item2];
				Send(new CarSetAdd(integrationSet.Snapshot()));
				_needsBatchPositionUpdate.Add(integrationSet);
			}
			foreach (uint item3 in _deltaChanged)
			{
				IntegrationSet integrationSet2 = _integrationSets[item3];
				Send(new CarSetChangeCars(integrationSet2.Snapshot()));
				_needsBatchPositionUpdate.Add(integrationSet2);
			}
			if (!(Multiplayer.Client != null))
			{
				return;
			}
			ClientManager client = Multiplayer.Client;
			foreach (IntegrationSet item4 in _needsBatchPositionUpdate)
			{
				item4.SendBatchCarPositionUpdate(client, critical: true);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception in SendDelta");
			Debug.LogException(exception);
			throw;
		}
		finally
		{
			ClearDeltas();
			_needsBatchPositionUpdate.Clear();
			disposable?.Dispose();
		}
	}

	private void CleanupDeltas()
	{
		HashSet<uint> hashSet = CollectionPool<HashSet<uint>, uint>.Get();
		try
		{
			foreach (uint item in _deltaRemoved)
			{
				_deltaChanged.Remove(item);
				if (_deltaAdded.Remove(item))
				{
					hashSet.Add(item);
				}
			}
			foreach (uint item2 in hashSet)
			{
				_deltaRemoved.Remove(item2);
			}
		}
		finally
		{
			CollectionPool<HashSet<uint>, uint>.Release(hashSet);
		}
	}

	private void RecordDeltaAdd(IntegrationSet set)
	{
		_deltaAdded.Add(set.Id);
	}

	private void RecordDeltaRemove(IntegrationSet set)
	{
		uint id = set.Id;
		_deltaRemoved.Add(id);
	}

	private void RecordDeltaChange(IntegrationSet set)
	{
		if (!_deltaAdded.Contains(set.Id))
		{
			_deltaChanged.Add(set.Id);
		}
	}

	private static void Send(IGameMessage message)
	{
		if (Multiplayer.Client != null)
		{
			Multiplayer.Client.Send(message);
		}
	}

	public uint GenerateId()
	{
		while (_integrationSets.ContainsKey(_idCursor))
		{
			_idCursor++;
		}
		return _idCursor++;
	}

	public void RemoveEmpty()
	{
		_integrationSetsToRemove.Clear();
		foreach (var (item, integrationSet2) in _integrationSets)
		{
			if (integrationSet2.IsEmpty)
			{
				_integrationSetsToRemove.Add(item);
			}
		}
		foreach (uint item2 in _integrationSetsToRemove)
		{
			Remove(_integrationSets[item2]);
		}
	}

	public void Add(IntegrationSet set)
	{
		_integrationSets[set.Id] = set;
		RecordDeltaAdd(set);
	}

	public void Remove(IntegrationSet set)
	{
		_integrationSets.Remove(set.Id);
		RecordDeltaRemove(set);
	}

	public void Split(Car car1, Car car2)
	{
		try
		{
			IntegrationSet set = car1.set;
			set.Split(car1, car2, out var newSet);
			_integrationSets[newSet.Id] = newSet;
			RecordDeltaChange(set);
			RecordDeltaAdd(newSet);
		}
		catch (Exception propertyValue)
		{
			Log.Error("Error splitting set: {car1} {car2} {exception}", car1, car2, propertyValue);
		}
	}

	public void Union(Car car1, Car car2)
	{
		try
		{
			if (car1.set == null && car2.set == null)
			{
				CreateIntegrationSet(GenerateId(), (IReadOnlyCollection<Car>)(object)new Car[2] { car1, car2 });
			}
			else if (car1.set != null && car2.set == null)
			{
				car1.set.AddCar(car2);
				RecordDeltaChange(car1.set);
			}
			else if (car1.set == null && car2.set != null)
			{
				car2.set.AddCar(car1);
				RecordDeltaChange(car2.set);
			}
			else if (car1.set != car2.set)
			{
				_integrationSets.Remove(car2.set.Id);
				RecordDeltaRemove(car2.set);
				car1.set.Union(car2.set);
				RecordDeltaChange(car1.set);
			}
		}
		catch (Exception propertyValue)
		{
			Log.Error("Error unioning sets: {car1} {car2} {exception}", car1, car2, propertyValue);
		}
	}

	public void Clear()
	{
		_integrationSets.Clear();
		_idCursor = 0u;
	}

	public void RemoveCar(Car car)
	{
		IntegrationSet set = car.set;
		set.RemoveCar(car, out var newSet);
		RecordDeltaChange(set);
		if (set.IsEmpty)
		{
			Remove(set);
		}
		if (newSet != null && !newSet.IsEmpty)
		{
			_integrationSets[newSet.Id] = newSet;
			RecordDeltaAdd(newSet);
		}
	}

	public void HandleBatchCarPositionUpdate(BatchCarPositionUpdate update, Graph graph)
	{
		if (!_integrationSets.TryGetValue(update.Id, out var value))
		{
			Log.Error("Received BatchCarPositionUpdate for unknown id: {id}", update.Id);
			return;
		}
		try
		{
			Location wheelBoundsA = graph.MakeLocation(update.StartLocation);
			float[] positions = update.Positions;
			float[] velocities = update.Velocities.Select(Mathf.HalfToFloat).ToArray();
			value.HandleCarPositionUpdate(wheelBoundsA, positions, velocities, update.Tick);
		}
		catch (Exception ex)
		{
			Log.Error("BatchPositionUpdate: error handling: {SetId}, {@Exception}", update.Id, ex);
			Debug.LogException(ex);
		}
	}

	public void AddWithoutDelta(Snapshot.CarSet snapshot, Dictionary<string, Car> carLookup)
	{
		IntegrationSet integrationSet = CreateIntegrationSet(snapshot.Id, snapshot.CarIds.Select((string carId) => carLookup[carId]).ToList());
		integrationSet.SetPositions(snapshot.Positions, snapshot.FrontIsAs, immediate: true);
		_integrationSets[integrationSet.Id] = integrationSet;
	}

	public void RemoveWithoutDelta(uint setId)
	{
		_integrationSets.Remove(setId);
	}

	public void ChangeCarsWithoutDelta(Snapshot.CarSet snapshot, Dictionary<string, Car> carLookup)
	{
		List<Car> list = snapshot.CarIds.Select((string carId) => carLookup[carId]).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			Car car = list[num];
			car.FrontIsA = snapshot.FrontIsAs[num];
			if (car.set != null)
			{
				car.set.RemoveCarInternal(car);
			}
		}
		IntegrationSet integrationSet = CreateIntegrationSet(snapshot.Id, list);
		integrationSet.SetPositions(snapshot.Positions, snapshot.FrontIsAs, immediate: false);
		_integrationSets[integrationSet.Id] = integrationSet;
	}

	public void LogState()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (var (num2, integrationSet2) in _integrationSets.OrderBy((KeyValuePair<uint, IntegrationSet> kv) => kv.Key))
		{
			stringBuilder.AppendLine(string.Format("{0:D4}: {1}", num2, string.Join(", ", integrationSet2.Cars.Select((Car car) => car.ToString()))));
		}
		Debug.Log($"{_integrationSets.Count} CarSets:\n{stringBuilder}");
	}

	public void ClearDeltas()
	{
		_deltaAdded.Clear();
		_deltaRemoved.Clear();
		_deltaChanged.Clear();
	}
}
