using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Markroader;
using Newtonsoft.Json;
using Serilog;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Guide;

public class GuideWindow : MonoBehaviour, IBuilderWindow
{
	private class Document
	{
		public HashSet<string> LinkAnchors = new HashSet<string>();

		public string Identifier { get; }

		public string Path { get; }

		public string Section { get; }

		public string Title { get; private set; }

		public string TextMeshMarkup { get; private set; }

		public IReadOnlyDictionary<string, string> Headers { get; }

		public Document(string identifier, string path, string section, IReadOnlyDictionary<string, string> headers, string title, string textMeshMarkup)
		{
			Identifier = identifier;
			Path = path;
			Section = section;
			Title = title;
			TextMeshMarkup = textMeshMarkup;
			Headers = headers;
		}
	}

	[Serializable]
	public struct GuideIndex
	{
		public struct Section
		{
			public string Name;

			public string Path;
		}

		public List<Section> Sections;
	}

	private Window _window;

	private static GuideWindow _instance;

	private readonly UIState<string> _selectedItem = new UIState<string>(null);

	[CanBeNull]
	private Tuple<string, string> _linkedItem;

	private UIPanel _panel;

	private List<Document> _contents;

	public UIBuilderAssets BuilderAssets { get; set; }

	private static GuideWindow Instance => WindowManager.Shared.GetWindow<GuideWindow>();

	public static void Toggle()
	{
		if (Instance._window.IsShown)
		{
			Instance._window.CloseWindow();
		}
		else
		{
			Instance.Show();
		}
	}

	private void Show()
	{
		Populate();
		_window.ShowWindow();
	}

	public static void Show(string identifier)
	{
		Instance.Show();
		Instance.JumpToLinkId(identifier);
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate()
	{
		_window.Title = "Guide";
		if (_contents == null)
		{
			_contents = new List<Document>();
			string path = Path.Combine(Application.streamingAssetsPath, "Guide");
			foreach (GuideIndex.Section section in JsonConvert.DeserializeObject<GuideIndex>(File.ReadAllText(Path.Combine(path, "Index.json"))).Sections)
			{
				foreach (string item in Directory.GetFiles(Path.Combine(path, section.Path), "*.md").ToList())
				{
					try
					{
						Markroader.Document document = Parser.Parse(File.ReadAllText(item));
						string text = TMPMarkupRenderer.Render(document.Elements);
						string title = document.Elements.First(delegate(Element el)
						{
							ElementType type = el.Type;
							return type == ElementType.H1 || type == ElementType.H2;
						}).Slice.ToString();
						Document document2 = new Document(Path.GetFileNameWithoutExtension(item), item, section.Name, document.Headers, title, text);
						document2.LinkAnchors = FindLinkAnchors(text);
						_contents.Add(document2);
					}
					catch (Exception exception)
					{
						Log.Error(exception, "Error opening file {filename}", item);
					}
				}
			}
			_selectedItem.Value = _contents.FirstOrDefault()?.Identifier;
		}
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, delegate(UIPanelBuilder builder)
		{
			List<UIPanelBuilder.ListItem<Document>> list = new List<UIPanelBuilder.ListItem<Document>>();
			list.AddRange(_contents.Select((Document doc) => new UIPanelBuilder.ListItem<Document>(doc.Identifier, doc, doc.Section, doc.Title)));
			builder.AddListDetail(list, _selectedItem, delegate(UIPanelBuilder builder2, Document document3)
			{
				if (document3 == null)
				{
					builder2.AddLabel("Please make a selection.");
				}
				else
				{
					BuildDetailForDocument(builder2, document3);
				}
			}, 200f);
		});
	}

	private void BuildDetailForDocument(UIPanelBuilder builder, Document document)
	{
		RectTransform rectTransform = builder.AddTextArea(document.TextMeshMarkup, HandleLinkClicked);
		if (_linkedItem != null && document.Identifier == _linkedItem.Item1)
		{
			StartCoroutine(ScrollToPosition(rectTransform, _linkedItem.Item2));
			_linkedItem = null;
		}
	}

	private static IEnumerator ScrollToPosition(RectTransform rectTransform, string linkId)
	{
		yield return null;
		TMP_Text componentInChildren = rectTransform.GetComponentInChildren<TMP_Text>();
		for (int i = 0; i < componentInChildren.textInfo.linkCount; i++)
		{
			TMP_LinkInfo tMP_LinkInfo = componentInChildren.textInfo.linkInfo[i];
			if (!(tMP_LinkInfo.GetLinkID() != linkId))
			{
				int linkTextfirstCharacterIndex = tMP_LinkInfo.linkTextfirstCharacterIndex;
				TMP_CharacterInfo tMP_CharacterInfo = componentInChildren.textInfo.characterInfo[linkTextfirstCharacterIndex];
				((RectTransform)componentInChildren.transform).anchoredPosition = new Vector2(0f, 0f - tMP_CharacterInfo.topLeft.y);
				break;
			}
		}
	}

	private static HashSet<string> FindLinkAnchors(string markup)
	{
		HashSet<string> hashSet = new HashSet<string>();
		MatchCollection matchCollection = Regex.Matches(markup, "<link=\\\"(.*?)\\\">");
		for (int i = 0; i < matchCollection.Count; i++)
		{
			Group obj = matchCollection[i].Groups[1];
			hashSet.Add(obj.Value);
		}
		return hashSet;
	}

	private void HandleLinkClicked(string link)
	{
		if (!link.StartsWith("ett:"))
		{
			Log.Warning("Unrecognized link: {link}", link);
			return;
		}
		string id = link.Split(":")[1];
		JumpToLinkId(id);
	}

	private void JumpToLinkId(string id)
	{
		string text = "anchor:" + id;
		foreach (Document content in _contents)
		{
			if (content.LinkAnchors.Contains(text))
			{
				_selectedItem.Value = content.Identifier;
				_linkedItem = new Tuple<string, string>(content.Identifier, text);
				_panel.Rebuild();
				return;
			}
		}
		Log.Warning("No link with id: {id}", id);
	}
}
