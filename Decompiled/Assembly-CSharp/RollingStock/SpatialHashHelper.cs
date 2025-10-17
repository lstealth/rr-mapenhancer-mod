using System.Collections.Generic;
using Core;
using UnityEngine;

namespace RollingStock;

public class SpatialHashHelper
{
	private SpatialHashLinear _spatialHash;

	private readonly List<SpatialHashLinear.Entity> _entities;

	private readonly int _gridSize;

	private bool _needsUpdate;

	private readonly Dictionary<string, int> _idToIndex = new Dictionary<string, int>();

	private float _maxRadius;

	public SpatialHashHelper(int gridSize, int initCapacity)
	{
		_gridSize = gridSize;
		_spatialHash = new SpatialHashLinear(gridSize, initCapacity);
		_entities = new List<SpatialHashLinear.Entity>();
	}

	private void Update()
	{
		if (_entities.Count > _spatialHash.Capacity)
		{
			Reserve(_entities.Count + 16);
		}
		_spatialHash.Update(_entities);
		_needsUpdate = false;
	}

	public void AddUpdateEntity(string id, Vector3 position, float radius)
	{
		_needsUpdate = true;
		_maxRadius = Mathf.Max(_maxRadius, radius);
		if (!_idToIndex.TryGetValue(id, out var value))
		{
			value = _entities.Count;
			_idToIndex[id] = value;
			_entities.Add(new SpatialHashLinear.Entity(id, position));
		}
		else
		{
			SpatialHashLinear.Entity value2 = _entities[value];
			value2.Position = position;
			_entities[value] = value2;
		}
	}

	public void Query(Vector3 point, float radius, HashSet<string> result)
	{
		UpdateIfNeeded();
		_spatialHash.Query(point, radius + _maxRadius, result);
	}

	public void UpdateIfNeeded()
	{
		if (_needsUpdate)
		{
			Update();
		}
	}

	public void Remove(string id)
	{
		if (_idToIndex.TryGetValue(id, out var value))
		{
			_entities.RemoveAt(value);
			_needsUpdate = true;
			_idToIndex.Clear();
			for (int i = 0; i < _entities.Count; i++)
			{
				_idToIndex[_entities[i].Id] = i;
			}
		}
	}

	public void Reserve(int capacity)
	{
		if (_spatialHash.Capacity < capacity)
		{
			_spatialHash = new SpatialHashLinear(_gridSize, capacity);
		}
	}
}
