using System.Collections.Generic;
using Audio;
using Game.Messages;
using Game.State;
using Serilog;
using UI.Common;
using UnityEngine;

namespace Game.Notices;

public class NoticeManager : MonoBehaviour
{
	private class Entry
	{
		public NoticeRow Row;

		public string Content;
	}

	[SerializeField]
	private NoticeRow rowTemplate;

	[SerializeField]
	private RectTransform rowContainer;

	private readonly Dictionary<string, Entry> _notices = new Dictionary<string, Entry>();

	private static NoticeManager _shared;

	public static NoticeManager Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = Object.FindObjectOfType<NoticeManager>();
			}
			return _shared;
		}
	}

	private void Awake()
	{
		rowTemplate.gameObject.SetActive(value: false);
		rowContainer.DestroyChildrenExcept(rowTemplate);
	}

	public void PostEphemeral(EntityReference entity, string key, string content)
	{
		StateManager.AssertIsHost();
		StateManager.ApplyLocal(new PostNoticeEphemeral(new SerializableEntityReference(entity), key, content));
	}

	public void Clear()
	{
		_notices.Clear();
		rowContainer.DestroyChildrenExcept(rowTemplate);
	}

	public void Handle(PostNoticeEphemeral post)
	{
		EntityReference entity = new EntityReference(post.Entity);
		PostEphemeralLocal(entity, post.Key, post.Content);
	}

	public void PostEphemeralLocal(EntityReference entity, string contextualKey, string content)
	{
		if (entity.Type == EntityType.Player && entity.Id == PlayersManager.PlayerId.ToString())
		{
			Log.Debug("Ignoring Self Post {entity} {key} {content}", entity, contextualKey, content);
			return;
		}
		string key = $"{(int)entity.Type}//{entity.Id}//{contextualKey}";
		if (_notices.TryGetValue(key, out var value))
		{
			if (string.IsNullOrEmpty(content))
			{
				Log.Information("Notice Clear {entity} {key} {content}", entity, contextualKey, content);
				DismissRow(key);
				return;
			}
			if (value.Content == content)
			{
				return;
			}
			Log.Information("Notice Post {entity} {key} {content}", entity, contextualKey, content);
			DismissRow(key);
		}
		else if (string.IsNullOrEmpty(content))
		{
			return;
		}
		ScheduledAudioPlayer.PlaySoundLocal("telegraph-ditdit");
		Entry entry = new Entry();
		_notices[key] = entry;
		entry.Content = content;
		entry.Row = Object.Instantiate(rowTemplate, rowContainer);
		NoticeRow row = entry.Row;
		row.gameObject.SetActive(value: true);
		row.label.text = LabelTextForNotice(entity, content);
		row.OnDismiss = delegate
		{
			DismissRow(key);
		};
		row.SetOffscreen(offscreen: true, animated: false);
		row.SetOffscreen(offscreen: false, animated: true);
	}

	private string LabelTextForNotice(EntityReference entity, string content)
	{
		string text = Hyperlink.To(entity).ToString();
		return "<style=b>" + text + "</style>  <style=p>" + content + "</style>";
	}

	private void DismissRow(string key)
	{
		if (_notices.Remove(key, out var value))
		{
			value.Row.AnimatedDestroy();
		}
	}
}
