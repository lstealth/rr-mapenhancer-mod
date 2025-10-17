using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Image))]
public class ColorPicker : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler, IPointerUpHandler
{
	private enum PointerDownLocation
	{
		HueCircle,
		SVSquare,
		Outside
	}

	private const float recip2Pi = 1f / (2f * MathF.PI);

	private const string colorPickerShaderName = "UI/ColorPicker";

	private static readonly int _HSV = Shader.PropertyToID("_HSV");

	private static readonly int _AspectRatio = Shader.PropertyToID("_AspectRatio");

	private static readonly int _HueCircleInner = Shader.PropertyToID("_HueCircleInner");

	private static readonly int _SVSquareSize = Shader.PropertyToID("_SVSquareSize");

	[SerializeField]
	[HideInInspector]
	private Shader colorPickerShader;

	private Material generatedMaterial;

	private PointerDownLocation pointerDownLocation = PointerDownLocation.Outside;

	private RectTransform rectTransform;

	private Image image;

	private float h;

	private float s;

	private float v;

	public Color color
	{
		get
		{
			return Color.HSVToRGB(h, s, v);
		}
		set
		{
			SetColor(value);
		}
	}

	public event Action<Color> onColorChanged;

	public void SetColor(Color newColor, bool notify = true)
	{
		Color.RGBToHSV(newColor, out h, out s, out v);
		ApplyColor(notify);
	}

	private void Awake()
	{
		rectTransform = base.transform as RectTransform;
		image = GetComponent<Image>();
		if (WrongShader())
		{
			Debug.LogWarning("Color picker requires image material with UI/ColorPicker shader.");
			if (Application.isPlaying && colorPickerShader != null)
			{
				generatedMaterial = new Material(colorPickerShader);
				generatedMaterial.hideFlags = HideFlags.HideAndDontSave;
			}
			image.material = generatedMaterial;
		}
		else
		{
			ApplyColor();
		}
	}

	private void Reset()
	{
		colorPickerShader = Shader.Find("UI/ColorPicker");
	}

	private bool WrongShader()
	{
		return image?.material?.shader?.name != "UI/ColorPicker";
	}

	private void Update()
	{
		if (!WrongShader())
		{
			Rect rect = rectTransform.rect;
			image.material.SetFloat(_AspectRatio, rect.width / rect.height);
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!WrongShader())
		{
			Vector2 relativePosition = GetRelativePosition(eventData);
			if (pointerDownLocation == PointerDownLocation.HueCircle)
			{
				h = (Mathf.Atan2(relativePosition.y, relativePosition.x) * (1f / (2f * MathF.PI)) + 1f) % 1f;
				ApplyColor();
			}
			if (pointerDownLocation == PointerDownLocation.SVSquare)
			{
				float num = image.material.GetFloat(_SVSquareSize);
				s = Mathf.InverseLerp(0f - num, num, relativePosition.x);
				v = Mathf.InverseLerp(0f - num, num, relativePosition.y);
				ApplyColor();
			}
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (WrongShader())
		{
			return;
		}
		Vector2 relativePosition = GetRelativePosition(eventData);
		float magnitude = relativePosition.magnitude;
		if (magnitude < 0.5f && magnitude > image.material.GetFloat(_HueCircleInner))
		{
			pointerDownLocation = PointerDownLocation.HueCircle;
			h = (Mathf.Atan2(relativePosition.y, relativePosition.x) * (1f / (2f * MathF.PI)) + 1f) % 1f;
			ApplyColor();
			return;
		}
		float num = image.material.GetFloat(_SVSquareSize);
		if (relativePosition.x >= 0f - num && relativePosition.x <= num && relativePosition.y >= 0f - num && relativePosition.y <= num)
		{
			pointerDownLocation = PointerDownLocation.SVSquare;
			s = Mathf.InverseLerp(0f - num, num, relativePosition.x);
			v = Mathf.InverseLerp(0f - num, num, relativePosition.y);
			ApplyColor();
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		pointerDownLocation = PointerDownLocation.Outside;
	}

	private void ApplyColor(bool notify = true)
	{
		if (image == null)
		{
			image = GetComponent<Image>();
		}
		image.material.SetVector(_HSV, new Vector3(h, s, v));
		if (notify)
		{
			this.onColorChanged?.Invoke(color);
		}
	}

	private void OnDestroy()
	{
		if (generatedMaterial != null)
		{
			UnityEngine.Object.DestroyImmediate(generatedMaterial);
		}
	}

	public Vector2 GetRelativePosition(PointerEventData eventData)
	{
		Rect squaredRect = GetSquaredRect();
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint);
		return new Vector2(InverseLerpUnclamped(squaredRect.xMin, squaredRect.xMax, localPoint.x), InverseLerpUnclamped(squaredRect.yMin, squaredRect.yMax, localPoint.y)) - Vector2.one * 0.5f;
	}

	public Rect GetSquaredRect()
	{
		Rect rect = rectTransform.rect;
		float num = Mathf.Min(rect.width, rect.height);
		return new Rect(rect.center - Vector2.one * num * 0.5f, Vector2.one * num);
	}

	public float InverseLerpUnclamped(float min, float max, float value)
	{
		return (value - min) / (max - min);
	}
}
