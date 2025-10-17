using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

public class DebugGUI : MonoBehaviour
{
	private class AttributeKey
	{
		public MemberInfo memberInfo;

		public AttributeKey(MemberInfo memberInfo)
		{
			this.memberInfo = memberInfo;
		}
	}

	private struct TransientLog
	{
		public string text;

		public float expiryTime;

		public TransientLog(string text, float duration)
		{
			this.text = text;
			expiryTime = Time.realtimeSinceStartup + duration;
		}
	}

	[Serializable]
	private class GraphContainer
	{
		public string name;

		public float max = 1f;

		public float min;

		public bool autoScale;

		public Color color;

		public int group;

		private Texture2D tex0;

		private Texture2D tex1;

		private bool texFlipFlop;

		private int currentIndex;

		private float[] values;

		private static Color32[] clearColorArray = new Color32[30000];

		public void SetMinMax(float min, float max)
		{
			if (this.min != min || this.max != max)
			{
				RegenerateGraph();
				this.min = min;
				this.max = max;
			}
		}

		public GraphContainer(int width, int height)
		{
			values = new float[width];
			tex0 = new Texture2D(width, height);
			tex0.SetPixels32(clearColorArray);
			tex1 = new Texture2D(width, height);
			tex1.SetPixels32(clearColorArray);
		}

		public void Push(float val)
		{
			if (autoScale && (val > max || val < min))
			{
				SetMinMax(Mathf.Min(val, min), Mathf.Max(val, max));
			}
			currentIndex = (currentIndex + 1) % values.Length;
			values[currentIndex] = val;
			Texture2D texture2D = (texFlipFlop ? tex0 : tex1);
			Texture2D texture2D2 = (texFlipFlop ? tex1 : tex0);
			texFlipFlop = !texFlipFlop;
			Graphics.CopyTexture(texture2D, 0, 0, 0, 0, texture2D.width - 1, texture2D.height, texture2D2, 0, 0, 1, 0);
			for (int i = 0; i < texture2D2.height; i++)
			{
				texture2D2.SetPixel(0, i, Color.clear);
			}
			float value = values[Mod(currentIndex, values.Length)];
			float value2 = values[Mod(currentIndex - 1, values.Length)];
			int num = (int)(Mathf.InverseLerp(min, max, value) * 100f);
			int num2 = (int)(Mathf.InverseLerp(min, max, value2) * 100f);
			num = ((num >= 100) ? 99 : num);
			num2 = ((num2 >= 100) ? 99 : num2);
			DrawLine(texture2D2, 0, num, 1, num2, color);
		}

		public void Clear()
		{
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = 0f;
			}
		}

		public void Draw(Rect rect)
		{
			Texture2D texture2D = (texFlipFlop ? tex1 : tex0);
			texture2D.Apply();
			GUI.DrawTexture(rect, texture2D);
		}

		public float GetValue(int index)
		{
			return values[Mod(currentIndex + index, values.Length)];
		}

		private void RegenerateGraph()
		{
			Texture2D tex = (texFlipFlop ? tex0 : tex1);
			tex0.SetPixels32(clearColorArray);
			tex1.SetPixels32(clearColorArray);
			for (int i = 0; i < values.Length - 1; i++)
			{
				DrawLine(tex, i, (int)(Mathf.InverseLerp(min, max, values[Mod(currentIndex - i, values.Length)]) * 100f), i + 1, (int)(Mathf.InverseLerp(min, max, values[Mod(currentIndex - i - 1, values.Length)]) * 100f), color);
			}
		}

		private static int Mod(int n, int m)
		{
			return (n % m + m) % m;
		}

