using System;
using System.Collections;
using Helpers;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Track.Signals.Panel;

public class CTCPanelMarker : MonoBehaviour, IPickable
{
	private enum EditOption
	{
		Cancel,
		Delete,
		Apply
	}

	public CTCPanelMarkerManager manager;

	[SerializeField]
	private TMP_Text label;

	[SerializeField]
	private Image image;

	private Coroutine _dragCoroutine;

	private RectTransform _canvasRectTransform;

	private Vector2 _pointAtActivate;

	private Camera _camera;

	private RectTransform _rectTransform;

	private Vector2 _anchoredPositionAtActivate;

	private float _lastDeactivate;

	private CTCPanelSchematicFace _currentFace;

	private string _id;

	private Vector2 _lastSentPosition;

	private Vector2 _targetAnchoredPosition;

	private Coroutine _animateToTarget;

	private const float InsetBoundsWidth = 1.25f;

	private const float InsetBoundsHeight = 0.6f;

	private const int CharacterLimit = 48;

	public float MaxPickDistance => 5f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo => new TooltipInfo("Marker", label.text);

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private Vector2 CanvasSize
	{
		get
		{
			Rect rect = _canvasRectTransform.rect;
			rect.width -= 1.25f;
			rect.height -= 0.6f;
			return new Vector2(rect.width, rect.height);
		}
	}

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
	}

	public void Configure(string id, string text)
	{
		_id = id;
		label.richText = false;
		label.text = text.Truncate(48);
		image.color = InferColorFromText(text);
	}

	public void Position(Vector2 logical)
	{
		if (_dragCoroutine == null)
		{
			(int, Vector2) tuple = PositionFromLogical(logical);
			int item = tuple.Item1;
			Vector2 item2 = tuple.Item2;
			CTCPanelSchematicFace cTCPanelSchematicFace = manager.faces[item];
			bool flag = cTCPanelSchematicFace != _currentFace;
			if (flag)
			{
				ChangeFace(cTCPanelSchematicFace);
			}
			Vector2 canvasSize = CanvasSize;
			_targetAnchoredPosition = new Vector2(Mathf.Lerp((0f - canvasSize.x) / 2f, canvasSize.x / 2f, item2.x), Mathf.Lerp((0f - canvasSize.y) / 2f, canvasSize.y / 2f, item2.y));
			if (_rectTransform.anchoredPosition == Vector2.zero || flag)
			{
				_rectTransform.anchoredPosition = _targetAnchoredPosition;
				StopAnimateToTarget();
			}
			else if (_animateToTarget == null)
			{
				_animateToTarget = StartCoroutine(AnimateToTarget());
			}
		}
	}

	private IEnumerator AnimateToTarget()
	{
		do
		{
			_rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, _targetAnchoredPosition, 0.1f);
			yield return null;
		}
		while (Vector2.Distance(_rectTransform.anchoredPosition, _targetAnchoredPosition) > 0.01f);
		_rectTransform.anchoredPosition = _targetAnchoredPosition;
		_animateToTarget = null;
	}

	private void StopAnimateToTarget()
	{
		if (_animateToTarget != null)
		{
			StopCoroutine(_animateToTarget);
			_animateToTarget = null;
		}
	}

	public void Activate(PickableActivateEvent evt)
	{
		Debug.Log("Marker Activate");
		_camera = Camera.main;
		_currentFace = manager.FaceForMousePosition(Input.mousePosition, out var _);
		if (_currentFace == null)
		{
			Debug.LogWarning("Couldn't find face.");
			return;
		}
		if (_dragCoroutine != null)
		{
			StopCoroutine(_dragCoroutine);
		}
		_dragCoroutine = StartCoroutine(DragCoroutine());
		UpdateForFace();
		_anchoredPositionAtActivate = _rectTransform.anchoredPosition;
	}

	public void Deactivate()
	{
		if (_dragCoroutine != null)
		{
			StopCoroutine(_dragCoroutine);
			_dragCoroutine = null;
			float unscaledTime = Time.unscaledTime;
			if (unscaledTime - _lastDeactivate < 0.25f)
			{
				ShowEditMarker();
			}
			_lastDeactivate = unscaledTime;
			SendPosition(0.001f);
		}
	}

	private void ShowEditMarker()
	{
		ModalAlertController.Present("Edit Marker", null, label.text ?? "", new(EditOption, string)[3]
		{
			(EditOption.Cancel, "Cancel"),
			(EditOption.Delete, "Delete"),
			(EditOption.Apply, "Apply")
		}, delegate((EditOption, string) tuple)
		{
			var (editOption, text) = tuple;
			switch (editOption)
			{
			case EditOption.Delete:
				Delete();
				break;
			case EditOption.Apply:
				HandleSetTextFromModal(text);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case EditOption.Cancel:
				break;
			}
		});
	}

	private void Delete()
	{
		manager.RemoveMarker(_id);
	}

	private void HandleSetTextFromModal(string text)
	{
		text = text.Truncate(48);
		manager.UpdateMarker(_id, text);
	}

	private static Color InferColorFromText(string text)
	{
		Color result = ColorHelper.ColorFromHexLiteral("#72B159");
		Color result2 = ColorHelper.ColorFromHexLiteral("#B15959");
		Color result3 = ColorHelper.ColorFromHexLiteral("#5A82B1");
		if (text.StartsWith(">") || text.EndsWith(">"))
		{
			return result;
		}
		if (text.StartsWith("<") || text.EndsWith("<"))
		{
			return result2;
		}
		return result3;
	}

	private Vector2 CurrentLogicalPosition()
	{
		int num = Mathf.Clamp(manager.faces.IndexOf(_currentFace), 0, manager.faces.Count - 1);
		Vector2 canvasSize = CanvasSize;
		Vector2 anchoredPosition = _rectTransform.anchoredPosition;
		return new Vector2(Mathf.InverseLerp((0f - canvasSize.x) / 2f, canvasSize.x / 2f, anchoredPosition.x), Mathf.InverseLerp((0f - canvasSize.y) / 2f, canvasSize.y / 2f, anchoredPosition.y)) + new Vector2(num, 0f);
	}

	private (int, Vector2) PositionFromLogical(Vector2 logical)
	{
		int value = Mathf.FloorToInt(logical.x);
		value = Mathf.Clamp(value, 0, manager.faces.Count - 1);
		logical.x -= value;
		logical.x = Mathf.Clamp01(logical.x);
		logical.y = Mathf.Clamp01(logical.y);
		return (value, logical);
	}

	private void SendPosition(float threshold)
	{
		Vector2 vector = CurrentLogicalPosition();
		if (!(Vector2.Distance(_lastSentPosition, vector) < threshold))
		{
			manager.SendPosition(_id, vector);
			_lastSentPosition = vector;
		}
	}

	private void UpdateForFace()
	{
		_canvasRectTransform = _currentFace.RectTransform;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, Input.mousePosition, _camera, out _pointAtActivate);
	}

	private IEnumerator DragCoroutine()
	{
		while (true)
		{
			yield return null;
			Vector3 mousePosition = Input.mousePosition;
			Vector2 localPointOnFaceCanvas;
			CTCPanelSchematicFace cTCPanelSchematicFace = manager.FaceForMousePosition(mousePosition, out localPointOnFaceCanvas);
			if (!(cTCPanelSchematicFace == null))
			{
				if (cTCPanelSchematicFace != _currentFace)
				{
					ChangeFace(cTCPanelSchematicFace);
					_anchoredPositionAtActivate = localPointOnFaceCanvas;
				}
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, mousePosition, _camera, out var localPoint))
				{
					Vector2 vector = localPoint - _pointAtActivate;
					Vector2 anchoredPosition = _anchoredPositionAtActivate + vector;
					Vector2 canvasSize = CanvasSize;
					anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, (0f - canvasSize.x) / 2f, canvasSize.x / 2f);
					anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, (0f - canvasSize.y) / 2f, canvasSize.y / 2f);
					_rectTransform.anchoredPosition = anchoredPosition;
					SendPosition(0.1f);
				}
			}
		}
	}

	private void ChangeFace(CTCPanelSchematicFace face)
	{
		_currentFace = face;
		UpdateForFace();
		base.transform.SetParent(face.canvas.transform, worldPositionStays: false);
		Vector3 localPosition = base.transform.localPosition;
		localPosition.z = -0.2f;
		base.transform.localPosition = localPosition;
	}
}
