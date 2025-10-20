using MapEnhancer.UMM;
using Model;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UI.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapEnhancer
{
	/// <summary>
	/// Displays enhanced tooltips when hovering over car/locomotive map icons while holding ALT
	/// </summary>
	public class MapCarTooltip : MonoBehaviour
	{
		private static GameObject? _tooltipObject;
		private static RectTransform? _tooltipRect;
		private static TextMeshProUGUI? _tooltipTitle;
		private static TextMeshProUGUI? _tooltipText;
		private static Image? _tooltipBackground;
		private static CanvasGroup? _canvasGroup;

		private static Car? _currentHoveredCar;
		private static float _tooltipShowDelay = 0.3f;
		private static float _hoverTimer = 0f;
		private static bool _isShowing = false;
		private static string? _cachedTooltipText;
		private static float _cachedTooltipTextTime;
		
		// Track last mouse position to avoid redundant updates
		private static Vector2 _lastMousePosition;
		private static bool _needsPositionUpdate = false;

		private static int _lastHitCount = -1;
		private static bool _lastAltState = false;
		private static int _lastDistance = -1;
		private static float _lastUpdateLogTime = 0f;
		private static float _lastIconCountLogTime = 0f;
		private static float _lastSearchLogTime = 0f;

		/// <summary>
		/// Initialize the tooltip UI
		/// </summary>
		public static void Initialize()
		{
			Loader.Log("[MapCarTooltip] Initialize() called");

			if (_tooltipObject != null)
			{
				Loader.Log("[MapCarTooltip] Tooltip already initialized, skipping");
				return;
			}

			try
			{
				// Create tooltip GameObject
				_tooltipObject = new GameObject("MapCarTooltip");
				_tooltipObject.layer = LayerMask.NameToLayer("UI");
				_tooltipRect = _tooltipObject.AddComponent<RectTransform>();

				Loader.Log("[MapCarTooltip] Created tooltip GameObject");

				// Parent to map window
				_tooltipRect.SetParent(MapWindow.instance._window.transform, false);

				// Make sure tooltip renders on top by adding a Canvas component
				var canvas = _tooltipObject.AddComponent<Canvas>();
				canvas.overrideSorting = true;
				canvas.sortingOrder = 1000; // Very high value to ensure it's on top

				// Add GraphicRaycaster so it can receive UI events properly
				_tooltipObject.AddComponent<GraphicRaycaster>();

				_tooltipRect.anchorMin = new Vector2(0, 0);
				_tooltipRect.anchorMax = new Vector2(0, 0);
				_tooltipRect.pivot = new Vector2(0, 1);
				_tooltipRect.sizeDelta = new Vector2(300, 150);

				// Canvas group for fade in/out
				_canvasGroup = _tooltipObject.AddComponent<CanvasGroup>();
				_canvasGroup.alpha = 0f;
				_canvasGroup.interactable = false;
				_canvasGroup.blocksRaycasts = false;

				// Background
				_tooltipBackground = _tooltipObject.AddComponent<Image>();
				_tooltipBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

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
				_tooltipTitle.color = Color.gray;
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

				Loader.Log("[MapCarTooltip] Successfully initialized tooltip UI");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapCarTooltip] ERROR during initialization: {ex.Message}");
				Loader.Log($"[MapCarTooltip] Stack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Update tooltip position and visibility
		/// </summary>
		public static void Update()
		{
			// Log that we're being called (throttled)
			if (Time.unscaledTime - _lastUpdateLogTime > 2f)
			{
				//Loader.Log($"[MapCarTooltip] Update() called - tooltipObj:{_tooltipObject != null}, MapWindow:{MapWindow.instance != null}, shown:{MapWindow.instance?._window.IsShown}");
				_lastUpdateLogTime = Time.unscaledTime;
			}

			if (_tooltipObject == null || MapWindow.instance == null || !MapWindow.instance._window.IsShown)
			{
				if (_isShowing)
				{
					Loader.Log("[MapCarTooltip] Hiding tooltip - preconditions not met");
					HideTooltip();
				}
				return;
			}

			// Only show tooltip when ALT is held down
			bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

			if (!altHeld)
			{
				if (_currentHoveredCar != null || _isShowing)
				{
					Loader.Log("[MapCarTooltip] Hiding tooltip - ALT released");
				}
				HideTooltip();
				_currentHoveredCar = null;
				_hoverTimer = 0f;
				return;
			}

			// Check if mouse is over map window
			if (!MapWindow.instance.mapDrag._pointerOver)
			{
				if (_currentHoveredCar != null || _isShowing)
				{
					Loader.Log("[MapCarTooltip] Hiding tooltip - mouse not over map");
				}
				HideTooltip();
				_currentHoveredCar = null;
				_hoverTimer = 0f;
				return;
			}

			// Check if mouse has moved significantly (more than 2 pixels)
			Vector2 currentMousePos = Input.mousePosition;
			if (_isShowing && Vector2.Distance(currentMousePos, _lastMousePosition) > 2f)
			{
				_needsPositionUpdate = true;
				_lastMousePosition = currentMousePos;
			}

			// Raycast to find car under mouse
			Car? hoveredCar = GetCarUnderMouse();

			if (hoveredCar != _currentHoveredCar)
			{
				if (hoveredCar != null)
				{
					Loader.Log($"[MapCarTooltip] Now hovering over car: {hoveredCar.DisplayName}");
				}
				else if (_currentHoveredCar != null)
				{
					Loader.Log("[MapCarTooltip] No longer hovering over car");
				}

				_currentHoveredCar = hoveredCar;
				_hoverTimer = 0f;
				_needsPositionUpdate = false;

				if (hoveredCar == null)
				{
					HideTooltip();
				}
			}

			if (_currentHoveredCar != null)
			{
				_hoverTimer += Time.unscaledDeltaTime;

				if (_hoverTimer >= _tooltipShowDelay)
				{
					if (!_isShowing)
					{
						// First time showing - generate and display tooltip
						Loader.Log($"[MapCarTooltip] Showing tooltip for {_currentHoveredCar.DisplayName} after {_hoverTimer:F2}s hover");
						ShowTooltip(_currentHoveredCar);
						_lastMousePosition = currentMousePos;
						_needsPositionUpdate = false;
					}
					else if (_needsPositionUpdate)
					{
						// Tooltip is already showing, just update position
						UpdateTooltipPosition();
						_needsPositionUpdate = false;
					}
					// If already showing and mouse hasn't moved, do nothing
				}
			}
		}

		/// <summary>
		/// Find the car under the mouse cursor by checking screen distance to icons
		/// </summary>
		private static Car? GetCarUnderMouse()
		{
			try
			{
				// Use the map window's normalized mouse position (same as used for flare placement)
				var mapWindow = MapWindow.instance;
				var mapDrag = mapWindow.mapDrag;

				Vector2 normalizedMousePos = mapDrag.NormalizedMousePosition();

				// Convert normalized position to render texture pixel coordinates
				Camera mapCamera = MapBuilder.Shared.mapCamera;
				RenderTexture renderTexture = mapCamera.targetTexture;
				if (renderTexture == null)
				{
					Loader.Log("[MapCarTooltip] Map camera has no render texture");
					return null;
				}

				Vector2 textureMousePos = new Vector2(
					normalizedMousePos.x * renderTexture.width,
					normalizedMousePos.y * renderTexture.height
				);

				// Get all map icons from MapBuilder
				var mapIcons = MapBuilder.Shared._mapIcons;

				if (mapIcons == null)
				{
					Loader.Log("[MapCarTooltip] MapBuilder._mapIcons is null");
					return null;
				}

				// Log icon count (throttled)
				if (Time.unscaledTime - _lastIconCountLogTime > 5f)
				{
					Loader.Log($"[MapCarTooltip] Checking {mapIcons.Count} map icons, normalizedPos: {normalizedMousePos}, textureMousePos: {textureMousePos}");
					_lastIconCountLogTime = Time.unscaledTime;
				}

				// Find the closest car icon to the mouse in render texture space
				Car? closestCar = null;
				float closestDistance = float.MaxValue;
				const float maxDistance = 20f; // Maximum distance in pixels to consider a "hit"
				int checkedCount = 0;
				int carCount = 0;
				float minIconDist = float.MaxValue; // Track minimum distance found overall

				foreach (var mapIcon in mapIcons)
				{
					if (mapIcon == null) continue;
					checkedCount++;

					// Get the car component from the icon's parent
					var car = mapIcon.transform.parent?.GetComponent<Car>();
					if (car == null || car.IsInBardo) continue;
					carCount++;

					// Convert icon's 3D position to render texture screen space
					Vector3 iconScreenPos = mapCamera.WorldToScreenPoint(mapIcon.transform.position);

					// Calculate 2D distance in render texture space
					float distance = Vector2.Distance(textureMousePos, new Vector2(iconScreenPos.x, iconScreenPos.y));

					if (distance < minIconDist)
					{
						minIconDist = distance;
					}

					if (distance < closestDistance && distance < maxDistance)
					{
						closestDistance = distance;
						closestCar = car;
					}
				}

				// Log search results (throttled)
				if (Time.unscaledTime - _lastSearchLogTime > 5f)
				{
					Loader.Log($"[MapCarTooltip] Checked {checkedCount} icons, {carCount} cars, minDist: {minIconDist:F2}px, closest: {(closestCar != null ? $"{closestCar.DisplayName} at {closestDistance:F2}px" : "none (threshold 50px)")}");
					_lastSearchLogTime = Time.unscaledTime;
				}

				if (closestCar != null && (int)closestDistance != _lastDistance)
				{
					Loader.Log($"[MapCarTooltip] Found car {closestCar.DisplayName} at screen distance {closestDistance:F2} pixels");
					_lastDistance = (int)closestDistance;
				}

				return closestCar;
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapCarTooltip] ERROR in GetCarUnderMouse: {ex.Message}");
				Loader.Log($"[MapCarTooltip] Stack trace: {ex.StackTrace}");
			}

			return null;
		}

		/// <summary>
		/// Show the tooltip for the specified car
		/// </summary>
		private static void ShowTooltip(Car car)
		{
			if (_tooltipObject == null || car == null || _tooltipTitle == null || _tooltipText == null)
			{
				Loader.Log($"[MapCarTooltip] Cannot show tooltip - missing components: obj={_tooltipObject != null}, car={car != null}, title={_tooltipTitle != null}, text={_tooltipText != null}");
				return;
			}

			try
			{
				// Generate tooltip content
				string title = GenerateTooltipTitle(car);
				string text = GenerateTooltipText(car);

				Loader.Log($"[MapCarTooltip] Generated tooltip - Title: '{title}', Text length: {text.Length} chars");

				_tooltipTitle.text = title;
				_tooltipText.text = text;

				_tooltipObject.SetActive(true);
				if (_canvasGroup != null) _canvasGroup.alpha = 1f;
				_isShowing = true;

				// Force layout rebuild to get correct size
				if (_tooltipRect != null)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);
					Canvas.ForceUpdateCanvases();
				}

				// Update position immediately after showing
				UpdateTooltipPosition();

				Loader.Log($"[MapCarTooltip] Tooltip displayed at position {_tooltipRect?.anchoredPosition}");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapCarTooltip] ERROR in ShowTooltip: {ex.Message}");
				Loader.Log($"[MapCarTooltip] Stack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Hide the tooltip
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
		/// Update tooltip position to follow mouse
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
					Loader.Log("[MapCarTooltip] Parent rect is null!");
					return;
				}

				// Convert screen position to local position within the parent canvas
				Camera? camera = null; // UI camera (null for screen space overlay)
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
					parentRect, mousePos, camera, out Vector2 localMousePos))
				{
					Loader.Log($"[MapCarTooltip] Failed to convert screen point {mousePos} to local point");
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
					Loader.Log("[MapCarTooltip] Map camera has no render texture");
					return;
				}
				Vector2 textureMousePos = new Vector2(normalizedMousePos.x * renderTexture.width, normalizedMousePos.y * renderTexture.height);

				Loader.Log($"[MapCarTooltip] UpdatePosition - mouseScreen:{mousePos}, localMouse:{localMousePos}, textureMousePos:{textureMousePos}, tooltipSize:{tooltipSize}, parentBounds:{parentBounds}");

				// Offset tooltip from cursor (to the right and down)
				// Note: In this coordinate system, negative Y is down (single or right multi screen)
				Vector2 offset = new Vector2(0, 0); //(5, -5);
				Vector2 targetPos = offset;
				if (mousePos.x < 0)  //left multiscreen
				{
					localMousePos.x = parentBounds.width * (textureMousePos.x / 1550);
					localMousePos.y = parentBounds.height * (textureMousePos.y / 1240);    //(localMousePos.y + 45) - parentBounds.height;
					targetPos += localMousePos;

					if (targetPos.x + tooltipSize.x > parentBounds.xMax - 10)
					{
						targetPos.x = localMousePos.x - tooltipSize.x;
						Loader.Log($"[MapCarTooltip] Flipped horizontally, new x: {targetPos.x}");
					}
					//targetPos.y = localMousePos.y;
					if (targetPos.y - tooltipSize.y < 0)
					{
						targetPos.y = tooltipSize.y + 5;
						Loader.Log($"[MapCarTooltip] Flipped vertically, new y: {targetPos.y}");
					}
				}
				else  //single screen, right multiscreen
				{
					targetPos += localMousePos;
					// Flip horizontally if tooltip would go off right edge
					if (targetPos.x + tooltipSize.x > parentBounds.xMax - 10)
					{
						targetPos.x = localMousePos.x - tooltipSize.x;
						Loader.Log($"[MapCarTooltip] Flipped horizontally, new x: {targetPos.x}");
					}

					// Flip vertically if tooltip would go off bottom edge
					if (targetPos.y - tooltipSize.y < parentBounds.yMin + 10)
					{
						targetPos.y = localMousePos.y + tooltipSize.y;
						Loader.Log($"[MapCarTooltip] Flipped vertically, new y: {targetPos.y}");
					}

					// Clamp to stay within bounds
					float clampedX = targetPos.x; //Mathf.Clamp(targetPos.x, parentBounds.xMin + 10, parentBounds.xMax - tooltipSize.x - 10);
					float clampedY = targetPos.y; //Mathf.Clamp(targetPos.y, parentBounds.yMin + tooltipSize.y + 10, parentBounds.yMax - 10);

					if (clampedX != targetPos.x || clampedY != targetPos.y)
					{
						Loader.Log($"[MapCarTooltip] Clamped position from ({targetPos.x}, {targetPos.y}) to ({clampedX}, {clampedY})");
					}

					targetPos.x = clampedX;
					targetPos.y = parentBounds.height + clampedY; // Singlescreen y negative (reverted)
				}

				_tooltipRect.anchoredPosition = targetPos;
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapCarTooltip] ERROR in UpdateTooltipPosition: {ex.Message}");
				Loader.Log($"[MapCarTooltip] Stack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Generate tooltip title
		/// </summary>
		private static string GenerateTooltipTitle(Car car)
		{
			return car.DisplayName;
		}

		/// <summary>
		/// Generate tooltip text content
		/// </summary>
		private static string GenerateTooltipText(Car car)
		{
			float unscaledTime = Time.unscaledTime;

			// Cache tooltip for 1 second to avoid recalculation
			if (_cachedTooltipText != null && _cachedTooltipTextTime + 1f > unscaledTime && _currentHoveredCar == car)
			{
				return _cachedTooltipText;
			}

			List<string> lines = new List<string>();

			try
			{
				// Destination info
				OpsController opsController = OpsController.Shared;
				if (opsController != null && opsController.TryGetDestinationInfo(car,
					out string destinationName, out bool isAtDestination, out _, out _))
				{
					string item = (isAtDestination ? "<sprite name=\"Spotted\">" : "<sprite name=\"Destination\">") + " " + destinationName;
					lines.Add(item);
				}

				// Train name
				if (car.TryGetTrainName(out string trainName))
				{
					lines.Add($"Train: {trainName}");
				}

				// For locomotives, show fuel/water info
				if (car.IsLocomotive)
				{
					AddLocomotiveInfo(car, lines);
				}

				// Load info for freight/tender cars
				bool hasLoadSlots = car.Definition.LoadSlots.Count > 0;
				bool hasLoadInfo = false;
				StringBuilder stringBuilder = new StringBuilder();

				if (hasLoadSlots && !car.IsLocomotive)  // Skip locomotives - they're handled in AddLocomotiveInfo
				{
					foreach (var slotData in car.Definition.DisplayOrderLoadSlots())
					{
						LoadSlot slot = slotData.slot;
						int index = slotData.index;
						CarLoadInfo? loadInfo = car.GetLoadInfo(index);

						if (loadInfo.HasValue)
						{
							CarLoadInfo value = loadInfo.Value;
							Load? load = CarPrototypeLibrary.instance.LoadForId(value.LoadId);

							if (load != null)
							{
								// Shows pie chart percentage + quantity
								stringBuilder.Append(TextSprites.PiePercent(value.Quantity, slot.MaximumCapacity));
								stringBuilder.Append(" ");
								stringBuilder.Append(value.LoadString(load));
								lines.Add(stringBuilder.ToString());
								stringBuilder.Clear();  // Clear for next iteration
								hasLoadInfo = true;
							}
						}
					}
				}

				// Show "Empty" if has slots but nothing loaded
				if (hasLoadSlots && !hasLoadInfo && !car.IsLocomotive)
				{
					lines.Add(TextSprites.PiePercent(0f, 1f) + " Empty");
				}

				// Passenger info
				if (car.IsPassengerCar())
				{
					PassengerMarker? passengerMarker = car.GetPassengerMarker();
					if (passengerMarker.HasValue)
					{
						string passengerInfo = GeneratePassengerString(car, passengerMarker.Value);
						lines.Add(passengerInfo);
					}
				}

				// Handbrake status
				if (car.air.handbrakeApplied)
				{
					lines.Add("<sprite name=\"HandbrakeWheel\"> Handbrake");
				}

				// Hotbox warning
				if (car.HasHotbox)
				{
					lines.Add("<sprite name=\"Flame\"> Hotbox");
				}

				Loader.Log($"[MapCarTooltip] Generated {lines.Count} lines of tooltip text for {car.DisplayName}");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapCarTooltip] ERROR generating tooltip text: {ex.Message}");
				lines.Add($"Error: {ex.Message}");
			}

			string result = _cachedTooltipText = string.Join("\n", lines);
			_cachedTooltipTextTime = unscaledTime;
			return result;
		}

		/// <summary>
		/// Add locomotive-specific information (fuel, water, etc.)
		/// </summary>
		private static void AddLocomotiveInfo(Car car, List<string> lines)
		{
			if (car is BaseLocomotive loco)
			{
				// Add speed info
				float speedMph = Mathf.Abs(loco.velocity) * 2.23694f; // m/s to mph
				if (speedMph > 0.5f)
				{
					lines.Add($"Speed: {speedMph:F1} mph");
				}

				// Steam locomotive specific info
				if (car is SteamLocomotive steamLoco)
				{
					AddSteamLocoInfo(steamLoco, lines);
				}
				// Diesel locomotive specific info
				else if (car is DieselLocomotive dieselLoco)
				{
					AddDieselLocoInfo(dieselLoco, lines);
				}
			}
		}

		/// <summary>
		/// Add steam locomotive fuel and water info
		/// </summary>
		private static void AddSteamLocoInfo(SteamLocomotive steamLoco, List<string> lines)
		{
			// Get the fuel car (locomotive or tender)
			Car? fuelCar = steamLoco.FuelCar();
			if (fuelCar == null)
			{
				Loader.Log($"[MapCarTooltip] Steam loco {steamLoco.DisplayName} has no fuel car");
				return;
			}

			// Find coal and water slots
			int coalSlot = fuelCar.Definition.LoadSlots.FindIndex(slot => slot.RequiredLoadIdentifier == "coal");
			int waterSlot = fuelCar.Definition.LoadSlots.FindIndex(slot => slot.RequiredLoadIdentifier == "water");

			Loader.Log($"[MapCarTooltip] Steam loco slots - coal:{coalSlot}, water:{waterSlot}");

			// Coal info
			if (coalSlot >= 0)
			{
				CarLoadInfo? coalInfo = fuelCar.GetLoadInfo(coalSlot);
				if (coalInfo.HasValue)
				{
					LoadSlot slot = fuelCar.Definition.LoadSlots[coalSlot];
					Loader.Log($"[MapCarTooltip] Steam loco slots - coal:{coalInfo.Value.Quantity}, water:{waterSlot}");
					float coal = coalInfo.Value.Quantity/2000;
					lines.Add(TextSprites.PiePercent(coalInfo.Value.Quantity, slot.MaximumCapacity) + " " + coal.ToString("F1") + "T Coal");
				}
			}

			// Water info
			if (waterSlot >= 0)
			{
				CarLoadInfo? waterInfo = fuelCar.GetLoadInfo(waterSlot);
				if (waterInfo.HasValue)
				{
					LoadSlot slot = fuelCar.Definition.LoadSlots[waterSlot];
					lines.Add(TextSprites.PiePercent(waterInfo.Value.Quantity, slot.MaximumCapacity) + " " + waterInfo.Value.Quantity.ToString("F0") + " gal Water");
				}
			}
		}

		/// <summary>
		/// Add diesel locomotive fuel info
		/// </summary>
		private static void AddDieselLocoInfo(DieselLocomotive dieselLoco, List<string> lines)
		{
			// Find diesel fuel slot
			int dieselSlot = dieselLoco.Definition.LoadSlots.FindIndex(slot => slot.RequiredLoadIdentifier == "diesel");

			Loader.Log($"[MapCarTooltip] Diesel loco slot - diesel:{dieselSlot}");

			if (dieselSlot >= 0)
			{
				CarLoadInfo? dieselInfo = dieselLoco.GetLoadInfo(dieselSlot);
				if (dieselInfo.HasValue)
				{
					LoadSlot slot = dieselLoco.Definition.LoadSlots[dieselSlot];
					lines.Add(TextSprites.PiePercent(dieselInfo.Value.Quantity, slot.MaximumCapacity) + " Diesel: " + dieselInfo.Value.Quantity.ToString("F0") + " gal");
				}
			}
		}

		/// <summary>
		/// Generate passenger car info string
		/// </summary>
		private static string GeneratePassengerString(Car car, PassengerMarker marker)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(car.PassengerCountString(marker));
			sb.Append(" Passengers");

			HashSet<string> destinations = (from g in marker.Groups
											where g.Count > 0
											select PassengerStop.ShortNameForIdentifier(g.Destination)).ToHashSet();

			if (destinations.Count > 0)
			{
				sb.Append(" → ");
				sb.Append(string.Join(", ", destinations));
			}

			return sb.ToString();
		}

		/// <summary>
		/// Cleanup tooltip on destroy
		/// </summary>
		public static void Cleanup()
		{
			Loader.Log("[MapCarTooltip] Cleanup() called");

			if (_tooltipObject != null)
			{
				Object.Destroy(_tooltipObject);
				_tooltipObject = null;
				_tooltipRect = null;
				_tooltipTitle = null;
				_tooltipText = null;
				_tooltipBackground = null;
				_canvasGroup = null;

				Loader.Log("[MapCarTooltip] Tooltip destroyed");
			}

			_currentHoveredCar = null;
			_isShowing = false;
			_needsPositionUpdate = false;
			_lastMousePosition = Vector2.zero;
		}
	}
}
