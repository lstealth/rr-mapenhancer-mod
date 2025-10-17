using TMPro;
using Track;
using UnityEngine;
using UnityEngine.UI;

namespace MapEnhancer;

public class TrackGradeMarker : MonoBehaviour
{
	public static TrackGradeMarker? gradePrefabYellow;
	public static TrackGradeMarker? gradePrefabOrange;
	public static TrackGradeMarker? gradePrefabRed;
	
	public TMP_Text? text;

	public static void CreatePrefabs()
	{
		gradePrefabYellow = CreatePrefab("Grade Marker Yellow", Color.yellow, "^");
		gradePrefabOrange = CreatePrefab("Grade Marker Orange", new Color(1f, 0.647f, 0f), "^^"); // Orange color
		gradePrefabRed = CreatePrefab("Grade Marker Red", Color.red, "^^^");
	}

	private static TrackGradeMarker CreatePrefab(string name, Color color, string symbol)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(MapEnhancer.prefabHolder.transform, false);
		go.hideFlags = HideFlags.HideAndDontSave;
		go.layer = LayerMask.NameToLayer("Map");

		TrackGradeMarker marker = go.AddComponent<TrackGradeMarker>();
		
		// Create canvas
		Canvas canvas = go.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		
		// Create text object
		GameObject textGo = new GameObject("Text");
		textGo.transform.SetParent(go.transform, false);
		textGo.layer = LayerMask.NameToLayer("Map");

		marker.text = textGo.AddComponent<TextMeshProUGUI>();
		marker.text.text = symbol;
		marker.text.fontSize = 32;
		marker.text.alignment = TextAlignmentOptions.Center;
		// Make the color semi-transparent (50% opacity)
		color.a = 0.3f;
		marker.text.color = color;
		marker.text.fontStyle = FontStyles.Bold;
		marker.text.enableAutoSizing = false;
		marker.text.raycastTarget = false;

		RectTransform rectTransform = textGo.GetComponent<RectTransform>();
		rectTransform.sizeDelta = new Vector2(200, 200);
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		
		// Set canvas rect transform
		RectTransform canvasRect = go.GetComponent<RectTransform>();
		canvasRect.sizeDelta = new Vector2(200, 200);

		return marker;
	}

	public static TrackGradeMarker? GetPrefabForGrade(float gradePercent)
	{
		float absGrade = Mathf.Abs(gradePercent);
		
		if (absGrade >= 1.5f)
			return gradePrefabRed;
		else if (absGrade >= 1.0f)
			return gradePrefabOrange;
		else if (absGrade >= 0.5f)
			return gradePrefabYellow;
		
		return null;
	}

	public static float CalculateGrade(TrackSegment segment, float distance)
	{
		// Sample positions to calculate grade
		float sampleDistance = 10f; // Sample 10 meters apart
		float halfSample = sampleDistance / 2f;
		
		// Make sure we don't go out of bounds
		float startDist = Mathf.Max(0, distance - halfSample);
		float endDist = Mathf.Min(segment.GetLength(), distance + halfSample);
		
		if (endDist - startDist < 1f) return 0f; // Too short to calculate
		
		Vector3 startPos = new Location(segment, startDist, TrackSegment.End.A).GetPosition();
		Vector3 endPos = new Location(segment, endDist, TrackSegment.End.A).GetPosition();
		
		float horizontalDistance = new Vector2(endPos.x - startPos.x, endPos.z - startPos.z).magnitude;
		float verticalChange = endPos.y - startPos.y;
		
		if (horizontalDistance < 0.1f) return 0f; // Avoid division by zero
		
		// Calculate grade as percentage (rise/run * 100)
		float gradePercent = (verticalChange / horizontalDistance) * 100f;
		
		return gradePercent;
	}
}
