using System;
using System.Text;
using Audio;
using Helpers;
using Helpers.Culling;
using Model;
using Serilog;
using UnityEngine;

namespace RollingStock;

public class Hose : MonoBehaviour, CullingManager.ICullingEventHandler
{
	private struct Point
	{
		public Vector3 Position;

		public Vector3 OldPosition;

		public Vector3 Acceleration;
	}

	private Hose _connectedTo;

	private bool _firstConnectedTo = true;

	private float _damping;

	public MeshRenderer meshRenderer;

	public HoseProfile profile;

	private float _hoseLength = 0.52f;

	private const int NumPoints = 9;

	private readonly Point[] _points = new Point[9];

	private readonly Vector3[] _lastUpdatePoints = new Vector3[9];

	private float _propulsion;

	private const int EdgeCount = 8;

	private Vector3[] _splinePoints;

	private Quaternion[] _splineRotations;

	private Transform _gladhand;

	private bool _isVisible;

	private TubeMeshBuilder _meshBuilder;

	private CullingManager.Token _cullingToken;

	private static readonly Vector3[] PointsAtRest = new Vector3[9]
	{
		new Vector3(0f, 0f, 0f),
		new Vector3(0f, -0.07f, 0.041f),
		new Vector3(0f, -0.138f, 0.074f),
		new Vector3(0f, -0.206f, 0.104f),
		new Vector3(0f, -0.277f, 0.131f),
		new Vector3(0f, -0.348f, 0.154f),
		new Vector3(0f, -0.419f, 0.172f),
		new Vector3(0f, -0.488f, 0.187f),
		new Vector3(0f, -0.555f, 0.197f)
	};

	public Func<float> OnGetPressure { get; set; }

	private Vector3 EndPoint => base.transform.TransformPoint(_points[^1].Position);

	private Quaternion EndRotation => base.transform.rotation * ((_splineRotations == null) ? Quaternion.identity : _splineRotations[^1]);

	private float EdgeLength => _hoseLength / 8f;

	private void Awake()
	{
		_gladhand = UnityEngine.Object.Instantiate(profile.gladhandPrefab, base.transform).transform;
		_damping = profile.dampingAtRest;
	}

