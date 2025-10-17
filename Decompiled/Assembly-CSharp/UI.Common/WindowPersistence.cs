using Game;
using Helpers;
using Newtonsoft.Json;
using Serilog;
using UnityEngine;

namespace UI.Common;

public static class WindowPersistence
{
	private struct WindowRecord
	{
		public bool Shown;

		[JsonConverter(typeof(Vector2Converter))]
		public Vector2 Position;

		[JsonConverter(typeof(Vector2Converter))]
		public Vector2 Size;
	}

	private static string KeyForWindow(string identifier)
	{
		return "window." + identifier;
	}

	private static bool TryGetWindowPositionSize(string identifier, out bool shown, out Vector2 position, out Vector2Int size)
	{
		shown = false;
		position = Vector2.zero;
		size = Vector2Int.zero;
		try
		{
			string text = KeyForWindow(identifier);
			string value = PlayerPrefs.GetString(text);
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			WindowRecord windowRecord = JsonConvert.DeserializeObject<WindowRecord>(value);
			shown = windowRecord.Shown;
			position = windowRecord.Position;
			size = new Vector2Int(Mathf.RoundToInt(windowRecord.Size.x), Mathf.RoundToInt(windowRecord.Size.y));
			Log.Debug("Window {identifier}: Loaded {shown}, {position}, {size} from {key}", identifier, shown, position, size, text);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void SaveWindow(string identifier, bool shown, Vector2 position, Vector2Int contentSize)
	{
		string key = KeyForWindow(identifier);
		WindowRecord windowRecord = new WindowRecord
		{
			Shown = shown,
			Position = position,
			Size = contentSize
		};
		PlayerPrefs.SetString(key, JsonConvert.SerializeObject(windowRecord));
	}

	public static void SetInitialPositionSize(this Window window, string identifier, Vector2 defaultSize, Window.Position defaultPosition, Window.Sizing sizing)
	{
		if (sizing.IsResizable)
		{
			window.SetResizable(sizing.MinSize, sizing.MaxSize);
		}
		if (TryGetWindowPositionSize(identifier, out var _, out var position, out var size))
		{
			float graphicsCanvasScale = Preferences.GraphicsCanvasScale;
			window.SetContentSize(sizing.Clamp(size));
			window.SetPositionRestoring(new Vector2(position.x * (float)Screen.width * graphicsCanvasScale, position.y * (float)Screen.height * graphicsCanvasScale));
		}
		else
		{
			window.SetContentSize(defaultSize);
			window.SetPosition(defaultPosition);
		}
		window.OnShownDidChange += delegate
		{
			DoSaveWindow();
		};
		window.OnDidPosition += delegate
		{
			DoSaveWindow();
		};
		window.OnDidResize += delegate
		{
			DoSaveWindow();
		};
		void DoSaveWindow()
		{
			float graphicsCanvasScale2 = Preferences.GraphicsCanvasScale;
			Vector2 position2 = window.GetPosition();
			Vector2 contentSize = window.GetContentSize();
			SaveWindow(identifier, window.IsShown, new Vector2(position2.x / (graphicsCanvasScale2 * (float)Screen.width), position2.y / (graphicsCanvasScale2 * (float)Screen.height)), new Vector2Int(Mathf.RoundToInt(contentSize.x), Mathf.RoundToInt(contentSize.y)));
		}
	}
}
