using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GPUInstancer;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Serilog;
using UnityEngine;

namespace Track;

public class PrefabInstancer : MonoBehaviour
{
	public enum Prefab
	{
		Tie,
		TiePlate
	}

	private class InstanceInfo
	{
		public Matrix4x4[] Matrixes;

		public int Count;

		public List<Token> Tokens;
	}

	private class Token
	{
		public Prefab Prefab;

		public int Offset;

		public int Length;

		public Token(Prefab prefab, int offset, int length)
		{
			Prefab = prefab;
			Offset = offset;
			Length = length;
		}
	}

	private class Pending
	{
		public readonly Token Token;

		public readonly Matrix4x4[] Matrices;

		public Pending(Token token, Matrix4x4[] matrices)
		{
			Token = token;
			Matrices = matrices;
		}
	}

	[SerializeField]
	private GPUInstancerPrefabManager prefabManager;

	[SerializeField]
	private GPUInstancerPrefab[] prefabs;

	private static Prefab[] AllPrefabs = new Prefab[2]
	{
		Prefab.Tie,
		Prefab.TiePlate
	};

	private InstanceInfo[] _entries;

	private Coroutine _coroutine;

	private readonly Dictionary<Prefab, List<Pending>> _pendingAdd = new Dictionary<Prefab, List<Pending>>();

	private readonly Dictionary<Prefab, Queue<Token>> _pendingRemove = new Dictionary<Prefab, Queue<Token>>();

	private void OnEnable()
	{
		Messenger.Default.Register<WorldDidMoveEvent>(this, WorldDidMove);
		_entries = new InstanceInfo[prefabs.Length];
		for (int i = 0; i < _entries.Length; i++)
		{
			int num = (Prefab)i switch
			{
				Prefab.Tie => 65535, 
				Prefab.TiePlate => 65535, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			_entries[i] = new InstanceInfo
			{
				Matrixes = new Matrix4x4[num],
				Count = 0,
				Tokens = new List<Token>()
			};
			GPUInstancerAPI.InitializeWithMatrix4x4Array(prefabManager, prefabs[i].prefabPrototype, _entries[i].Matrixes);
		}
		_coroutine = StartCoroutine(UpdateCoroutine());
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		StopCoroutine(_coroutine);
		_coroutine = null;
	}

	public object AddInstances(Prefab prefab, Matrix4x4[] array)
	{
		if (!Application.isPlaying)
		{
			return null;
		}
		Token token = new Token(prefab, 0, 0);
		Pending item = new Pending(token, array);
		if (!_pendingAdd.TryGetValue(prefab, out var value))
		{
			value = new List<Pending>();
			_pendingAdd[prefab] = value;
		}
		value.Add(item);
		return token;
	}

	public void Release(object tokenObject)
	{
		if (tokenObject == null)
		{
			return;
		}
		Token token = (Token)tokenObject;
		if (_pendingAdd.TryGetValue(token.Prefab, out var value))
		{
			int num = value.FindIndex((Pending p) => p.Token == token);
			if (num >= 0)
			{
				value.RemoveAt(num);
				return;
			}
		}
		if (!_pendingRemove.TryGetValue(token.Prefab, out var value2))
		{
			value2 = new Queue<Token>();
			_pendingRemove[token.Prefab] = value2;
		}
		value2.Enqueue(token);
	}

	private IEnumerator UpdateCoroutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.1f);
		while (!prefabManager.isInitialized)
		{
			yield return wait;
		}
		while (true)
		{
			Prefab[] allPrefabs = AllPrefabs;
			foreach (Prefab prefab in allPrefabs)
			{
				if (_pendingRemove.TryGetValue(prefab, out var value))
				{
					Token result;
					while (value.TryDequeue(out result))
					{
						RemoveInstances(result);
					}
				}
				if (!_pendingAdd.TryGetValue(prefab, out var value2))
				{
					continue;
				}
				while (value2.Count > 0)
				{
					Pending pending = value2[0];
					if (!TryAddInstances(pending.Token.Prefab, pending.Matrices, pending.Token))
					{
						break;
					}
					value2.RemoveAt(0);
				}
				if (value2.Count > 0)
				{
					Log.Debug("PendingAdd queue for {prefab} has {queueAddCount} entries totalling {matrixCount} instances; prefab has {entryCount} entries", prefab, value2.Count, value2.Sum((Pending p) => p.Matrices.Length), _entries[(int)prefab].Count);
				}
			}
			yield return wait;
		}
	}

	private void WorldDidMove(WorldDidMoveEvent evt)
	{
		Vector3 offset = evt.Offset;
		GPUInstancerAPI.SetGlobalPositionOffset(prefabManager, offset);
		Matrix4x4 matrix4x = Matrix4x4.Translate(offset);
		InstanceInfo[] entries = _entries;
		foreach (InstanceInfo instanceInfo in entries)
		{
			for (int j = 0; j < instanceInfo.Count; j++)
			{
				Matrix4x4 matrix4x2 = instanceInfo.Matrixes[j];
				instanceInfo.Matrixes[j] = matrix4x * matrix4x2;
			}
		}
	}

	private bool TryAddInstances(Prefab prefab, Matrix4x4[] array, Token token)
	{
		if ((int)prefab >= _entries.Length || _entries[(int)prefab].Matrixes == null)
		{
			return false;
		}
		InstanceInfo instanceInfo = _entries[(int)prefab];
		int num = Mathf.Min(instanceInfo.Matrixes.Length - instanceInfo.Count, array.Length);
		if (num < array.Length)
		{
			return false;
		}
		int count = instanceInfo.Count;
		Array.Copy(array, 0, instanceInfo.Matrixes, count, num);
		instanceInfo.Count += num;
		GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(prefabManager, prefabs[(int)prefab].prefabPrototype, instanceInfo.Matrixes, count, count, num);
		token.Prefab = prefab;
		token.Offset = count;
		token.Length = num;
		instanceInfo.Tokens.Add(token);
		return true;
	}

	private void RemoveInstances(Token token)
	{
		int prefab = (int)token.Prefab;
		InstanceInfo instanceInfo = _entries[prefab];
		int length = token.Length;
		int num = instanceInfo.Count - (token.Offset + length);
		Array.Copy(instanceInfo.Matrixes, token.Offset + length, instanceInfo.Matrixes, token.Offset, num);
		instanceInfo.Count -= length;
		Array.Clear(instanceInfo.Matrixes, instanceInfo.Count, length);
		int num2 = instanceInfo.Tokens.IndexOf(token);
		instanceInfo.Tokens.RemoveAt(num2);
		for (int i = num2; i < instanceInfo.Tokens.Count; i++)
		{
			instanceInfo.Tokens[i].Offset -= length;
		}
		GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(prefabManager, prefabs[prefab].prefabPrototype, instanceInfo.Matrixes, token.Offset, token.Offset, num + length);
	}
}