	public void Configure(Vector3 airHosePosition)
	{
		float time = Vector3.Distance(new Vector3(0f, 0.5f, 1f), airHosePosition);
		_hoseLength = profile.lengthCurve.Evaluate(time);
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < _points.Length; i++)
		{
			_points[i].Position = zero;
			_points[i].OldPosition = zero;
			zero += Vector3.down * EdgeLength;
		}
		_meshBuilder = new TubeMeshBuilder(_points.Length, 6);
		meshRenderer.GetComponent<MeshFilter>().sharedMesh = _meshBuilder.Mesh;
		PopulatePoints(PointsAtRest);
	}

	private void PopulatePoints(Vector3[] points)
	{
		if (points.Length != _points.Length)
		{
			throw new ArgumentException("Points length mismatch");
		}
		float num = 0f;
		for (int i = 0; i < points.Length - 1; i++)
		{
			num += Vector3.Distance(points[i], points[i + 1]);
		}
		float num2 = _hoseLength / num;
		for (int j = 0; j < points.Length; j++)
		{
			_points[j].Position = (_points[j].OldPosition = points[j] * num2);
		}
	}

	private void OnEnable()
	{
		_cullingToken = CullingManager.Hose.AddSphere(base.transform, 2f, this);
		_cullingToken.RegisterFixedUpdate(base.transform);
	}

	private void OnDisable()
	{
		_cullingToken.Dispose();
		_cullingToken = null;
	}

	private void FixedUpdate()
	{
		if (_isVisible)
		{
			Simulate(Time.deltaTime);
			UpdateIfNeeded();
		}
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(_meshBuilder.Mesh);
	}

	public void SetConnectedTo(Hose other)
	{
		if ((object)_connectedTo == other)
		{
			return;
		}
		_connectedTo = other;
		if (!_firstConnectedTo)
		{
			if (_connectedTo == null)
			{
				Pop();
			}
			else
			{
				PlayConnect();
			}
		}
		_firstConnectedTo = false;
	}

	private void UpdateIfNeeded()
	{
		bool flag = false;
		for (int i = 0; i < _points.Length; i++)
		{
			if (!(Vector3.SqrMagnitude(_points[i].Position - _lastUpdatePoints[i]) < 1E-06f))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			UpdateSpline();
			_gladhand.localPosition = _points[^1].Position;
			_gladhand.rotation = EndRotation * Quaternion.Euler(0f, 0f, 180f);
			for (int j = 0; j < _points.Length; j++)
			{
				_lastUpdatePoints[j] = _points[j].Position;
			}
		}
	}

	private void UpdateSpline()
	{
		if (_splinePoints == null || _splinePoints.Length != _points.Length)
		{
			_splinePoints = new Vector3[_points.Length];
			_splineRotations = new Quaternion[_points.Length];
		}
		for (int i = 0; i < _points.Length; i++)
		{
			Vector3 position = _points[i].Position;
			_splinePoints[i] = position;
			Vector3 forward = ((i == _points.Length - 1) ? (position - _points[i - 1].Position) : (_points[i + 1].Position - position));
			_splineRotations[i] = Quaternion.LookRotation(forward, Vector3.up);
			_splinePoints[i] = _points[i].Position;
		}
		_meshBuilder.UpdateWithPoints(_splinePoints, _splineRotations, 0.022f);
	}

	private void OnDrawGizmos()
	{
		Gizmos.matrix = base.transform.localToWorldMatrix;
		Gizmos.DrawCube(Vector3.zero, Vector3.one * 0.02f);
		if (_splinePoints != null)
		{
			for (int i = 0; i < _splinePoints.Length; i++)
			{
				Vector3 vector = _splinePoints[i];
				Quaternion quaternion = _splineRotations[i];
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(vector, vector + quaternion * Vector3.forward * 0.05f);
				Gizmos.color = Color.green;
				Gizmos.DrawLine(vector, vector + quaternion * Vector3.up * 0.05f);
			}
		}
	}

	private void SetVisible(bool visible)
	{
		if (_isVisible == visible)
		{
			return;
		}
		_isVisible = visible;
		if (_isVisible && _splinePoints != null)
		{
			_damping = 1f;
			for (int i = 0; i < 10; i++)
			{
				Simulate(Time.fixedDeltaTime);
			}
			UpdateIfNeeded();
			_damping = profile.dampingAtRest;
		}
		meshRenderer.enabled = _isVisible;
		MeshRenderer[] componentsInChildren = _gladhand.GetComponentsInChildren<MeshRenderer>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			componentsInChildren[j].enabled = _isVisible;
		}
	}

	[ContextMenu("Log Points")]
	private void LogPoints()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("var points = new Vector3[] {");
		Point[] points = _points;
		for (int i = 0; i < points.Length; i++)
		{
			Point point = points[i];
			stringBuilder.AppendLine($"    new Vector3({point.Position.x:F3}f, {point.Position.y:F3}f, {point.Position.z:F3}f),");
		}
		stringBuilder.AppendLine("};");
		Debug.Log($"Hose Points:\n{stringBuilder}");
	}

	private void Simulate(float deltaTime)
	{
		UpdateForces();
		Integrate(deltaTime);
		IterateCollisions();
		_damping = Mathf.Lerp(_damping, profile.dampingAtRest, profile.dampingRestSpeed * deltaTime);
	}

	private void UpdateForces()
	{
		Vector3 acceleration = Vector3.down * profile.gravity;
		for (int i = 0; i < _points.Length; i++)
		{
			_points[i].Acceleration = Vector3.zero;
		}
		_points[^1].Acceleration = acceleration;
		if (_propulsion > 0.01f)
		{
			Vector3 normalized = new Vector3(-1f, 0f, UnityEngine.Random.Range(-0.5f, 0.5f)).normalized;
			_points[^1].Acceleration += _propulsion * profile.propulsion * normalized;
			_propulsion *= profile.propulsionDecay;
		}
	}

	private void Integrate(float dt)
	{
		float damping = _damping;
		for (int i = 0; i < _points.Length; i++)
		{
			Point point = _points[i];
			Vector3 position = point.Position;
			point.Position += damping * (point.Position - point.OldPosition + point.Acceleration * (dt * dt));
			point.OldPosition = position;
			_points[i] = point;
		}
	}

	private void UpdateEdges()
	{
		for (int i = 1; i < _points.Length; i++)
		{
			Point point = _points[i - 1];
			Point point2 = _points[i];
			Vector3 vector = point2.Position - point.Position;
			float num = vector.magnitude - EdgeLength;
			vector.Normalize();
			Vector3 vector2 = vector * (num * 0.5f);
			point.Position += vector2;
			point2.Position -= vector2;
			_points[i - 1] = point;
			_points[i] = point2;
		}
		ConstrainFirstEdge();
		_points[0].Position = Vector3.zero;
		float maxDegreesDelta = profile.maxDegreesDelta;
		float maxDegreesMove = profile.maxDegreesMove;
		for (int j = 1; j < _points.Length; j++)
		{
			Point point3 = _points[j - 1];
			Point point4 = _points[j];
			Vector3 vector3 = ((j >= 2) ? _points[j - 2].Position : new Vector3(0f, 1f, -0.5f));
			Vector3 vector4 = point3.Position + (point3.Position - vector3).normalized * Vector3.Distance(point4.Position, point3.Position);
			Vector3 vector5 = point4.Position - point3.Position;
			Vector3 vector6 = vector4 - point3.Position;
			float num2 = Vector3.Angle(vector5, vector6);
			_points[j].Position = point3.Position + Vector3.RotateTowards(vector5, vector6, Mathf.Clamp(num2 - maxDegreesDelta, 0f, maxDegreesMove) * (MathF.PI / 180f), profile.maxMagnitudeDelta);
		}
		ConstrainFirstEdge();
		UpdateForConnection();
	}

	private void ConstrainFirstEdge()
	{
		_points[0].Position = Vector3.zero;
		Vector3 normalized = (PointsAtRest[1] - PointsAtRest[0]).normalized;
		Vector3 vector = _points[1].Position - _points[0].Position;
		Vector3 normalized2 = vector.normalized;
		float magnitude = vector.magnitude;
		float num = Vector3.Angle(normalized2, normalized);
		float num2 = profile.maxDegreesDelta * 2f;
		if (num > num2)
		{
			Vector3 vector2 = Vector3.RotateTowards(normalized2, normalized, (num - num2) * 0.1f * (MathF.PI / 180f), 0.01f);
			Vector3 position = _points[0].Position + vector2 * magnitude;
			_points[1].Position = position;
		}
	}

	private void Pop()
	{
		_damping = profile.dampingAtPop;
		float value = OnGetPressure?.Invoke() ?? 0f;
		_propulsion = Mathf.Clamp01(Mathf.InverseLerp(0f, 90f, value));
		PlayPop(_propulsion);
	}

	private void UpdateForConnection()
	{
		if (!(_connectedTo == null))
		{
			Vector3 endPoint = EndPoint;
			Vector3 endPoint2 = _connectedTo.EndPoint;
			float num = Vector3.Distance(endPoint, endPoint2);
			if (num > 10f)
			{
				Car componentInParent = GetComponentInParent<Car>();
				Log.Warning("Hose for {car} is too long: {distance}", componentInParent, num);
				return;
			}
			Quaternion endRotation = EndRotation;
			Quaternion endRotation2 = _connectedTo.EndRotation;
			Vector3 gladhandOffset = profile.gladhandOffset;
			Vector3 vector = endRotation * gladhandOffset;
			Vector3 vector2 = endRotation2 * gladhandOffset;
			Vector3 a = endPoint + vector;
			Vector3 b = endPoint2 + vector2;
			float t = Mathf.InverseLerp(0f, _hoseLength + _connectedTo._hoseLength, _hoseLength);
			Vector3 vector3 = Vector3.Lerp(a, b, t);
			Vector3 b2 = base.transform.InverseTransformPoint(vector3 - vector);
			_points[^1].Position = Vector3.Lerp(_points[^1].Position, b2, 0.5f);
		}
	}

	private void IterateCollisions()
	{
		for (int i = 0; i < 4; i++)
		{
			UpdateEdges();
			_points[0].Position = Vector3.zero;
		}
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		SetVisible(isVisible && distanceBand < 1);
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}

	private void PlayPop(float intensity)
	{
		if (profile.popClips.Count == 0)
		{
			Log.Warning("Hose: Can't play pop - no clips");
		}
		else
		{
			AudioClip clip = profile.popClips.Random();
			PlayClip(intensity, clip);
		}
		if (profile.disconnectClips.Count == 0)
		{
			Log.Warning("Hose: Can't play disconnect - no clips");
			return;
		}
		AudioClip clip2 = profile.disconnectClips.Random();
		PlayClip(Mathf.Lerp(0.75f, 1f, intensity), clip2);
	}

	private void PlayConnect()
	{
		if (profile.connectClips.Count != 0)
		{
			PlayClip(1f, profile.connectClips.Random());
		}
	}

	private void PlayClip(float intensity, AudioClip clip)
	{
		IAudioSource audioSource = VirtualAudioSourcePool.Checkout("HosePop", clip, loop: false, AudioController.Group.AirPop, 10, base.transform, AudioDistance.Local);
		audioSource.volume = intensity;
		audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
		audioSource.Play();
		VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
	}
}
