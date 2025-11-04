using MapEnhancer.UMM;
using Model.Ops;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
	/// Displays tooltips when hovering over industry track segments while holding ALT
	/// Shows storage levels and basic industry information
	/// </summary>
	public class MapIndustryTooltip : MonoBehaviour
	{
		private static GameObject? _tooltipObject;
		private static RectTransform? _tooltipRect;
		private static TextMeshProUGUI? _tooltipTitle;
		private static TextMeshProUGUI? _tooltipText;
		private static Image? _tooltipBackground;
		private static CanvasGroup? _canvasGroup;

		private static Industry? _currentHoveredIndustry;
		private static TrackSegment? _currentHoveredSegment;
		private static float _tooltipShowDelay = 0.3f;
		private static float _hoverTimer = 0f;
		private static bool _isShowing = false;
		private static string? _cachedTooltipText;
		private static float _cachedTooltipTextTime;
		
		// Track last mouse position to avoid redundant updates
		private static Vector2 _lastMousePosition;
		private static bool _needsPositionUpdate = false;

		// Dictionary mapping track segment IDs to industries
		private static Dictionary<string, Industry> _segmentToIndustry = new Dictionary<string, Industry>();
		
		private static float _lastUpdateLogTime = 0f;
		private static float _lastSearchLogTime = 0f;

		/// <summary>
		/// Initialize the tooltip UI
		/// </summary>
		public static void Initialize()
		{
			Loader.Log("[MapIndustryTooltip] Initialize() called");

			if (_tooltipObject != null)
			{
				Loader.Log("[MapIndustryTooltip] Tooltip already initialized, skipping");
				return;
			}

			try
			{
				// Create tooltip GameObject
				_tooltipObject = new GameObject("MapIndustryTooltip");
				_tooltipObject.layer = LayerMask.NameToLayer("UI");
				_tooltipRect = _tooltipObject.AddComponent<RectTransform>();

				Loader.Log("[MapIndustryTooltip] Created tooltip GameObject");

				// Parent to map window
				_tooltipRect.SetParent(MapWindow.instance._window.transform, false);

				// Make sure tooltip renders on top
				var canvas = _tooltipObject.AddComponent<Canvas>();
				canvas.overrideSorting = true;
				canvas.sortingOrder = 1001; // Higher than car tooltip (1000)

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
				_tooltipBackground.color = new Color(0.15f, 0.1f, 0.05f, 0.95f); // Slightly brown tint for industry

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
				_tooltipTitle.color = new Color(1f, 0.8f, 0.4f); // Orange/gold for industry
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

				// Build segment to industry mapping
				BuildSegmentMapping();

				Loader.Log("[MapIndustryTooltip] Successfully initialized tooltip UI");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapIndustryTooltip] ERROR during initialization: {ex.Message}");
				Loader.Log($"[MapIndustryTooltip] Stack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Build mapping of track segment IDs to industries
		/// </summary>
		private static void BuildSegmentMapping()
		{
			_segmentToIndustry.Clear();

			var allIndustries = Object.FindObjectsOfType<Industry>();
			Loader.Log($"[MapIndustryTooltip] Found {allIndustries.Length} industries");

			int totalSegmentsMapped = 0;

			foreach (var industry in allIndustries)
			{
				if (industry == null) continue;

				// Get all track spans for this industry
				var trackSpans = industry.GetComponentsInChildren<TrackSpan>();
				Loader.Log($"[MapIndustryTooltip] Industry '{industry.gameObject.name}' has {trackSpans.Length} track spans");
				
				foreach (var span in trackSpans)
				{
					if (span == null) continue;

					// Get all segments in this span
					var segments = span.GetSegments();
					Loader.Log($"[MapIndustryTooltip]   TrackSpan has {segments.Count} segments");
					
					foreach (var segment in segments)
					{
						if (segment != null && !string.IsNullOrEmpty(segment.id))
						{
							// Map this segment to the industry
							if (!_segmentToIndustry.ContainsKey(segment.id))
							{
								_segmentToIndustry[segment.id] = industry;
								totalSegmentsMapped++;
								Loader.Log($"[MapIndustryTooltip]     Mapped segment {segment.id} to {industry.gameObject.name}");
							}
							else
							{
								Loader.Log($"[MapIndustryTooltip]     Segment {segment.id} already mapped (shared between industries?)");
							}
						}
					}
				}
			}

			Loader.Log($"[MapIndustryTooltip] Built mapping with {_segmentToIndustry.Count} track segments (total mapped: {totalSegmentsMapped})");
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
			// Don't show industry tooltip if car tooltip is already showing
			if (MapCarTooltip.IsShowing())
			{
				if (_isShowing)
				{
					Loader.Log("[MapIndustryTooltip] Hiding tooltip - car tooltip is active");
					HideTooltip();
				}
				_currentHoveredIndustry = null;
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

			// Only show tooltip when ALT is held down
			bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

			if (!altHeld)
			{
				HideTooltip();
				_currentHoveredIndustry = null;
				_hoverTimer = 0f;
				return;
			}

			// Check if mouse is over map window
			if (!MapWindow.instance.mapDrag._pointerOver)
			{
				HideTooltip();
				_currentHoveredIndustry = null;
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

			// Find industry under mouse
			Industry? hoveredIndustry = GetIndustryUnderMouse();

			if (hoveredIndustry != _currentHoveredIndustry)
			{
				if (hoveredIndustry != null)
				{
					Loader.Log($"[MapIndustryTooltip] Now hovering over industry: {hoveredIndustry.gameObject.name}");
				}
				else if (_currentHoveredIndustry != null)
				{
					Loader.Log("[MapIndustryTooltip] No longer hovering over industry");
				}

				_currentHoveredIndustry = hoveredIndustry;
				_hoverTimer = 0f;
				_needsPositionUpdate = false;

				if (hoveredIndustry == null)
				{
					HideTooltip();
				}
			}

			if (_currentHoveredIndustry != null)
			{
				_hoverTimer += Time.unscaledDeltaTime;

				if (_hoverTimer >= _tooltipShowDelay)
				{
					if (!_isShowing)
					{
						Loader.Log($"[MapIndustryTooltip] Showing tooltip for {_currentHoveredIndustry.gameObject.name}");
						ShowTooltip(_currentHoveredIndustry);
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
		/// Find industry under mouse by raycasting to track segments
		/// </summary>
		private static Industry? GetIndustryUnderMouse()
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
				float searchRadius = 50 + (MapBuilder.Shared.mapCamera.orthographicSize / 13); // Larger radius when zoomed out
				TrackSegment? closestSegment = null;
				float closestDistance = float.MaxValue;

				foreach (var segmentKvp in Graph.Shared.segments)
				{
					var segment = segmentKvp.Value;
					if (segment == null) continue;

					// Check if this segment is mapped to an industry
					if (!_segmentToIndustry.ContainsKey(segment.id))
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

				if (closestSegment != null && _segmentToIndustry.TryGetValue(closestSegment.id, out Industry industry))
				{
					// Store the closest segment for use in tooltip generation
					_currentHoveredSegment = closestSegment;
					return industry;
				}
				
				_currentHoveredSegment = null;
				return null;
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapIndustryTooltip] ERROR in GetIndustryUnderMouse: {ex.Message}");
				Loader.Log($"[MapIndustryTooltip] Stack trace: {ex.StackTrace}");
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
		/// Show tooltip for industry
		/// </summary>
		private static void ShowTooltip(Industry industry)
		{
			if (_tooltipObject == null || industry == null || _tooltipTitle == null || _tooltipText == null)
			{
				Loader.Log($"[MapIndustryTooltip] Cannot show tooltip - missing components");
				return;
			}

			try
			{
				string title = GenerateTooltipTitle(industry);
				string text = GenerateTooltipText(industry);

				Loader.Log($"[MapIndustryTooltip] Generated tooltip - Title: '{title}', Text length: {text.Length} chars");

				if (text.Length < 25) return;

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

				Loader.Log($"[MapIndustryTooltip] Tooltip displayed at position {_tooltipRect?.anchoredPosition}");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapIndustryTooltip] ERROR in ShowTooltip: {ex.Message}");
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
		/// Update tooltip position (same logic as MapCarTooltip)
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
					Loader.Log("[MapIndustryTooltip] Parent rect is null!");
					return;
				}

				// Convert screen position to local position within the parent canvas
				Camera? camera = null; // UI camera (null for screen space overlay)
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
					parentRect, mousePos, camera, out Vector2 localMousePos))
				{
					Loader.Log($"[MapIndustryTooltip] Failed to convert screen point {mousePos} to local point");
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
					Loader.Log("[MapIndustryTooltip] Map camera has no render texture");
					return;
				}
				Vector2 textureMousePos = new Vector2(normalizedMousePos.x * renderTexture.width, normalizedMousePos.y * renderTexture.height);

				Loader.Log($"[MapIndustryTooltip] UpdatePosition - mouseScreen:{mousePos}, localMouse:{localMousePos}, textureMousePos:{textureMousePos}, tooltipSize:{tooltipSize}, parentBounds:{parentBounds}");

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
						Loader.Log($"[MapIndustryTooltip] Flipped horizontally, new x: {targetPos.x}");
					}
					//targetPos.y = localMousePos.y;
					if (targetPos.y - tooltipSize.y < 0)
					{
						targetPos.y = tooltipSize.y + 5;
						Loader.Log($"[MapIndustryTooltip] Flipped vertically, new y: {targetPos.y}");
					}
				}
				else  //single screen, right multiscreen
				{
					targetPos += localMousePos;
					// Flip horizontally if tooltip would go off right edge
					if (targetPos.x + tooltipSize.x > parentBounds.xMax - 10)
					{
						targetPos.x = localMousePos.x - tooltipSize.x;
						Loader.Log($"[MapIndustryTooltip] Flipped horizontally, new x: {targetPos.x}");
					}

					// Flip vertically if tooltip would go off bottom edge
					if (targetPos.y - tooltipSize.y < parentBounds.yMin + 10)
					{
						targetPos.y = localMousePos.y + tooltipSize.y;
						Loader.Log($"[MapIndustryTooltip] Flipped vertically, new y: {targetPos.y}");
					}

					// Clamp to stay within bounds
					float clampedX = targetPos.x; //Mathf.Clamp(targetPos.x, parentBounds.xMin + 10, parentBounds.xMax - tooltipSize.x - 10);
					float clampedY = targetPos.y; //Mathf.Clamp(targetPos.y, parentBounds.yMin + tooltipSize.y + 10, parentBounds.yMax - 10);

					if (clampedX != targetPos.x || clampedY != targetPos.y)
					{
						Loader.Log($"[MapIndustryTooltip] Clamped position from ({targetPos.x}, {targetPos.y}) to ({clampedX}, {clampedY})");
					}

					targetPos.x = clampedX;
					targetPos.y = parentBounds.height + clampedY; // Singlescreen y negative (reverted)
				}

				_tooltipRect.anchoredPosition = targetPos;
				Loader.Log($"[MapIndustryTooltip] Final Position ({targetPos.x}, {targetPos.y})");
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapIndustryTooltip] ERROR in UpdateTooltipPosition: {ex.Message}");
				Loader.Log($"[MapIndustryTooltip] Stack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Generate tooltip title
		/// </summary>
		private static string GenerateTooltipTitle(Industry industry)
		{
			return industry.gameObject.name; // Use GameObject name
		}

		/// <summary>
		/// Generate tooltip text showing storage levels
		/// </summary>
		private static string GenerateTooltipText(Industry industry)
		{
			float unscaledTime = Time.unscaledTime;

			// Cache for 1 second
			if (_cachedTooltipText != null && _cachedTooltipTextTime + 1f > unscaledTime && _currentHoveredIndustry == industry)
			{
				return _cachedTooltipText;
			}

			List<string> lines = new List<string>();

			try
			{
				var storage = industry.Storage;

				// Storage levels
				var loads = storage.Loads().ToList();
				if (loads.Count > 0)
				{
					lines.Add("<b>Storage:</b>");
					foreach (var load in loads)
					{
						float quantity = storage.QuantityInStorage(load);
						if (industry.TryGetStorageCapacity(load, out float capacity))
						{
							// Show pie chart and quantity
							string pieChart = TextSprites.PiePercent(quantity, capacity);
							lines.Add($"  {pieChart} {load.QuantityString(quantity)}");
						}
						else
						{
							lines.Add($"  • {load.QuantityString(quantity)}");
						}
					}
				}
				else
				{
					lines.Add("<b>Storage:</b> Empty");
				}

				// Production information from IndustryLoader components
				// Get loader components from the hovered track segment
				var allComponents = new List<IndustryComponent>();
				
				if (_currentHoveredSegment != null)
				{
					// Find the TrackSpan that contains this segment
					var trackSpans = industry.GetComponentsInChildren<TrackSpan>();
					
					foreach (var span in trackSpans)
					{
						if (span == null) continue;
						
						var segments = span.GetSegments();
						if (segments.Contains(_currentHoveredSegment))
						{
							// This span contains our hovered segment, get its components
							var spanComponents = span.GetComponents<IndustryComponent>();
							foreach (var comp in spanComponents)
							{
								if (comp != null)
								{
									allComponents.Add(comp);
									Loader.Log($"[MapIndustryTooltip] Found component on hovered segment: {comp.GetType().Name}");
								}
							}
							break; // Found the right span
						}
					}
				}
				
				// Also check for FormulaicIndustryComponent on the industry itself
				var formulaicComponents = industry.GetComponents<IndustryComponent>()
					.Where(c => c != null && c.GetType().Name == "FormulaicIndustryComponent")
					.ToList();
				
				foreach (var comp in formulaicComponents)
				{
					allComponents.Add(comp);
				}
				
				Loader.Log($"[MapIndustryTooltip] Total components: {allComponents.Count}");
				
				// Debug: Log all components before filtering
				foreach (var comp in allComponents)
				{
					Loader.Log($"[MapIndustryTooltip] Component type: {comp.GetType().FullName}");
				}
				
				// Process FormulaicIndustryComponent first (if any)
				bool hasProductionInfo = false;
				foreach (var component in allComponents)
				{
					if (component.GetType().Name == "FormulaicIndustryComponent")
					{
						Loader.Log($"[MapIndustryTooltip] Processing FormulaicIndustryComponent");
						
						if (!hasProductionInfo)
						{
							lines.Add(""); // Blank line separator
							lines.Add("<b>Production:</b>");
							hasProductionInfo = true;
						}
						
						try
						{
							const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
							var componentType = component.GetType();
							
							// Process OUTPUT terms (what the industry produces)
							var outputTermsField = componentType.GetField("outputTerms", flags);
							Loader.Log($"[MapIndustryTooltip] outputTermsField: {outputTermsField != null}");
							
							if (outputTermsField != null)
							{
								var outputTerms = outputTermsField.GetValue(component) as System.Collections.IEnumerable;
								Loader.Log($"[MapIndustryTooltip] outputTerms: {outputTerms != null}");
								
								if (outputTerms != null)
								{
									int termCount = 0;
									foreach (var term in outputTerms)
									{
										termCount++;
										var termType = term.GetType();
										var loadField = termType.GetField("load", flags);
										var unitsPerDayField = termType.GetField("unitsPerDay", flags);
					
										if (loadField != null && unitsPerDayField != null)
										{
											var load = loadField.GetValue(term);
											float unitsPerDay = (float)unitsPerDayField.GetValue(term);
											
											if (load != null && unitsPerDay > 0.001f)
											{
												var loadType = load.GetType();
												var descField = loadType.GetField("description", flags);
												var nominalQtyField = loadType.GetProperty("NominalQuantityPerCarLoad");
												
												if (descField != null && nominalQtyField != null)
												{
													string desc = (string)descField.GetValue(load);
													float nominalQty = (float)nominalQtyField.GetValue(load);
													float carsPerDay = unitsPerDay / nominalQty;
													
													// Format with 2 decimal places for better precision
													lines.Add($"  Produces: {desc} @ {carsPerDay:F1} cars/day");
												}
											}
										}
									}
									Loader.Log($"[MapIndustryTooltip] Processed {termCount} output terms");
								}
							}
							
							// Process INPUT terms (what the industry consumes)
							var inputTermsField = componentType.GetField("inputTerms", flags);
							Loader.Log($"[MapIndustryTooltip] inputTermsField: {inputTermsField != null}");
							
							if (inputTermsField != null)
							{
								var inputTerms = inputTermsField.GetValue(component) as System.Collections.IEnumerable;
								Loader.Log($"[MapIndustryTooltip] inputTerms: {inputTerms != null}");
								
								if (inputTerms != null)
								{
									int termCount = 0;
									foreach (var term in inputTerms)
									{
										termCount++;
										var termType = term.GetType();
										var loadField = termType.GetField("load", flags);
										var unitsPerDayField = termType.GetField("unitsPerDay", flags);
					
										if (loadField != null && unitsPerDayField != null)
										{
											var load = loadField.GetValue(term);
											float unitsPerDay = (float)unitsPerDayField.GetValue(term);
											
											if (load != null && unitsPerDay > 0.001f)
											{
												var loadType = load.GetType();
												var descField = loadType.GetField("description", flags);
												var nominalQtyField = loadType.GetProperty("NominalQuantityPerCarLoad");
												
												if (descField != null && nominalQtyField != null)
												{
													string desc = (string)descField.GetValue(load);
													float nominalQty = (float)nominalQtyField.GetValue(load);
													float carsPerDay = unitsPerDay / nominalQty;
												
													// Format with 2 decimal places for better precision
													lines.Add($"  Consumes: {desc} @ {carsPerDay:F1} cars/day");
												}
											}
										}
									}
									Loader.Log($"[MapIndustryTooltip] Processed {termCount} input terms");
								}
							}
						}
						catch (System.Exception ex)
						{
							Loader.Log($"[MapIndustryTooltip] ERROR reading FormulaicIndustryComponent: {ex.Message}");
							Loader.Log($"[MapIndustryTooltip] Stack trace: {ex.StackTrace}");
						}
					}
				}
				
				// Now process regular loaders/unloaders
				var loaders = allComponents
					.Where(c => c != null && (c.GetType().Name == "IndustryLoader" || c.GetType().Name == "IndustryUnloader"))
					.ToList();
				
				Loader.Log($"[MapIndustryTooltip] Found {loaders.Count} loader/unloader components after filtering");
				
				if (loaders.Count > 0)
				{
					if (!hasProductionInfo)
					{
						lines.Add(""); // Blank line separator
						lines.Add("<b>Production:</b>");
						hasProductionInfo = true;
					}
					
					foreach (var loader in loaders)
					{
						try
						{
							Loader.Log($"[MapIndustryTooltip] Processing loader: {loader.GetType().Name}");
							
							var loaderType = loader.GetType();
							const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
							
							var loadField = loaderType.GetField("load", flags);
							var carLoadRateField = loaderType.GetField("carLoadRate", flags);
							var carUnloadRateField = loaderType.GetField("carUnloadRate", flags);
							
							if (loadField != null)
							{
								var load = loadField.GetValue(loader);
								
								Loader.Log($"[MapIndustryTooltip] Load: {load?.GetType().Name ?? "null"}");
								
								if (load != null)
								{
									var loadType = load.GetType();
									var descField = loadType.GetField("description", flags);
									var nominalQtyField = loadType.GetProperty("NominalQuantityPerCarLoad");
				
									if (descField != null && nominalQtyField != null)
									{
										string desc = (string)descField.GetValue(load);
										float nominalQty = (float)nominalQtyField.GetValue(load);
										
										string loaderTypeName = loaderType.Name;
										if (loaderTypeName == "IndustryUnloader")
										{
											// For unloaders, show unloading rate
											if (carUnloadRateField != null)
											{
												float carUnloadRate = (float)carUnloadRateField.GetValue(loader);
												if (carUnloadRate > 0.001f)
												{
													float unloadCarsPerDay = carUnloadRate / nominalQty;
													lines.Add($"  Unloads: {desc} @ {unloadCarsPerDay:F1} cars/day");
												}
											}
										}
										else if (loaderTypeName == "IndustryLoader")
										{
											// For loaders, show loading rate
											if (carLoadRateField != null)
											{
												float carLoadRate = (float)carLoadRateField.GetValue(loader);
												if (carLoadRate > 0.001f)
												{
													float loadCarsPerDay = carLoadRate / nominalQty;
													lines.Add($"  Loads: {desc} @ {loadCarsPerDay:F1} cars/day");
												}
											}
										}
									}
								}
							}
						}
						catch (System.Exception ex)
						{
							Loader.Log($"[MapIndustryTooltip] ERROR reading loader component: {ex.Message}");
							Loader.Log($"[MapIndustryTooltip] Stack trace: {ex.StackTrace}");
						}
					}
				}
				
				if (!hasProductionInfo)
				{
					Loader.Log($"[MapIndustryTooltip] No production components found for {industry.gameObject.name}");
				}
				
				// Process TeamTrack components
				var teamTracks = allComponents
					.Where(c => c != null && c.GetType().Name == "TeamTrack")
					.ToList();
				
				if (teamTracks.Count > 0)
				{
					if (!hasProductionInfo)
					{
						lines.Add(""); // Blank line separator
						lines.Add("<b>Production:</b>");
						hasProductionInfo = true;
					}
					
					foreach (var teamTrack in teamTracks)
					{
						try
						{
							var teamTrackType = teamTrack.GetType();
							const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
							
							var profileField = teamTrackType.GetField("profile", flags);
							var idealCarsField = teamTrackType.GetField("idealCars", flags);
							
							if (profileField != null && idealCarsField != null)
							{
								var profile = profileField.GetValue(teamTrack);
								float idealCars = (float)idealCarsField.GetValue(teamTrack);
								
								if (profile != null)
								{
									var profileType = profile.GetType();
									var entriesField = profileType.GetField("entries", flags);
									
									if (entriesField != null)
									{
										var entries = entriesField.GetValue(profile) as System.Collections.IEnumerable;
										
										if (entries != null)
										{
											lines.Add($"  Team Track (wants {idealCars:F0} cars):");
						
											foreach (var entry in entries)
											{
												var entryType = entry.GetType();
												var loadField = entryType.GetField("load", flags);
												var exportField = entryType.GetField("export", flags);
												var loadingTimeField = entryType.GetField("loadingTime", flags);
									
												if (loadField != null && exportField != null && loadingTimeField != null)
												{
													var load = loadField.GetValue(entry);
													bool export = (bool)exportField.GetValue(entry);
													float loadingTime = (float)loadingTimeField.GetValue(entry);
									
													if (load != null)
													{
														var loadType = load.GetType();
														var descField = loadType.GetField("description", flags);
														var nominalQtyField = loadType.GetProperty("NominalQuantityPerCarLoad");
									
														if (descField != null && nominalQtyField != null)
														{
															string desc = (string)descField.GetValue(load);
															float nominalQty = (float)nominalQtyField.GetValue(load);
															
															// Calculate cars per day based on loading time
															float carsPerDay = 1.0f / loadingTime;
															
															if (export)
															{
																lines.Add($"    Exports: {desc} @ {carsPerDay:F1} cars/day");
															}
															else
															{
																lines.Add($"    Imports: {desc} @ {carsPerDay:F1} cars/day");
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
						catch (System.Exception ex)
						{
							Loader.Log($"[MapIndustryTooltip] ERROR reading TeamTrack component: {ex.Message}");
							Loader.Log($"[MapIndustryTooltip] Stack trace: {ex.StackTrace}");
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				Loader.Log($"[MapIndustryTooltip] ERROR generating tooltip: {ex.Message}");
				lines.Add($"Error: {ex.Message}");
			}

			string result = _cachedTooltipText = string.Join("\n", lines);
			_cachedTooltipTextTime = unscaledTime;
			return result;
		}

		/// <summary>
		/// Rebuild segment mapping (call when industries change)
		/// </summary>
		public static void RebuildMapping()
		{
			Loader.Log("[MapIndustryTooltip] Rebuilding segment mapping");
			BuildSegmentMapping();
		}

		/// <summary>
		/// Cleanup
		/// </summary>
		public static void Cleanup()
		{
			Loader.Log("[MapIndustryTooltip] Cleanup() called");

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

			_currentHoveredIndustry = null;
			_isShowing = false;
			_needsPositionUpdate = false;
			_lastMousePosition = Vector2.zero;
			_segmentToIndustry.Clear();

			Loader.Log("[MapIndustryTooltip] Tooltip destroyed");
		}
	}
}