		private void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
		{
			int num = y1 - y0;
			int num2 = x1 - x0;
			int num3;
			if (num < 0)
			{
				num = -num;
				num3 = -1;
			}
			else
			{
				num3 = 1;
			}
			int num4;
			if (num2 < 0)
			{
				num2 = -num2;
				num4 = -1;
			}
			else
			{
				num4 = 1;
			}
			num <<= 1;
			num2 <<= 1;
			float num5 = 0f;
			tex.SetPixel(x0, y0, col);
			if (num2 > num)
			{
				num5 = num - (num2 >> 1);
				while (((x0 > x1) ? (x0 - x1) : (x1 - x0)) > 1)
				{
					if (num5 >= 0f)
					{
						y0 += num3;
						num5 -= (float)num2;
					}
					x0 += num4;
					num5 += (float)num;
					tex.SetPixel(x0, y0, col);
				}
				return;
			}
			num5 = num2 - (num >> 1);
			while (((y0 > y1) ? (y0 - y1) : (y1 - y0)) > 1)
			{
				if (num5 >= 0f)
				{
					x0 += num4;
					num5 -= (float)num;
				}
				y0 += num3;
				num5 += (float)num2;
				tex.SetPixel(x0, y0, col);
			}
		}

		public void DestroyTextures()
		{
			UnityEngine.Object.Destroy(tex0);
			UnityEngine.Object.Destroy(tex1);
		}
	}

	private static DebugGUI _instance;

	private const int graphWidth = 300;

	private const int graphHeight = 100;

	private const float temporaryLogLifetime = 5f;

	[SerializeField]
	private bool drawInBuild;

	[SerializeField]
	private bool displayGraphs = true;

	[SerializeField]
	private bool displayLogs = true;

	[SerializeField]
	private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);

	[Header("Runtime Debugging Only")]
	[SerializeField]
	private List<GraphContainer> graphs = new List<GraphContainer>();

	private Dictionary<object, string> persistentLogs = new Dictionary<object, string>();

	private Queue<TransientLog> transientLogs = new Queue<TransientLog>();

	private Dictionary<object, GraphContainer> graphDictionary = new Dictionary<object, GraphContainer>();

	private GUIStyle minMaxTextStyle;

	private GUIStyle boxStyle;

	private bool freezeGraphs;

	private Texture2D boxTexture;

	private const float minMaxTextHeight = 8f;

	private const float nextLineHeight = 15f;

	private GUIContent labelGuiContent = new GUIContent();

	private float textWidth;

	private Rect textRect;

	private HashSet<int> graphGroupBoxesDrawn = new HashSet<int>();

	private float graphLabelWidth;

	private StringBuilder stringBuilder = new StringBuilder();

	private List<MonoBehaviour> attributeContainers = new List<MonoBehaviour>();

	private Dictionary<Type, HashSet<FieldInfo>> debugGUIPrintFields = new Dictionary<Type, HashSet<FieldInfo>>();

	private Dictionary<Type, HashSet<PropertyInfo>> debugGUIPrintProperties = new Dictionary<Type, HashSet<PropertyInfo>>();

	private Dictionary<Type, HashSet<FieldInfo>> debugGUIGraphFields = new Dictionary<Type, HashSet<FieldInfo>>();

	private Dictionary<Type, HashSet<PropertyInfo>> debugGUIGraphProperties = new Dictionary<Type, HashSet<PropertyInfo>>();

	private Dictionary<Type, int> typeInstanceCounts = new Dictionary<Type, int>();

	private Dictionary<MonoBehaviour, List<AttributeKey>> attributeKeys = new Dictionary<MonoBehaviour, List<AttributeKey>>();

	private static DebugGUI Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindObjectOfType<DebugGUI>();
				if (_instance == null && Application.isPlaying)
				{
					_instance = new GameObject("DebugGUI").AddComponent<DebugGUI>();
				}
			}
			return _instance;
		}
	}

	private static bool LogsEnabled
	{
		get
		{
			if (Instance.displayLogs)
			{
				if (!Instance.drawInBuild)
				{
					return Application.isEditor;
				}
				return true;
			}
			return false;
		}
	}

	private static bool GraphsEnabled
	{
		get
		{
			if (Instance.displayGraphs)
			{
				if (!Instance.drawInBuild)
				{
					return Application.isEditor;
				}
				return true;
			}
			return false;
		}
	}

	public static void LogPersistent(object key, object message)
	{
		if (LogsEnabled)
		{
			Instance.InstanceLogPersistent(key, message);
		}
	}

	public static void RemovePersistent(object key)
	{
		if (LogsEnabled)
		{
			Instance.InstanceRemovePersistent(key);
		}
	}

	public static void ClearPersistent()
	{
		if (LogsEnabled)
		{
			Instance.InstanceClearPersistent();
		}
	}

	public static void Log(object message)
	{
		if (LogsEnabled)
		{
			Instance.InstanceLog(message.ToString());
		}
	}

	public static void SetGraphProperties(object key, string label, float min, float max, int group, Color color, bool autoScale)
	{
		if (GraphsEnabled)
		{
			Instance.InstanceSetGraphProperties(key, label, min, max, group, color, autoScale);
		}
	}

	public static void Graph(object key, float val)
	{
		if (GraphsEnabled)
		{
			Instance.InstanceGraph(key, val);
		}
	}

	public static void RemoveGraph(object key)
	{
		if (GraphsEnabled)
		{
			Instance.InstanceRemoveGraph(key);
		}
	}

	public static void ClearGraph(object key)
	{
		if (GraphsEnabled)
		{
			Instance.InstanceClearGraph(key);
		}
	}

	private void InstanceLogPersistent(object key, object message)
	{
		if (persistentLogs.ContainsKey(key))
		{
			persistentLogs[key] = message.ToString();
		}
		else
		{
			persistentLogs.Add(key, message.ToString());
		}
	}

	private void InstanceRemovePersistent(object key)
	{
		if (persistentLogs.ContainsKey(key))
		{
			persistentLogs.Remove(key);
		}
	}

	private void InstanceClearPersistent()
	{
		persistentLogs.Clear();
	}

	private void InstanceRemoveGraph(object key)
	{
		if (graphDictionary.ContainsKey(key))
		{
			GraphContainer graphContainer = graphDictionary[key];
			graphContainer.DestroyTextures();
			graphs.Remove(graphContainer);
			graphDictionary.Remove(key);
		}
	}

	private void InstanceClearGraph(object key)
	{
		if (graphDictionary.ContainsKey(key))
		{
			graphDictionary[key].Clear();
		}
	}

	private void InstanceLog(string str)
	{
		transientLogs.Enqueue(new TransientLog(str, 5f));
	}

	private void InstanceGraph(object key, float val)
	{
		if (!graphDictionary.ContainsKey(key))
		{
			InstanceCreateGraph(key);
		}
		if (!freezeGraphs)
		{
			graphDictionary[key].Push(val);
		}
	}

	private void InstanceSetGraphProperties(object key, string label, float min, float max, int group, Color color, bool autoScale)
	{
		if (!graphDictionary.ContainsKey(key))
		{
			InstanceCreateGraph(key);
		}
		GraphContainer graphContainer = graphDictionary[key];
		graphContainer.name = label;
		graphContainer.SetMinMax(min, max);
		graphContainer.group = Mathf.Max(0, group);
		graphContainer.color = color;
		graphContainer.autoScale = autoScale;
	}

	private void InstanceCreateGraph(object key)
	{
		graphDictionary.Add(key, new GraphContainer(300, 100));
		graphs.Add(graphDictionary[key]);
	}

	private void Awake()
	{
		if (drawInBuild || Application.isEditor)
		{
			InitializeGUIStyles();
			RegisterAttributes();
		}
	}

	private void Update()
	{
		if (LogsEnabled || GraphsEnabled)
		{
			CleanUpDeletedAtributes();
		}
		if (LogsEnabled)
		{
			while (transientLogs.Count > 0 && transientLogs.Peek().expiryTime <= Time.realtimeSinceStartup)
			{
				transientLogs.Dequeue();
			}
		}
		if (!GraphsEnabled || freezeGraphs)
		{
			return;
		}
		for (int i = 0; i < attributeContainers.Count; i++)
		{
			MonoBehaviour monoBehaviour = attributeContainers[i];
			if (!(monoBehaviour != null) || !attributeKeys.ContainsKey(monoBehaviour) || !monoBehaviour.enabled)
			{
				continue;
			}
			foreach (AttributeKey item in attributeKeys[monoBehaviour])
			{
				if (item.memberInfo is FieldInfo)
				{
					float? num = (item.memberInfo as FieldInfo).GetValue(monoBehaviour) as float?;
					if (num.HasValue)
					{
						graphDictionary[item].Push(num.Value);
					}
				}
				else if (item.memberInfo is PropertyInfo)
				{
					float? num2 = (item.memberInfo as PropertyInfo).GetValue(monoBehaviour, null) as float?;
					if (num2.HasValue)
					{
						graphDictionary[item].Push(num2.Value);
					}
				}
			}
		}
	}

	private void OnGUI()
	{
		GUI.color = Color.white;
		if (LogsEnabled)
		{
			DrawLogs();
		}
		if (GraphsEnabled)
		{
			DrawGraphs();
		}
	}

	private void InitializeGUIStyles()
	{
		minMaxTextStyle = new GUIStyle();
		minMaxTextStyle.fontSize = 10;
		minMaxTextStyle.fontStyle = FontStyle.Bold;
		Color[] array = new Color[4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Color.white;
		}
		boxTexture = new Texture2D(2, 2);
		boxTexture.SetPixels(array);
		boxTexture.Apply();
		boxStyle = new GUIStyle();
		boxStyle.normal.background = boxTexture;
	}

	private void DrawLogs()
	{
		GUI.backgroundColor = backgroundColor;
		GUI.Box(new Rect(0f, 0f, textWidth + 10f, textRect.y + 5f), "", boxStyle);
		textRect = new Rect(5f, 0f, Screen.width, Screen.height);
		textWidth = 0f;
		for (int i = 0; i < attributeContainers.Count; i++)
		{
			MonoBehaviour monoBehaviour = attributeContainers[i];
			if (!(monoBehaviour != null) || !monoBehaviour.enabled)
			{
				continue;
			}
			Type type = monoBehaviour.GetType();
			if (debugGUIPrintFields.ContainsKey(type))
			{
				foreach (FieldInfo item in debugGUIPrintFields[type])
				{
					DrawLabel($"{monoBehaviour.name} {item.Name}: {item.GetValue(monoBehaviour)}");
				}
			}
			if (!debugGUIPrintProperties.ContainsKey(type))
			{
				continue;
			}
			foreach (PropertyInfo item2 in debugGUIPrintProperties[type])
			{
				DrawLabel($"{monoBehaviour.name} {item2.Name}: {item2.GetValue(monoBehaviour, null)}");
			}
		}
		foreach (string value in persistentLogs.Values)
		{
			DrawLabel(value);
		}
		if (textRect.y != 0f && transientLogs.Count != 0)
		{
			DrawLabel("-------------------");
		}
		foreach (TransientLog transientLog in transientLogs)
		{
			DrawLabel(transientLog.text);
		}
		while (textRect.y > (float)Screen.height && transientLogs.Count > 0)
		{
			transientLogs.Dequeue();
			textRect.y -= 15f;
		}
	}

	private void DrawLabel(string label)
	{
		labelGuiContent.text = label;
		GUI.Label(textRect, labelGuiContent);
		textRect.y += 15f;
		textWidth = Mathf.Max(textWidth, GUIStyle.none.CalcSize(labelGuiContent).x);
	}

	private void DrawGraphs()
	{
		float num = 103f;
		float num2 = 303f;
		GUI.backgroundColor = backgroundColor;
		foreach (int item in graphGroupBoxesDrawn)
		{
			GUI.Box(new Rect((float)Screen.width - num2 - graphLabelWidth - 5f, (float)item * num, graphLabelWidth + 5f, 100f), "", boxStyle);
		}
		graphLabelWidth = 0f;
		graphGroupBoxesDrawn.Clear();
		foreach (GraphContainer value in graphDictionary.Values)
		{
			if (graphGroupBoxesDrawn.Add(value.group))
			{
				GUI.Box(new Rect(Screen.width - 300, 0f + num * (float)value.group, 300f, 100f), "", boxStyle);
			}
			value.Draw(new Rect(Screen.width - 300, 0f + num * (float)value.group, 300f, 100f));
		}
		foreach (int item2 in graphGroupBoxesDrawn)
		{
			float num3 = (float)item2 * num;
			float num4 = num3 + 8f;
			float num5 = 0f;
			foreach (GraphContainer value2 in graphDictionary.Values)
			{
				labelGuiContent.text = "";
				if (value2.group == item2)
				{
					minMaxTextStyle.normal.textColor = value2.color;
					GUI.color = Color.white;
					string text = value2.min.ToString("F2");
					string text2 = value2.max.ToString("F2");
					Vector2 vector = minMaxTextStyle.CalcSize(labelGuiContent);
					labelGuiContent.text = text;
					float x = vector.x;
					labelGuiContent.text = text2;
					x = Mathf.Max(x, minMaxTextStyle.CalcSize(labelGuiContent).x);
					labelGuiContent.text = value2.max.ToString("F2");
					num5 += x + 5f;
					GUI.Label(new Rect((float)(Screen.width - 300) - num5, num3, num5, 100f), labelGuiContent, minMaxTextStyle);
					labelGuiContent.text = value2.min.ToString("F2");
					GUI.Label(new Rect((float)(Screen.width - 300) - num5, num3 + 100f - vector.y, num5, 100f), labelGuiContent, minMaxTextStyle);
					GUI.color = value2.color;
					labelGuiContent.text = value2.name;
					float num6 = GUIStyle.none.CalcSize(labelGuiContent).x + 5f;
					graphLabelWidth = Mathf.Max(num6, graphLabelWidth, num5);
					GUI.Label(new Rect((float)(Screen.width - 300) - num6, num4, num6, 100f), labelGuiContent);
					num4 += 15f;
				}
			}
		}
		Vector3 mousePosition = Input.mousePosition;
		mousePosition.y = (float)Screen.height - mousePosition.y;
		if (freezeGraphs && !Input.GetMouseButton(0))
		{
			freezeGraphs = false;
		}
		foreach (int item3 in graphGroupBoxesDrawn)
		{
			if (!(mousePosition.x < (float)Screen.width) || !(mousePosition.x > (float)(Screen.width - 300)) || !(mousePosition.y > (float)item3 * num) || !(mousePosition.y < (float)item3 * num + 100f))
			{
				continue;
			}
			if (Input.GetMouseButtonDown(0))
			{
				freezeGraphs = true;
			}
			GUI.backgroundColor = new Color(1f, 1f, 0f, 0.75f);
			GUI.color = new Color(1f, 1f, 0f, 0.75f);
			GUI.Box(new Rect(mousePosition.x, (float)item3 * num, 1f, 100f), "", boxStyle);
			GUI.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
			GUI.color = Color.white;
			GUI.Box(new Rect(mousePosition.x - 60f, (float)item3 * num, 55f, 55f), "", boxStyle);
			int index = (int)((float)Screen.width - mousePosition.x);
			float num7 = 0f;
			{
				foreach (GraphContainer value3 in graphDictionary.Values)
				{
					if (value3.group == item3)
					{
						minMaxTextStyle.normal.textColor = value3.color;
						GUI.color = Color.white;
						labelGuiContent.text = value3.GetValue(index).ToString("F3");
						GUI.Label(new Rect(mousePosition.x + -55f, (float)item3 * num + num7, 45f, 50f), labelGuiContent, minMaxTextStyle);
						num7 += 8f;
					}
				}
				break;
			}
		}
	}

	public static void ForceReinitializeAttributes()
	{
		List<object> list = new List<object>();
		foreach (object key in Instance.graphDictionary.Keys)
		{
			if (key is AttributeKey)
			{
				list.Add(key);
			}
		}
		foreach (object item in list)
		{
			Instance.InstanceRemoveGraph(item);
		}
		list.Clear();
		foreach (object key2 in Instance.persistentLogs.Keys)
		{
			if (key2 is AttributeKey)
			{
				list.Add(key2);
			}
		}
		foreach (object item2 in list)
		{
			Instance.persistentLogs.Remove(item2);
		}
		Instance.attributeContainers = new List<MonoBehaviour>();
		Instance.debugGUIPrintFields = new Dictionary<Type, HashSet<FieldInfo>>();
		Instance.debugGUIPrintProperties = new Dictionary<Type, HashSet<PropertyInfo>>();
		Instance.debugGUIGraphFields = new Dictionary<Type, HashSet<FieldInfo>>();
		Instance.debugGUIGraphProperties = new Dictionary<Type, HashSet<PropertyInfo>>();
		Instance.typeInstanceCounts = new Dictionary<Type, int>();
		Instance.attributeKeys = new Dictionary<MonoBehaviour, List<AttributeKey>>();
		Instance.RegisterAttributes();
	}

	private void RegisterAttributes()
	{
		MonoBehaviour[] array = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
		HashSet<MonoBehaviour> hashSet = new HashSet<MonoBehaviour>();
		MonoBehaviour[] array2 = array;
		foreach (MonoBehaviour monoBehaviour in array2)
		{
			Type type = monoBehaviour.GetType();
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int j = 0; j < fields.Length; j++)
			{
				if (Attribute.GetCustomAttribute(fields[j], typeof(DebugGUIPrintAttribute)) is DebugGUIPrintAttribute)
				{
					hashSet.Add(monoBehaviour);
					if (!debugGUIPrintFields.ContainsKey(type))
					{
						debugGUIPrintFields.Add(type, new HashSet<FieldInfo>());
					}
					if (!debugGUIPrintProperties.ContainsKey(type))
					{
						debugGUIPrintProperties.Add(type, new HashSet<PropertyInfo>());
					}
					debugGUIPrintFields[type].Add(fields[j]);
				}
				if (!(Attribute.GetCustomAttribute(fields[j], typeof(DebugGUIGraphAttribute)) is DebugGUIGraphAttribute debugGUIGraphAttribute))
				{
					continue;
				}
				if (!(fields[j].GetValue(monoBehaviour) as float?).HasValue)
				{
					Debug.LogError($"Cannot cast {type.Name}.{fields[j].Name} to float. This member will be ignored.");
					continue;
				}
				hashSet.Add(monoBehaviour);
				if (!debugGUIGraphFields.ContainsKey(type))
				{
					debugGUIGraphFields.Add(type, new HashSet<FieldInfo>());
				}
				if (!debugGUIGraphProperties.ContainsKey(type))
				{
					debugGUIGraphProperties.Add(type, new HashSet<PropertyInfo>());
				}
				debugGUIGraphFields[type].Add(fields[j]);
				GraphContainer graphContainer = new GraphContainer(300, 100)
				{
					name = fields[j].Name,
					max = debugGUIGraphAttribute.max,
					min = debugGUIGraphAttribute.min,
					group = debugGUIGraphAttribute.group,
					autoScale = debugGUIGraphAttribute.autoScale
				};
				if (!debugGUIGraphAttribute.color.Equals(default(Color)))
				{
					graphContainer.color = debugGUIGraphAttribute.color;
				}
				AttributeKey attributeKey = new AttributeKey(fields[j]);
				if (!attributeKeys.ContainsKey(monoBehaviour))
				{
					attributeKeys.Add(monoBehaviour, new List<AttributeKey>());
				}
				attributeKeys[monoBehaviour].Add(attributeKey);
				graphDictionary.Add(attributeKey, graphContainer);
				graphs.Add(graphContainer);
			}
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int k = 0; k < properties.Length; k++)
			{
				if (Attribute.GetCustomAttribute(properties[k], typeof(DebugGUIPrintAttribute)) is DebugGUIPrintAttribute)
				{
					hashSet.Add(monoBehaviour);
					if (!debugGUIPrintFields.ContainsKey(type))
					{
						debugGUIPrintFields.Add(type, new HashSet<FieldInfo>());
					}
					if (!debugGUIPrintProperties.ContainsKey(type))
					{
						debugGUIPrintProperties.Add(type, new HashSet<PropertyInfo>());
					}
					debugGUIPrintProperties[type].Add(properties[k]);
				}
				if (!(Attribute.GetCustomAttribute(properties[k], typeof(DebugGUIGraphAttribute)) is DebugGUIGraphAttribute debugGUIGraphAttribute2))
				{
					continue;
				}
				if (!(properties[k].GetValue(monoBehaviour, null) as float?).HasValue)
				{
					Debug.LogError("Cannot cast " + properties[k].Name + " to float. This member will be ignored.");
					continue;
				}
				hashSet.Add(monoBehaviour);
				if (!debugGUIGraphFields.ContainsKey(type))
				{
					debugGUIGraphFields.Add(type, new HashSet<FieldInfo>());
				}
				if (!debugGUIGraphProperties.ContainsKey(type))
				{
					debugGUIGraphProperties.Add(type, new HashSet<PropertyInfo>());
				}
				debugGUIGraphProperties[type].Add(properties[k]);
				GraphContainer graphContainer2 = new GraphContainer(300, 100)
				{
					name = properties[k].Name,
					max = debugGUIGraphAttribute2.max,
					min = debugGUIGraphAttribute2.min,
					group = debugGUIGraphAttribute2.group,
					autoScale = debugGUIGraphAttribute2.autoScale
				};
				if (!debugGUIGraphAttribute2.color.Equals(default(Color)))
				{
					graphContainer2.color = debugGUIGraphAttribute2.color;
				}
				AttributeKey attributeKey2 = new AttributeKey(properties[k]);
				if (!attributeKeys.ContainsKey(monoBehaviour))
				{
					attributeKeys.Add(monoBehaviour, new List<AttributeKey>());
				}
				attributeKeys[monoBehaviour].Add(attributeKey2);
				graphDictionary.Add(attributeKey2, graphContainer2);
				graphs.Add(graphContainer2);
			}
		}
		foreach (MonoBehaviour item in hashSet)
		{
			attributeContainers.Add(item);
			Type type2 = item.GetType();
			if (!typeInstanceCounts.ContainsKey(type2))
			{
				typeInstanceCounts.Add(type2, 0);
			}
			typeInstanceCounts[type2]++;
		}
	}

	private void CleanUpDeletedAtributes()
	{
		for (int i = 0; i < attributeContainers.Count; i++)
		{
			if (!(attributeContainers[i] == null))
			{
				continue;
			}
			MonoBehaviour monoBehaviour = attributeContainers[i];
			attributeContainers.RemoveAt(i);
			foreach (AttributeKey item in attributeKeys[monoBehaviour])
			{
				InstanceRemoveGraph(item);
			}
			attributeKeys.Remove(monoBehaviour);
			Type type = monoBehaviour.GetType();
			typeInstanceCounts[type]--;
			if (typeInstanceCounts[type] == 0)
			{
				if (debugGUIPrintFields.ContainsKey(type))
				{
					debugGUIPrintFields.Remove(type);
				}
				if (debugGUIPrintProperties.ContainsKey(type))
				{
					debugGUIPrintProperties.Remove(type);
				}
				if (debugGUIGraphFields.ContainsKey(type))
				{
					debugGUIGraphFields.Remove(type);
				}
				if (debugGUIGraphProperties.ContainsKey(type))
				{
					debugGUIGraphProperties.Remove(type);
				}
			}
			i--;
		}
	}

	private void OnDestroy()
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(boxTexture);
		}
	}
}
