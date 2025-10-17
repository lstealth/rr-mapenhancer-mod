using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TMPro;
using UnityEngine;

namespace Effects.Decals;

public class CanvasDecalRenderer : MonoBehaviour
{
	private class Record
	{
		public int ReferenceCount = 1;

		public Texture Texture { get; }

		public Record(Texture texture)
		{
			Texture = texture;
		}
	}

	private class Request
	{
		public Vector2 DecalProjectorSize;

		public string Template;

		public string Text;

		public TaskCompletionSource<CanvasDecal> TaskCompletionSource;

		public CancellationToken CancellationToken;

		public Request(Vector2 decalProjectorSize, string template, string text, TaskCompletionSource<CanvasDecal> tcs, CancellationToken cancellationToken)
		{
			DecalProjectorSize = decalProjectorSize;
			Template = template;
			Text = text;
			TaskCompletionSource = tcs;
			CancellationToken = cancellationToken;
		}
	}

	public Camera canvasCamera;

	public RectTransform container;

	public int pixelsPerMeterLarge = 75;

	public int pixelsPerMeterSmall = 150;

	public Material referenceMaterial;

	private const float StandardContainerWidth = 1000f;

	private readonly Dictionary<string, CanvasGroup> _canvasGroups = new Dictionary<string, CanvasGroup>();

	private Serilog.ILogger _log;

	private readonly Dictionary<string, Record> _records = new Dictionary<string, Record>();

	private Coroutine _coroutine;

	private Queue<Request> _queue = new Queue<Request>();

	private static CanvasDecalRenderer _instance;

	public static CanvasDecalRenderer Shared
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindFirstObjectByType<CanvasDecalRenderer>();
			}
			return _instance;
		}
	}

	private void Awake()
	{
		canvasCamera.enabled = false;
		_log = Log.ForContext<CanvasDecalRenderer>();
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	private IEnumerator WorkQueue()
	{
		while (_queue.Count > 0)
		{
			double num = BudgetTime();
			do
			{
				Request request = _queue.Dequeue();
				Handle(request);
			}
			while (_queue.Count > 0 && BudgetTime() - num < 0.004999999888241291);
			yield return null;
		}
		_coroutine = null;
	}

	private static double BudgetTime()
	{
		return Time.realtimeSinceStartupAsDouble;
	}

	private void Handle(Request request)
	{
		if (request.CancellationToken.IsCancellationRequested)
		{
			request.TaskCompletionSource.SetCanceled();
			return;
		}
		try
		{
			CanvasDecal result = Render(request.DecalProjectorSize, request.Template, request.Text);
			request.TaskCompletionSource.SetResult(result);
		}
		catch (Exception exception)
		{
			request.TaskCompletionSource.SetException(exception);
		}
	}

	private void PrepareCanvasGroups()
	{
		if (_canvasGroups.Count <= 0)
		{
			CanvasGroup[] componentsInChildren = container.GetComponentsInChildren<CanvasGroup>();
			foreach (CanvasGroup canvasGroup in componentsInChildren)
			{
				_canvasGroups[canvasGroup.name] = canvasGroup;
			}
		}
	}

	public Task<CanvasDecal> Render(Vector2 decalProjectorSize, string template, string text, CancellationToken cancellationToken)
	{
		TaskCompletionSource<CanvasDecal> taskCompletionSource = new TaskCompletionSource<CanvasDecal>();
		_queue.Enqueue(new Request(decalProjectorSize, template, text, taskCompletionSource, cancellationToken));
		if (_coroutine == null)
		{
			_coroutine = StartCoroutine(WorkQueue());
		}
		return taskCompletionSource.Task;
	}

	private CanvasDecal Render(Vector2 decalProjectorSize, string template, string text)
	{
		float num = decalProjectorSize.y / decalProjectorSize.x;
		int num2 = Mathf.CeilToInt(Mathf.Lerp(pixelsPerMeterSmall, pixelsPerMeterLarge, Mathf.InverseLerp(1f, 10f, decalProjectorSize.x)) * decalProjectorSize.x);
		int num3 = Mathf.CeilToInt(num * (float)num2);
		string text2 = $"{template}/{text}/{num2}x{num3}";
		if (_records.TryGetValue(text2, out var value))
		{
			value.ReferenceCount++;
			return new CanvasDecal(text2, value.Texture, this);
		}
		PrepareCanvasGroups();
		bool flag = false;
		foreach (KeyValuePair<string, CanvasGroup> canvasGroup2 in _canvasGroups)
		{
			canvasGroup2.Deconstruct(out var key, out var value2);
			string text3 = key;
			CanvasGroup canvasGroup = value2;
			bool flag2 = text3 == template;
			canvasGroup.alpha = (flag2 ? 1 : 0);
			if (flag2)
			{
				flag = true;
				canvasGroup.GetComponentInChildren<TMP_Text>().text = text;
			}
		}
		if (!flag)
		{
			_log.Warning("Template not found: {template}, decal will be blank", template);
		}
		float num4 = 1000f;
		container.localScale = Vector3.one * ((float)num2 / num4);
		container.sizeDelta = new Vector2(num4, num * num4);
		_log.Debug("Build {key}, size = {size}", text2, num2 * num3);
		RenderTexture temporary = RenderTexture.GetTemporary(num2, num3, 8, RenderTextureFormat.R8);
		canvasCamera.enabled = true;
		canvasCamera.targetTexture = temporary;
		canvasCamera.Render();
		canvasCamera.targetTexture = null;
		canvasCamera.enabled = false;
		Texture2D texture2D = RenderTextureToTexture2D(temporary);
		texture2D.name = "CanvasDecalRendererTexture";
		RenderTexture.ReleaseTemporary(temporary);
		value = new Record(texture2D);
		_records[text2] = value;
		return new CanvasDecal(text2, texture2D, this);
	}

	private static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
	{
		int width = renderTexture.width;
		int height = renderTexture.height;
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = renderTexture;
		try
		{
			Texture2D texture2D = new Texture2D(width, height, TextureFormat.R8, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, recalculateMipMaps: false);
			texture2D.Apply(updateMipmaps: false);
			return texture2D;
		}
		finally
		{
			RenderTexture.active = active;
		}
	}

	internal void Return(CanvasDecal canvasDecal)
	{
		string key = canvasDecal.Key;
		if (!_records.TryGetValue(key, out var value))
		{
			_log.Error("Return called with no existing key {key}", key);
			return;
		}
		if (value.ReferenceCount <= 0)
		{
			_log.Error("Return called with count {count} for {key}", value.ReferenceCount, key);
			return;
		}
		value.ReferenceCount--;
		if (value.ReferenceCount <= 0)
		{
			_log.Debug("Destroy {key}", key);
			UnityEngine.Object.Destroy(canvasDecal.Texture);
			_records.Remove(key);
		}
	}
}
