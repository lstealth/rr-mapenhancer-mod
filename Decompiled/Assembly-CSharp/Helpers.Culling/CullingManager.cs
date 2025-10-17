using System;
using System.Collections.Generic;
using Serilog;
using UnityEngine;

namespace Helpers.Culling;

[ExecuteInEditMode]
public class CullingManager : MonoBehaviour
{
	public class Token : IDisposable
	{
		public ICullingEventHandler Handler;

		public readonly CullingManager Manager;

		public readonly int Index;

		public Token(int index, CullingManager manager, ICullingEventHandler handler)
		{
			Index = index;
			Manager = manager;
			Handler = handler;
		}

		public void Dispose()
		{
			Manager.Remove(this);
		}

		public void UpdatePosition(Transform transform, float? radius = null)
		{
			UpdatePosition(transform.position, radius);
		}

		public void UpdatePosition(Vector3 worldPosition, float? radius = null)
		{
			Manager.UpdatePosition(this, worldPosition, radius);
		}

		public void RegisterFixedUpdate(Transform transform)
		{
			Manager.RegisterFixedUpdate(this, transform);
		}
	}

	public interface ICullingEventHandler
	{
		void CullingSphereStateChanged(bool isVisible, int distanceBand);

		void RequestUpdateCullingPosition();
	}

	private CullingGroup _cullingGroup;

	private BoundingSphere[] _spheres;

	private List<Token> _tokens;

	private readonly Dictionary<Token, HashSet<Transform>> _fixedUpdateTransforms = new Dictionary<Token, HashSet<Transform>>();

	private float[] _distances;

	private string _managerName;

	private int _nextSphere;

	private int _sphereCount;

	private readonly HashSet<Token> _needsUpdate = new HashSet<Token>();

	public static CullingManager Hose => CullingManagerInitializer.Shared.Hose;

	public static CullingManager Bridge => CullingManagerInitializer.Shared.Bridge;

	public static CullingManager CTC => CullingManagerInitializer.Shared.CTC;

	public static CullingManager Signal => CullingManagerInitializer.Shared.Signal;

	public static CullingManager Scenery => CullingManagerInitializer.Shared.Scenery;

	public static CullingManager Flare => CullingManagerInitializer.Shared.Flare;

	public void Configure(string managerName, float[] distances)
	{
		_managerName = managerName;
		_distances = distances;
		_cullingGroup.SetBoundingDistances(_distances);
	}

	private void Awake()
	{
		_spheres = new BoundingSphere[32];
		_sphereCount = 0;
		_tokens = new List<Token>(_spheres.Length);
		GrowTokensToMatchSphereCount();
	}

	private void OnEnable()
	{
		_cullingGroup = new CullingGroup();
		_cullingGroup.SetBoundingSpheres(_spheres);
		_cullingGroup.SetBoundingSphereCount(_sphereCount);
		_cullingGroup.onStateChanged = CullingGroupStateChanged;
		_cullingGroup.SetBoundingDistances(_distances);
		_cullingGroup.AutoAssignTargetCamera(this);
		if (WorldTransformer.TryGetShared(out var shared))
		{
			shared.OnDidMove += OnWorldDidMove;
		}
	}

	private void Update()
	{
		if (_cullingGroup == null)
		{
			return;
		}
		foreach (Token item in _needsUpdate)
		{
			if (item == null)
			{
				Log.Error("Null token in _needsUpdate");
				continue;
			}
			int distanceBand = _cullingGroup.CalculateDistanceBand(_spheres[item.Index].position, _distances);
			bool isVisible = _cullingGroup.IsVisible(item.Index);
			if (item.Handler == null)
			{
				Log.Error("Null Handler in _needsUpdate with index {index}", item.Index);
			}
			else
			{
				item.Handler.CullingSphereStateChanged(isVisible, distanceBand);
			}
		}
		_needsUpdate.Clear();
	}

	private void FixedUpdate()
	{
		foreach (var (token2, hashSet2) in _fixedUpdateTransforms)
		{
			foreach (Transform item in hashSet2)
			{
				UpdatePosition(token2, item.position, null);
			}
		}
	}

	private void OnDisable()
	{
		DisposeCullingGroup();
		if (WorldTransformer.TryGetShared(out var shared))
		{
			shared.OnDidMove -= OnWorldDidMove;
		}
	}

	private void OnApplicationQuit()
	{
		DisposeCullingGroup();
	}

	public Token AddSphere(Transform entryTransform, float radius, ICullingEventHandler handler)
	{
		return AddSphere(entryTransform.position, radius, handler);
	}

	public Token AddSphere(Vector3 worldPosition, float radius, ICullingEventHandler handler)
	{
		while (_nextSphere >= _spheres.Length || _tokens[_nextSphere] != null)
		{
			if (_nextSphere >= _spheres.Length)
			{
				Array.Resize(ref _spheres, Mathf.CeilToInt((float)_spheres.Length * 1.5f));
				_cullingGroup.SetBoundingSpheres(_spheres);
				GrowTokensToMatchSphereCount();
				Log.Debug("CullingManager: Grow {len}", _spheres.Length);
			}
			if (_tokens[_nextSphere] != null)
			{
				_nextSphere++;
			}
		}
		Token token = new Token(_nextSphere, this, handler);
		_tokens[token.Index] = token;
		_spheres[token.Index] = new BoundingSphere(worldPosition, radius);
		_nextSphere++;
		_sphereCount = Mathf.Max(_sphereCount, _nextSphere);
		_cullingGroup.SetBoundingSphereCount(_sphereCount);
		_needsUpdate.Add(token);
		return token;
	}

	private void UpdatePosition(Token token, Vector3 worldPosition, float? radius)
	{
		CheckToken(token);
		_spheres[token.Index].position = worldPosition;
		if (radius.HasValue)
		{
			_spheres[token.Index].radius = radius.Value;
		}
	}

	private void RegisterFixedUpdate(Token token, Transform tokenTransform)
	{
		if (!_fixedUpdateTransforms.TryGetValue(token, out var value))
		{
			value = (_fixedUpdateTransforms[token] = new HashSet<Transform>());
		}
		value.Add(tokenTransform);
	}

	private void Remove(Token token)
	{
		CheckToken(token);
		_tokens[token.Index] = null;
		_needsUpdate.Remove(token);
		_fixedUpdateTransforms.Remove(token);
		if (token.Index < _nextSphere)
		{
			_nextSphere = token.Index;
		}
		token.Handler = null;
	}

	private void CheckToken(Token token)
	{
		if (_tokens[token.Index] != token)
		{
			throw new ArgumentException("Token already removed?");
		}
	}

	private void GrowTokensToMatchSphereCount()
	{
		while (_tokens.Count < _spheres.Length)
		{
			_tokens.Add(null);
		}
	}

	private void CullingGroupStateChanged(CullingGroupEvent evt)
	{
		_tokens[evt.index]?.Handler.CullingSphereStateChanged(evt.isVisible, evt.currentDistance);
	}

	private void OnWorldDidMove(Vector3 offset)
	{
		foreach (Token token in _tokens)
		{
			token?.Handler.RequestUpdateCullingPosition();
		}
	}

	private void DisposeCullingGroup()
	{
		if (_cullingGroup != null)
		{
			_cullingGroup.Dispose();
			_cullingGroup = null;
		}
	}
}
