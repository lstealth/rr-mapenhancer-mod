using JetBrains.Annotations;
using UnityEngine;

namespace Helpers;

[ExecuteInEditMode]
public class InstancedMeshDrawer : MonoBehaviour
{
	private struct InstanceEntry
	{
		[UsedImplicitly]
		public Matrix4x4 Position;

		[UsedImplicitly]
		public Matrix4x4 Inverse;

		[UsedImplicitly]
		public Vector4 Control;

		public const int SizeInBytes = 144;
	}

	public Mesh mesh;

	public int subMeshIndex;

	public Material material;

	public Matrix4x4[] instances;

	private ComputeBuffer _positionBuffer;

	private ComputeBuffer _argsBuffer;

	private uint[] _args = new uint[5];

	private Vector3 _lastPosition = Vector3.zero;

	private int _cachedInstanceCount = -1;

	private int _cachedSubMeshIndex = -1;

	private Bounds _bounds;

	private MaterialPropertyBlock _propertyBlock;

	public void Mark()
	{
		_lastPosition = base.transform.position;
	}

	private void Start()
	{
		UpdateBuffers();
	}

	private void Update()
	{
		bool flag = _argsBuffer == null;
		if (base.transform.hasChanged)
		{
			base.transform.hasChanged = false;
			flag = true;
			OffsetInstancesIfNeeded();
		}
		if (flag || _cachedInstanceCount != instances.Length || _cachedSubMeshIndex != subMeshIndex)
		{
			UpdateBuffers();
		}
		Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, material, _bounds, _argsBuffer, 0, _propertyBlock);
	}

	private void OnDisable()
	{
		_positionBuffer?.Release();
		_positionBuffer = null;
		_argsBuffer?.Release();
		_argsBuffer = null;
	}

	private void OffsetInstancesIfNeeded()
	{
		Vector3 vector = base.transform.position - _lastPosition;
		if (!((double)vector.magnitude < 0.001))
		{
			for (int i = 0; i < instances.Length; i++)
			{
				instances[i] = Matrix4x4.Translate(vector) * instances[i];
			}
			_lastPosition = base.transform.position;
		}
	}

	private void UpdateBuffers()
	{
		_positionBuffer?.Release();
		_positionBuffer = new ComputeBuffer(instances.Length, 144);
		InstanceEntry[] array = new InstanceEntry[instances.Length];
		for (int i = 0; i < instances.Length; i++)
		{
			Matrix4x4 position = instances[i];
			array[i].Position = position;
			array[i].Inverse = position.inverse;
			array[i].Control = Vector4.zero;
			Vector3 center = position.MultiplyPoint3x4(Vector3.zero);
			Bounds bounds = new Bounds(center, 2f * Vector3.one);
			if (i == 0)
			{
				_bounds = bounds;
			}
			else
			{
				_bounds.Encapsulate(bounds);
			}
		}
		_positionBuffer.SetData(array);
		if (_propertyBlock == null)
		{
			_propertyBlock = new MaterialPropertyBlock();
		}
		_propertyBlock.SetBuffer("VisibleShaderDataBuffer", _positionBuffer);
		_propertyBlock.SetBuffer("IndirectShaderDataBuffer", _positionBuffer);
		if (_argsBuffer == null)
		{
			_argsBuffer = new ComputeBuffer(1, _args.Length * 4, ComputeBufferType.DrawIndirect);
		}
		if (mesh != null)
		{
			_args[0] = mesh.GetIndexCount(subMeshIndex);
			_args[1] = (uint)instances.Length;
			_args[2] = mesh.GetIndexStart(subMeshIndex);
			_args[3] = mesh.GetBaseVertex(subMeshIndex);
		}
		else
		{
			_args[0] = (_args[1] = (_args[2] = (_args[3] = 0u)));
		}
		_argsBuffer.SetData(_args);
		_cachedInstanceCount = instances.Length;
		_cachedSubMeshIndex = subMeshIndex;
	}
}
