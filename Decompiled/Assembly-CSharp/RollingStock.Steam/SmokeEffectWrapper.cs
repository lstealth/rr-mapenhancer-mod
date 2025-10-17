using UnityEngine;
using UnityEngine.VFX;

namespace RollingStock.Steam;

public readonly struct SmokeEffectWrapper
{
	private readonly VisualEffect _effect;

	private static readonly int VFXNameRate = Shader.PropertyToID("Rate");

	private static readonly int VFXNameVelocity = Shader.PropertyToID("Velocity");

	private static readonly int VFXNameLifetime = Shader.PropertyToID("Lifetime");

	private static readonly int VFXNameColor = Shader.PropertyToID("Color");

	private static readonly int VFXNameSize0 = Shader.PropertyToID("Size0");

	private static readonly int VFXNameSize1 = Shader.PropertyToID("Size1");

	private static readonly int VFXNameTurbulenceIntensity = Shader.PropertyToID("TurbulenceIntensity");

	private static readonly int VFXNamePositionOffset = Shader.PropertyToID("PositionOffset");

	public bool IsValid => _effect != null;

	public float Rate
	{
		get
		{
			return _effect.GetFloat(VFXNameRate);
		}
		set
		{
			_effect.SetFloat(VFXNameRate, value);
		}
	}

	public float Velocity
	{
		get
		{
			return _effect.GetFloat(VFXNameVelocity);
		}
		set
		{
			_effect.SetFloat(VFXNameVelocity, value);
		}
	}

	public float Lifetime
	{
		get
		{
			return _effect.GetFloat(VFXNameLifetime);
		}
		set
		{
			_effect.SetFloat(VFXNameLifetime, value);
		}
	}

	public float Size0
	{
		set
		{
			_effect.SetFloat(VFXNameSize0, value);
		}
	}

	public float Size1
	{
		set
		{
			_effect.SetFloat(VFXNameSize1, value);
		}
	}

	public float TurbulenceIntensity
	{
		set
		{
			_effect.SetFloat(VFXNameTurbulenceIntensity, value);
		}
	}

	public Vector4 Color
	{
		get
		{
			return _effect.GetVector4(VFXNameColor);
		}
		set
		{
			_effect.SetVector4(VFXNameColor, value);
		}
	}

	public Vector3 PositionOffset
	{
		get
		{
			return _effect.GetVector3(VFXNamePositionOffset);
		}
		set
		{
			_effect.SetVector3(VFXNamePositionOffset, value);
		}
	}

	public SmokeEffectWrapper(VisualEffect effect)
	{
		_effect = effect;
	}
}
