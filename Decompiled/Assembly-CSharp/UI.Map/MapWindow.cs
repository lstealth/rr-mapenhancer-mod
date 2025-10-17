using Helpers;
using Map.Runtime;
using Serilog;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Map;

[RequireComponent(typeof(Window))]
public class MapWindow : MonoBehaviour
{
	private Window _window;

	[SerializeField]
	private MapDrag mapDrag;

	[SerializeField]
	private RawImage rawImage;

	[SerializeField]
	private MapBuilder mapBuilder;

	private Vector3 _cameraPositionAtDragStart;

	private RenderTexture _renderTexture;

	private bool _hasSetMapCenter;

	private static MapWindow instance => WindowManager.Shared.GetWindow<MapWindow>();

	public static void Show()
	{
		instance._Show(null);
	}

	public static void Show(Vector3 gamePosition)
	{
		instance._Show(gamePosition);
	}

	public static void Toggle()
	{
		if (instance._window.IsShown)
		{
			instance._window.CloseWindow();
		}
		else
		{
			instance._Show(null);
		}
	}

	private void _Show(Vector3? gamePosition)
	{
		if (gamePosition.HasValue)
		{
			mapBuilder.SetMapCenter(gamePosition.Value);
		}
		else if (!_hasSetMapCenter)
		{
			mapBuilder.SetMapCenter(CameraSelector.shared.CurrentCameraPosition);
		}
		_hasSetMapCenter = true;
		_window.Title = "Map";
		_window.ShowWindow();
	}

	private void Start()
	{
		_window = GetComponent<Window>();
		_window.CloseWindow();
		_window.OnShownDidChange += OnWindowShown;
		_window.OnDidResize += OnWindowResize;
		Vector2Int vector2Int = new Vector2Int(600, 500);
		_window.SetInitialPositionSize("Map", vector2Int, Window.Position.UpperLeft, Window.Sizing.Resizable(vector2Int));
		SetupRenderTexture();
		mapDrag.OnDragStart = OnDragStart;
		mapDrag.OnDragChange = OnDragChange;
		mapDrag.OnZoom = OnZoom;
		mapDrag.OnTeleport = OnTeleport;
		mapDrag.OnClick = OnClick;
		mapBuilder.SetVisible(visible: false);
	}

	private void OnDestroy()
	{
		CleanupRenderTexture();
	}

	private void OnWindowShown(bool shown)
	{
		if (shown)
		{
			mapBuilder.SetVisible(visible: true);
			mapBuilder.Zoom(0f, Vector3.zero);
			mapBuilder.Rebuild();
		}
		else
		{
			mapBuilder.SetVisible(visible: false);
		}
	}

	private void OnWindowResize(Vector2 size)
	{
		SetupRenderTexture();
	}

	private void OnDragStart()
	{
		_cameraPositionAtDragStart = mapBuilder.mapCamera.transform.localPosition;
	}

	private void OnDragChange(Vector2 delta)
	{
		float num = mapBuilder.mapCamera.orthographicSize * 2f / mapDrag.RectHeight;
		mapBuilder.mapCamera.transform.localPosition = _cameraPositionAtDragStart + new Vector3(delta.x, 0f, delta.y) * num;
	}

	private void OnZoom(float delta, Vector2 viewportNormalizedPoint)
	{
		mapBuilder.Zoom(delta, viewportNormalizedPoint);
	}

	private void OnTeleport(Vector2 viewportNormalizedPoint)
	{
		Ray ray = RayForViewportNormalizedPoint(viewportNormalizedPoint);
		Vector3 gamePoint = MapManager.Instance.FindTerrainPointForXZ(WorldTransformer.WorldToGame(ray.origin));
		CameraSelector.shared.JumpToPoint(gamePoint, Quaternion.identity);
	}

	private Ray RayForViewportNormalizedPoint(Vector2 viewportNormalizedPoint)
	{
		return mapBuilder.mapCamera.ViewportPointToRay(new Vector3(viewportNormalizedPoint.x, viewportNormalizedPoint.y, 0f));
	}

	private void OnClick(Vector2 viewportNormalizedPoint)
	{
		Ray ray = RayForViewportNormalizedPoint(viewportNormalizedPoint);
		float y = mapBuilder.mapCamera.transform.position.y;
		if (Physics.Raycast(ray, out var hitInfo, y, 1 << Layers.Map))
		{
			IMapClickable componentInParent = hitInfo.collider.transform.GetComponentInParent<IMapClickable>();
			if (componentInParent != null)
			{
				componentInParent.Click();
			}
			else
			{
				Debug.LogWarning($"MapClick: Collider has no MapIcon parent: {hitInfo.collider}", hitInfo.collider);
			}
		}
	}

	private void SetupRenderTexture()
	{
		CleanupRenderTexture();
		Rect rect = _window.contentRectTransform.rect;
		int num = (int)rect.width;
		int num2 = (int)rect.height;
		Log.Debug("MapWindow RenderTexture: {w} x {h}", num, num2);
		if (_renderTexture != null)
		{
			_renderTexture.Release();
		}
		_renderTexture = new RenderTexture(num, num2, 16);
		rawImage.texture = _renderTexture;
		mapBuilder.mapCamera.targetTexture = _renderTexture;
		mapBuilder.windowSizeFactor = (float)Mathf.Min(num, num2) / 500f;
	}

	private void CleanupRenderTexture()
	{
		if (!(_renderTexture == null))
		{
			if (rawImage != null)
			{
				rawImage.texture = null;
			}
			if (mapBuilder != null && mapBuilder.mapCamera != null)
			{
				mapBuilder.mapCamera.targetTexture = null;
			}
			_renderTexture.Release();
			_renderTexture = null;
		}
	}

	public void ClickLocateMe()
	{
		mapBuilder.SetMapCenter(CameraSelector.shared.CurrentCameraPosition);
	}
}
