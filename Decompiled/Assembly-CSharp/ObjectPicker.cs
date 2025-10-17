using System.Text;
using Helpers;
using RLD;
using Track;
using UI;
using UI.Common;
using UI.ContextMenu;
using UnityEngine;

public class ObjectPicker : MonoBehaviour
{
	[SerializeField]
	private GameInput gameInput;

	public Callout calloutPrefab;

	public Canvas canvas;

	private static ObjectPicker _instance;

	private Callout _callout;

	private Camera _camera;

	private int _pickableLayerMask;

	private bool _activatePrimaryDown;

	private bool _activatePrimaryUp;

	private bool _activateSecondary;

	private IPickable _active;

	private IPickable _target;

	private TooltipInfo _displayTooltipInfo;

	private TooltipInfo _uiTooltipInfo;

	private float _uiTooltipTime;

	private Vector3 _uiTooltipMousePosition;

	private bool _showDebugInfo;

	private string _debugInfo = "";

	private bool _mouseOverUI;

	public static ObjectPicker Shared
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindObjectOfType<ObjectPicker>();
			}
			return _instance;
		}
	}

	public bool IsOverObject
	{
		get
		{
			if (_active != null || _target != null)
			{
				return true;
			}
			RTGizmosEngine get = MonoSingleton<RTGizmosEngine>.Get;
			if (get != null)
			{
				return get.HoveredGizmo != null;
			}
			return false;
		}
	}

	public IPickable HoveringOver => _target;

	public float DistanceToTarget { get; private set; }

	public static int LayerClickable => Layers.Clickable;

	private void Start()
	{
		_callout = Object.Instantiate(calloutPrefab, canvas.transform);
		_callout.gameObject.SetActive(value: false);
		_pickableLayerMask = (1 << LayerClickable) | (1 << Layers.UI) | (1 << Layers.Default) | (1 << Layers.Terrain);
	}

	private void Update()
	{
		if (!MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			return;
		}
		TooltipInfo uiTooltipInfo = _uiTooltipInfo;
		_mouseOverUI = GameInput.IsMouseOverUI(out _uiTooltipInfo, out _debugInfo);
		if (_mouseOverUI)
		{
			_displayTooltipInfo = TooltipInfo.Empty;
			if (!uiTooltipInfo.Equals(_uiTooltipInfo))
			{
				_uiTooltipTime = Time.realtimeSinceStartup;
				_uiTooltipMousePosition = Input.mousePosition;
			}
		}
		if (!_mouseOverUI && gameInput.PrimaryPressStartedThisFrame)
		{
			_activatePrimaryDown = true;
		}
		if (gameInput.PrimaryPressEndedThisFrame)
		{
			_activatePrimaryUp = true;
		}
		if (!_mouseOverUI && gameInput.ActivateSecondary)
		{
			_activateSecondary = true;
		}
		if (Input.GetKeyDown(KeyCode.F8))
		{
			_showDebugInfo = !_showDebugInfo;
		}
		TooltipInfo tooltipInfo;
		if (!_uiTooltipInfo.IsEmpty)
		{
			Vector3 mousePosition = Input.mousePosition;
			float num = Vector3.Distance(mousePosition, _uiTooltipMousePosition);
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (num > 5f)
			{
				_uiTooltipTime = realtimeSinceStartup;
				_uiTooltipMousePosition = mousePosition;
			}
			tooltipInfo = ((!(realtimeSinceStartup - _uiTooltipTime > 0.5f)) ? TooltipInfo.Empty : _uiTooltipInfo);
		}
		else
		{
			tooltipInfo = _displayTooltipInfo;
		}
		_callout.SetTooltipInfo(tooltipInfo);
		UpdateCalloutPosition();
	}

	private void FixedUpdate()
	{
		if (!MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			return;
		}
		bool flag = IsMouseOnScreen();
		bool activatePrimaryDown = _activatePrimaryDown;
		bool activateSecondary = _activateSecondary;
		bool flag2 = _activatePrimaryUp || !flag;
		_activatePrimaryDown = false;
		_activatePrimaryUp = false;
		_activateSecondary = false;
		if (_mouseOverUI && !flag2)
		{
			return;
		}
		TooltipInfo displayTooltipInfo = TooltipInfo.Empty;
		if (_active == null)
		{
			Vector3 mousePosition = Input.mousePosition;
			Ray ray = _camera.ScreenPointToRay(mousePosition);
			if (TryGetPickableUnderMouse(ray, out var picked, out var distanceToPickable))
			{
				PickableActivationFilter activationFilter = picked.ActivationFilter;
				if (activateSecondary && activationFilter.Accepts(PickableActivation.Secondary))
				{
					PickableActivateEvent evt = CreateEvent(PickableActivation.Secondary);
					picked.Activate(evt);
					picked.Deactivate();
				}
				else if (activatePrimaryDown && activationFilter.Accepts(PickableActivation.Primary))
				{
					PickableActivateEvent evt2 = CreateEvent(PickableActivation.Primary);
					_active = picked;
					_active.Activate(evt2);
				}
				displayTooltipInfo = picked.TooltipInfo;
			}
			else if (GameInput.shared.Query)
			{
				displayTooltipInfo = QueryTooltipInfo(ray);
			}
			if (GameInput.shared.CopyLocation)
			{
				CopyLocation(ray);
			}
			_target = picked;
			DistanceToTarget = distanceToPickable;
		}
		else
		{
			try
			{
				displayTooltipInfo = _active.TooltipInfo;
			}
			catch (MissingReferenceException arg)
			{
				Debug.LogWarning($"Dropping _active: {arg}");
				_active = null;
			}
		}
		if (flag2 && _active != null)
		{
			_active.Deactivate();
			_active = null;
		}
		_displayTooltipInfo = displayTooltipInfo;
	}

	private static PickableActivateEvent CreateEvent(PickableActivation activation)
	{
		return new PickableActivateEvent
		{
			Activation = activation,
			IsControlDown = GameInput.IsControlDown,
			IsShiftDown = GameInput.IsShiftDown
		};
	}

	private bool TryGetPickableUnderMouse(Ray ray, out IPickable picked, out float distanceToPickable)
	{
		distanceToPickable = float.MaxValue;
		int num = int.MinValue;
		picked = null;
		float num2 = 500f;
		StringBuilder stringBuilder = (_showDebugInfo ? new StringBuilder() : null);
		RaycastHit hitInfo;
		while (Physics.Raycast(ray, out hitInfo, num2, _pickableLayerMask))
		{
			GameObject gameObject = hitInfo.collider.gameObject;
			int layer = gameObject.layer;
			num2 -= Vector3.Distance(ray.origin, hitInfo.point);
			ray.origin = hitInfo.point + ray.direction * 0.001f;
			stringBuilder?.Append("[" + gameObject.name + "]");
			if (layer == Layers.Clickable)
			{
				IPickable componentInParent = gameObject.GetComponentInParent<IPickable>();
				if (componentInParent == null)
				{
					Debug.LogWarning("Object " + gameObject.name + " hit but no PickerBehavior", gameObject);
					break;
				}
				float num3 = Vector3.Distance(_camera.transform.position, hitInfo.point);
				if (num3 <= componentInParent.MaxPickDistance && componentInParent.Priority > num)
				{
					picked = componentInParent;
					distanceToPickable = num3;
					num = componentInParent.Priority;
					num2 = 2f;
					stringBuilder?.Append($" (picked) pri={componentInParent.Priority} {num3:F1}/{componentInParent.MaxPickDistance:F1}");
					if (num >= 0)
					{
						break;
					}
				}
				stringBuilder?.Append(" --> ");
				continue;
			}
			stringBuilder?.Append(" layer=" + LayerMask.LayerToName(layer));
			break;
		}
		_debugInfo = stringBuilder?.ToString();
		return picked != null;
	}

	private TooltipInfo QueryTooltipInfo(Ray ray)
	{
		if (!TryGetTrackLocation(ray, out var location))
		{
			return TooltipInfo.Empty;
		}
		Graph graph = TrainController.Shared.graph;
		float num = graph.CurvatureAtLocation(location);
		float num2 = Mathf.Abs(graph.GradeAtLocation(location));
		return new TooltipInfo("Track", $"{num2:F1}%, {num:F0} deg");
	}

	private bool TryGetTrackLocation(Ray ray, out Location location)
	{
		location = default(Location);
		if (!Physics.Raycast(ray, out var hitInfo, 100f, (1 << Layers.Terrain) | (1 << Layers.Track)))
		{
			return false;
		}
		if (hitInfo.collider.gameObject.layer != Layers.Track)
		{
			return false;
		}
		return TrainController.Shared.graph.TryGetLocationFromWorldPoint(hitInfo.point, 1f, out location);
	}

	private void CopyLocation(Ray ray)
	{
		IPickable picked;
		float distanceToPickable;
		RaycastHit hitInfo;
		if (TryGetTrackLocation(ray, out var location))
		{
			string text = (GUIUtility.systemCopyBuffer = TrainController.Shared.graph.LocationToString(location));
			Toast.Present("Copied location: " + text);
		}
		else if (TryGetPickableUnderMouse(ray, out picked, out distanceToPickable) && picked is SwitchStandClick switchStandClick)
		{
			string text3 = (GUIUtility.systemCopyBuffer = switchStandClick.node.id);
			Toast.Present("Copied node id: " + text3);
		}
		else if (Physics.Raycast(ray, out hitInfo, 200f, 1 << Layers.Terrain))
		{
			Vector3 vector = hitInfo.point.WorldToGame();
			string text4 = (GUIUtility.systemCopyBuffer = $"({vector.x:F1}, {vector.y:F1}, {vector.z:F1})");
			Toast.Present("Copied position: " + text4);
		}
	}

	private void UpdateCalloutPosition()
	{
		bool flag = Cursor.lockState == CursorLockMode.Locked;
		bool flag2 = !_callout.IsEmpty && !flag && !UI.ContextMenu.ContextMenu.IsShown;
		_callout.gameObject.SetActive(flag2);
		if (flag2)
		{
			RectTransform rectTransform = _callout.RectTransform;
			Vector2 vector = CanvasPositioningExtensions.ScreenToCanvasPosition(screenPosition: Input.mousePosition, canvas: canvas).XY();
			Rect rect = rectTransform.rect;
			int width = Screen.width;
			rectTransform.pivot = new Vector2(0f, 1f);
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.zero;
			Vector2 anchoredPosition = vector + new Vector2(12f, -4f);
			if (anchoredPosition.x + rect.width > (float)width)
			{
				anchoredPosition.x = (float)width - rect.width;
			}
			if (anchoredPosition.y - rect.height < 0f)
			{
				anchoredPosition.y = rect.height;
			}
			rectTransform.anchoredPosition = anchoredPosition;
		}
	}

	private static bool IsMouseOnScreen()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector2 vector = new Vector2(Screen.width, Screen.height);
		if (!(mousePosition.x <= 0f) && !(mousePosition.y <= 0f) && !(mousePosition.x >= vector.x - 1f))
		{
			return !(mousePosition.y >= vector.y - 1f);
		}
		return false;
	}
}
