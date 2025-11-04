using MapEnhancer.UMM;
using Model.Ops;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Track;
using Helpers;
using UI.Map;
using Map.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace MapEnhancer
{
	/// <summary>
	/// Displays tooltips when hovering over passenger station track segments while holding SHIFT
	/// Shows waiting passengers, potential passengers, and destination information
	/// </summary>
	public class MapPassengerTooltip : MonoBehaviour
	{
		private static GameObject? _tooltipObject;
		private static RectTransform? _tooltipRect;
		private static TextMeshProUGUI? _tooltipTitle;
		private static TextMeshProUGUI? _tooltipText;
		private static Image? _tooltipBackground;
		private static CanvasGroup? _canvasGroup;

		private static PassengerStop? _currentHoveredStation;
		private static TrackSegment? _currentHoveredSegment;
		private static float _tooltipShowDelay = 0.3f;
		private static float _hoverTimer = 0f;
		private static bool _isShowing = false;
		private static string? _cachedTooltipText;
		private static float _cachedTooltipTextTime;
		
		// Track last mouse position to avoid redundant updates
		private static Vector2 _lastMousePosition;
		private static bool _needsPositionUpdate = false;

		// Dictionary mapping track segment IDs to passenger stops
		private static Dictionary<string, PassengerStop> _segmentToPassengerStop = new Dictionary<string, PassengerStop>();
		
		private static float _lastUpdateLogTime = 0f;

		/// <summary>
		/// Initialize the tooltip UI
		/// </summary>
		public static void Initialize()
		{
			Loader.Log("[MapPassengerTooltip] Initialize() called");

			if (_tooltipObject != null)
			{
				Loader.Log("[MapPassengerTooltip] Tooltip already initialized, skipping");
				return;
			}

			try
			{
				// Create tooltip GameObject
				_tooltipObject = new GameObject("MapPassengerTooltip");
				_tooltipObject.layer = LayerMask.NameToLayer("UI");
				_tooltipRect = _tooltipObject.AddComponent<RectTransform>();

				Loader.Log("[MapPassengerTooltip] Created tooltip GameObject");

				// Parent to map window
				_tooltipRect.SetParent(MapWindow.instance._window.transform, false);

				// Make sure tooltip renders on top
				var canvas = _tooltipObject.AddComponent<Canvas>();
				canvas.overrideSorting = true;
				canvas.sortingOrder = 1002; // Higher than industry tooltip (1001)

				_tooltipObject.AddComponent<GraphicRaycaster>();

				_tooltipRect.anchorMin = new Vector2(0, 0);
				_tooltipRect.anchorMax = new Vector2(0, 0);
				_tooltipRect.pivot = new Vector2(0, 1);
				_tooltipRect.sizeDelta = new Vector2(350, 200);

				// Canvas group for fade in/out
				_canvasGroup = _tooltipObject.AddComponent<CanvasGroup>();
				_canvasGroup.alpha = 0f;
				_canvasGroup.interactable = false;
				_canvasGroup.blocksRaycasts = false;

				// Background
				_tooltipBackground = _tooltipObject.AddComponent<Image>();
				_tooltipBackground.color = new Color(0.05f, 0.1f, 0.2f, 0.95f); // Blueish tint for passenger

				// Add vertical layout
				var layout = _tooltipObject.AddComponent<VerticalLayoutGroup>();
				layout.padding = new RectOffset(10, 10, 8, 8);
				layout.spacing = 4;
				layout.childControlWidth = true;
				layout.childControlHeight = true;
				layout.childForceExpandWidth = true;
				layout.childForceExpandHeight = false;

				// Content size fitter
				var fitter = _tooltipObject.AddComponent<ContentSizeFitter>();
				fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
				fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

				// Title text
				var titleGo = new GameObject("Title");
				titleGo.transform.SetParent(_tooltipObject.transform, false);
				_tooltipTitle = titleGo.AddComponent<TextMeshProUGUI>();
				_tooltipTitle.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "Cabin-Bold SDF");
				_tooltipTitle.fontSize = 14;
				_tooltipTitle.fontStyle = FontStyles.Bold;
				_tooltipTitle.color = new Color(0.6f, 0.8f, 1f); // Light blue for passenger
				_tooltipTitle.alignment = TextAlignmentOptions.Left;
				_tooltipTitle.enableWordWrapping = false;

				// Body text
				var textGo = new GameObject("Text");
				textGo.transform.SetParent(_tooltipObject.transform, false);
				_tooltipText = textGo.AddComponent<TextMeshProUGUI>();
				_tooltipText.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "Cabin-Bold SDF");
				_tooltipText.fontSize = 12;
				_tooltipText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
				_tooltipText.alignment = TextAlignmentOptions.Left;
				_tooltipText.enableWordWrapping = true;

				_tooltipObject.SetActive(false);

				// Build segment to passenger stop mapping
				BuildSegmentMapping();

				Loader.Log("[MapPassengerTooltip] Successfully initialized tooltip UI");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapPassengerTooltip] ERROR during initialization: {ex.Message}");
				Loader.Log($"[MapPassengerTooltip] Stack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Build mapping of track segment IDs to passenger stops
		/// </summary>
		private static void BuildSegmentMapping()
		{
			_segmentToPassengerStop.Clear();

			var allPassengerStops = PassengerStop.FindAll().ToArray();
			Loader.Log($"[MapPassengerTooltip] Found {allPassengerStops.Length} passenger stops");

			int totalSegmentsMapped = 0;

			foreach (var stop in allPassengerStops)
			{
				if (stop == null) continue;

				// Get all track spans for this passenger stop
				var trackSpans = stop.GetComponentsInChildren<TrackSpan>();
				Loader.Log($"[MapPassengerTooltip] PassengerStop '{stop.DisplayName}' has {trackSpans.Length} track spans");
				
				foreach (var span in trackSpans)
				{
					if (span == null) continue;

					// Get all segments in this span
					var segments = span.GetSegments();
					Loader.Log($"[MapPassengerTooltip]   TrackSpan has {segments.Count} segments");
					
					foreach (var segment in segments)
					{
						if (segment != null && !string.IsNullOrEmpty(segment.id))
						{
							// Map this segment to the passenger stop
							if (!_segmentToPassengerStop.ContainsKey(segment.id))
							{
								_segmentToPassengerStop[segment.id] = stop;
								totalSegmentsMapped++;
								Loader.Log($"[MapPassengerTooltip]     Mapped segment {segment.id} to {stop.DisplayName}");
							}
							else
							{
								Loader.Log($"[MapPassengerTooltip]     Segment {segment.id} already mapped");
							}
						}
					}
				}
			}

			Loader.Log($"[MapPassengerTooltip] Built mapping with {_segmentToPassengerStop.Count} track segments (total mapped: {totalSegmentsMapped})");
		}

		/// <summary>
		/// Check if tooltip is currently showing
		/// </summary>
		public static bool IsShowing()
		{
			return _isShowing;
		}

		/// <summary>
		/// Update tooltip position and visibility
		/// </summary>
		public static void Update()
		{
			// Don't show passenger tooltip if car tooltip or industry tooltip is already showing
			if (MapCarTooltip.IsShowing() || MapIndustryTooltip.IsShowing())
			{
				if (_isShowing)
				{
					HideTooltip();
				}
				_currentHoveredStation = null;
				_hoverTimer = 0f;
				return;
			}

			if (_tooltipObject == null || MapWindow.instance == null || !MapWindow.instance._window.IsShown)
			{
				if (_isShowing)
				{
					HideTooltip();
				}
				return;
			}

			// Only show tooltip when SHIFT is held down
			bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

			if (!altHeld)
			{
				HideTooltip();
				_currentHoveredStation = null;
				_hoverTimer = 0f;
				return;
			}

			// Check if mouse is over map window
			if (!MapWindow.instance.mapDrag._pointerOver)
			{
				HideTooltip();
				_currentHoveredStation = null;
				_hoverTimer = 0f;
				return;
			}

			// Check if mouse has moved significantly
			Vector2 currentMousePos = Input.mousePosition;
			if (_isShowing && Vector2.Distance(currentMousePos, _lastMousePosition) > 2f)
			{
				_needsPositionUpdate = true;
				_lastMousePosition = currentMousePos;
			}

			// Find passenger stop under mouse
			PassengerStop? hoveredStation = GetPassengerStopUnderMouse();

			if (hoveredStation != _currentHoveredStation)
			{
				if (hoveredStation != null)
				{
					Loader.Log($"[MapPassengerTooltip] Now hovering over station: {hoveredStation.DisplayName}");
				}
				else if (_currentHoveredStation != null)
				{
					Loader.Log("[MapPassengerTooltip] No longer hovering over station");
				}

				_currentHoveredStation = hoveredStation;
				_hoverTimer = 0f;
				_needsPositionUpdate = false;

				if (hoveredStation == null)
				{
					HideTooltip();
				}
			}

			if (_currentHoveredStation != null)
			{
				_hoverTimer += Time.unscaledDeltaTime;

				if (_hoverTimer >= _tooltipShowDelay)
				{
					if (!_isShowing)
					{
						Loader.Log($"[MapPassengerTooltip] Showing tooltip for {_currentHoveredStation.DisplayName}");
						ShowTooltip(_currentHoveredStation);
						_lastMousePosition = currentMousePos;
						_needsPositionUpdate = false;
					}
					else if (_needsPositionUpdate)
					{
						UpdateTooltipPosition();
						_needsPositionUpdate = false;
					}
				}
			}
		}

		/// <summary>
		/// Find passenger stop under mouse by raycasting to track segments
		/// </summary>
		private static PassengerStop? GetPassengerStopUnderMouse()
		{
			try
			{
				var mapWindow = MapWindow.instance;
				var mapDrag = mapWindow.mapDrag;

				// Get mouse position in world space
				Vector2 normalizedMousePos = mapDrag.NormalizedMousePosition();
				Ray ray = mapWindow.RayForViewportNormalizedPoint(normalizedMousePos);
				
				// Convert ray to game coordinates using MapManager
				Vector3 worldPoint = ray.origin;
				Vector3 gamePoint = MapManager.Instance.FindTerrainPointForXZ(WorldTransformer.WorldToGame(worldPoint));

				// Find nearby track segments
				float searchRadius = 100 + (MapBuilder.Shared.mapCamera.orthographicSize / 10 ); // Larger radius when zoomed out
				
				TrackSegment? closestSegment = null;
				float closestDistance = float.MaxValue;

				foreach (var segmentKvp in Graph.Shared.segments)
				{
					var segment = segmentKvp.Value;
					if (segment == null) continue;

					// Check if this segment is mapped to a passenger stop
					if (!_segmentToPassengerStop.ContainsKey(segment.id))
						continue;

					// Get segment endpoints using Location
					Location locA = new Location(segment, 0f, TrackSegment.End.A);
					Location locB = new Location(segment, segment.GetLength(), TrackSegment.End.A);
					Vector3 pointA = locA.GetPosition();
					Vector3 pointB = locB.GetPosition();

					// Calculate distance from mouse to segment
					float distance = DistanceToLineSegment(gamePoint, pointA, pointB);
					if (distance < searchRadius && distance < closestDistance)
					{
						closestDistance = distance;
						closestSegment = segment;
					}
				}

				if (closestSegment != null && _segmentToPassengerStop.TryGetValue(closestSegment.id, out PassengerStop stop))
				{
					// Store the closest segment for use in tooltip generation
					_currentHoveredSegment = closestSegment;
					return stop;
				}
				
				_currentHoveredSegment = null;
				return null;
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapPassengerTooltip] ERROR in GetPassengerStopUnderMouse: {ex.Message}");
				Loader.Log($"[MapPassengerTooltip] Stack trace: {ex.StackTrace}");
			}

			return null;
		}

		/// <summary>
		/// Calculate distance from point to line segment
		/// </summary>
		private static float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
		{
			Vector3 line = lineEnd - lineStart;
			float lineLength = line.magnitude;
			
			if (lineLength < 0.001f)
				return Vector3.Distance(point, lineStart);

			Vector3 lineDirection = line / lineLength;
			Vector3 toPoint = point - lineStart;
			
			float dot = Vector3.Dot(toPoint, lineDirection);
			dot = Mathf.Clamp(dot, 0f, lineLength);
			
			Vector3 closestPoint = lineStart + lineDirection * dot;
			return Vector3.Distance(point, closestPoint);
		}

		/// <summary>
		/// Show tooltip for passenger stop
		/// </summary>
		private static void ShowTooltip(PassengerStop stop)
		{
			if (_tooltipObject == null || stop == null || _tooltipTitle == null || _tooltipText == null)
			{
				Loader.Log($"[MapPassengerTooltip] Cannot show tooltip - missing components");
				return;
			}

			try
			{
				string title = GenerateTooltipTitle(stop);
				string text = GenerateTooltipText(stop);

				Loader.Log($"[MapPassengerTooltip] Generated tooltip - Title: '{title}', Text length: {text.Length} chars");

				_tooltipTitle.text = title;
				_tooltipText.text = text;

				_tooltipObject.SetActive(true);
				if (_canvasGroup != null) _canvasGroup.alpha = 1f;
				_isShowing = true;

				// Force layout rebuild
				if (_tooltipRect != null)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);
					Canvas.ForceUpdateCanvases();
				}

				UpdateTooltipPosition();

				Loader.Log($"[MapPassengerTooltip] Tooltip displayed at position {_tooltipRect?.anchoredPosition}");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapPassengerTooltip] ERROR in ShowTooltip: {ex.Message}");
			}
		}

		/// <summary>
		/// Hide tooltip
		/// </summary>
		private static void HideTooltip()
		{
			if (_tooltipObject != null)
			{
				if (_canvasGroup != null) _canvasGroup.alpha = 0f;
				_tooltipObject.SetActive(false);
			}
			_isShowing = false;
		}

		/// <summary>
		/// Update tooltip position (same logic as MapCarTooltip and MapIndustryTooltip)
		/// </summary>
		private static void UpdateTooltipPosition()
		{
			if (_tooltipObject == null || !_isShowing || _tooltipRect == null) return;

			try
			{
				// Get mouse position in screen space
				Vector2 mousePos = Input.mousePosition;

				// Get the parent rect to know the bounds
				RectTransform? parentRect = _tooltipRect.parent as RectTransform;
				if (parentRect == null)
				{
					Loader.Log("[MapPassengerTooltip] Parent rect is null!");
					return;
				}

				// Convert screen position to local position within the parent canvas
				Camera? camera = null; // UI camera (null for screen space overlay)
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
					parentRect, mousePos, camera, out Vector2 localMousePos))
				{
					Loader.Log($"[MapPassengerTooltip] Failed to convert screen point {mousePos} to local point");
					return;
				}

				// Force layout update to ensure we have the correct size
				LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);

				// Get tooltip size for boundary checking
				Vector2 tooltipSize = _tooltipRect.rect.size;

				// Get parent bounds
				Rect parentBounds = parentRect.rect;

				var mapWindow = MapWindow.instance;
				var mapDrag = mapWindow.mapDrag;
				Vector2 normalizedMousePos = mapDrag.NormalizedMousePosition();
				// Convert normalized position to render texture pixel coordinates
				Camera mapCamera = MapBuilder.Shared.mapCamera;
				RenderTexture renderTexture = mapCamera.targetTexture;
				if (renderTexture == null)
				{
					Loader.Log("[MapPassengerTooltip] Map camera has no render texture");
					return;
				}
				Vector2 textureMousePos = new Vector2(normalizedMousePos.x * renderTexture.width, normalizedMousePos.y * renderTexture.height);

				Loader.Log($"[MapPassengerTooltip] UpdatePosition - mouseScreen:{mousePos}, localMouse:{localMousePos}, textureMousePos:{textureMousePos}, tooltipSize:{tooltipSize}, parentBounds:{parentBounds}");

				// Offset tooltip from cursor
				Vector2 offset = new Vector2(0, 0);
				Vector2 targetPos = offset;
				if (mousePos.x < 0)  // left multiscreen
				{
					localMousePos.x = parentBounds.width * (textureMousePos.x / 1550);
					localMousePos.y = parentBounds.height * (textureMousePos.y / 1240);
					targetPos += localMousePos;

					if (targetPos.x + tooltipSize.x > parentBounds.xMax - 10)
					{
						targetPos.x = localMousePos.x - tooltipSize.x;
						Loader.Log($"[MapPassengerTooltip] Flipped horizontally, new x: {targetPos.x}");
					}
					if (targetPos.y - tooltipSize.y < 0)
					{
						targetPos.y = tooltipSize.y + 5;
						Loader.Log($"[MapPassengerTooltip] Flipped vertically, new y: {targetPos.y}");
					}
				}
				else  // single screen, right multiscreen
				{
					targetPos += localMousePos;
					// Flip horizontally if tooltip would go off right edge
					if (targetPos.x + tooltipSize.x > parentBounds.xMax - 10)
					{
						targetPos.x = localMousePos.x - tooltipSize.x;
						Loader.Log($"[MapPassengerTooltip] Flipped horizontally, new x: {targetPos.x}");
					}

					// Flip vertically if tooltip would go off bottom edge
					if (targetPos.y - tooltipSize.y < parentBounds.yMin + 10)
					{
						targetPos.y = localMousePos.y + tooltipSize.y;
						Loader.Log($"[MapPassengerTooltip] Flipped vertically, new y: {targetPos.y}");
					}

					// Clamp to stay within bounds
					float clampedX = targetPos.x;
					float clampedY = targetPos.y;

					if (clampedX != targetPos.x || clampedY != targetPos.y)
					{
						Loader.Log($"[MapPassengerTooltip] Clamped position from ({targetPos.x}, {targetPos.y}) to ({clampedX}, {clampedY})");
					}

					targetPos.x = clampedX;
					targetPos.y = parentBounds.height + clampedY; // Singlescreen y negative (reverted)
				}

				_tooltipRect.anchoredPosition = targetPos;
				Loader.Log($"[MapPassengerTooltip] Final Position ({targetPos.x}, {targetPos.y})");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapPassengerTooltip] ERROR in UpdateTooltipPosition: {ex.Message}");
				Loader.Log($"[MapPassengerTooltip] Stack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Generate tooltip title
		/// </summary>
		private static string GenerateTooltipTitle(PassengerStop stop)
		{
			return stop.DisplayName;
		}

		/// <summary>
		/// Generate tooltip text showing passenger information
		/// </summary>
		private static string GenerateTooltipText(PassengerStop stop)
		{
			float unscaledTime = Time.unscaledTime;

			// Cache for 1 second
			if (_cachedTooltipText != null && _cachedTooltipTextTime + 1f > unscaledTime && _currentHoveredStation == stop)
			{
				return _cachedTooltipText;
			}

			List<string> lines = new List<string>();

			try
			{
				// Station capacity info
				lines.Add($"<b>Station Capacity:</b>");
				lines.Add($"  Base Population: {stop.basePopulation}");
				
				if (stop.AdditionalPopulation > 0)
				{
					lines.Add($"  Additional Population: +{stop.AdditionalPopulation}");
				}
				
				lines.Add($"  Max Capacity: {stop.basePopulation + stop.AdditionalPopulation}");

				// Calculate total waiting passengers
				int totalWaiting = 0;
				foreach (var waitingKvp in stop.Waiting)
				{
					totalWaiting += waitingKvp.Value.Total;
				}

				lines.Add("");
				lines.Add($"<b>Total Waiting: {totalWaiting}</b>");

				// Show waiting passengers by destination
				if (stop.Waiting.Count > 0)
				{
					lines.Add("");
					lines.Add("<b>Waiting by Destination:</b>");
					
					// Sort destinations by passenger count (descending)
					var sortedWaiting = stop.Waiting
						.OrderByDescending(kvp => kvp.Value.Total)
						.ToList();
					
					foreach (var waitingKvp in sortedWaiting)
					{
						string destName = PassengerStop.ShortNameForIdentifier(waitingKvp.Key);
						int count = waitingKvp.Value.Total;
						
						if (count > 0)
						{
							lines.Add($"  → {destName}: {count}");
							
							// Show origin breakdown for this destination (if multiple origins)
							var groups = waitingKvp.Value.Groups;
							if (groups.Count > 1)
							{
								foreach (var group in groups.OrderByDescending(g => g.Count))
								{
									if (group.Origin != stop.identifier)
									{
										string originName = PassengerStop.ShortNameForIdentifier(group.Origin);
										lines.Add($"      from {originName}: {group.Count}");
									}
								}
							}
						}
					}
				}
				else
				{
					lines.Add("  No passengers waiting");
				}

				// Show flag stop status
				if (stop.flagStop)
				{
					lines.Add("");
					lines.Add("<i>Flag Stop</i>");
				}

				// Show last served time if available
				var lastServed = stop.LastServed;
				if (lastServed.HasValue)
				{
					lines.Add("");
					lines.Add($"Last Served: {lastServed.Value.TimeString()}");
				}
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapPassengerTooltip] ERROR generating tooltip: {ex.Message}");
				lines.Add($"Error: {ex.Message}");
			}

			string result = _cachedTooltipText = string.Join("\n", lines);
			_cachedTooltipTextTime = unscaledTime;
			return result;
		}

		/// <summary>
		/// Rebuild segment mapping (call when passenger stops change)
		/// </summary>
		public static void RebuildMapping()
		{
			Loader.Log("[MapPassengerTooltip] Rebuilding segment mapping");
			BuildSegmentMapping();
		}

		/// <summary>
		/// Cleanup
		/// </summary>
		public static void Cleanup()
		{
			Loader.Log("[MapPassengerTooltip] Cleanup() called");

			if (_tooltipObject != null)
			{
				Object.Destroy(_tooltipObject);
				_tooltipObject = null;
				_tooltipRect = null;
				_tooltipTitle = null;
				_tooltipText = null;
				_tooltipBackground = null;
				_canvasGroup = null;
			}

			_currentHoveredStation = null;
			_isShowing = false;
			_needsPositionUpdate = false;
			_lastMousePosition = Vector2.zero;
			_segmentToPassengerStop.Clear();

			Loader.Log("[MapPassengerTooltip] Tooltip destroyed");
		}
	}
}
