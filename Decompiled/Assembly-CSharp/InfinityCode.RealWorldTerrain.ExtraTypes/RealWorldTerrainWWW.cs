using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace InfinityCode.RealWorldTerrain.ExtraTypes;

public class RealWorldTerrainWWW
{
	public enum RequestType
	{
		direct,
		www
	}

	public Action<RealWorldTerrainWWW> OnComplete;

	public object customData;

	private UnityWebRequest www;

	private RequestType type;

	private byte[] _bytes;

	private string _error;

	private bool _isDone;

	private string _url;

	private string responseHeadersString;

	private string _id;

	private IEnumerator waitResponse;

	private RealWorldTerrainWWWHelper _helper;

	public byte[] bytes
	{
		get
		{
			if (type == RequestType.www)
			{
				return www.downloadHandler.data;
			}
			return _bytes;
		}
	}

	public int bytesDownloaded
	{
		get
		{
			if (type == RequestType.www)
			{
				return (int)www.downloadedBytes;
			}
			if (_bytes == null)
			{
				return 0;
			}
			return _bytes.Length;
		}
	}

	public string error
	{
		get
		{
			if (type == RequestType.www)
			{
				return www.error;
			}
			return _error;
		}
	}

	private RealWorldTerrainWWWHelper helper
	{
		get
		{
			if (_helper != null)
			{
				return _helper;
			}
			GameObject gameObject = new GameObject("WWW Helper");
			return _helper = gameObject.AddComponent<RealWorldTerrainWWWHelper>();
		}
	}

	public string id => _id;

	public bool isDone
	{
		get
		{
			if (type == RequestType.www)
			{
				return www.isDone;
			}
			return _isDone;
		}
	}

	private bool isPlaying => true;

	public Dictionary<string, string> responseHeaders
	{
		get
		{
			if (!isDone)
			{
				throw new UnityException("WWW is not finished downloading yet");
			}
			if (type == RequestType.www)
			{
				return www.GetResponseHeaders();
			}
			return ParseHTTPHeaderString(responseHeadersString);
		}
	}

	public string text
	{
		get
		{
			if (type == RequestType.www)
			{
				return www.downloadHandler.text;
			}
			if (_bytes == null)
			{
				return null;
			}
			return GetTextEncoder().GetString(_bytes, 0, _bytes.Length);
		}
	}

	public string url => _url;

	public RealWorldTerrainWWW(string url)
	{
		_url = url;
		type = RequestType.www;
		www = UnityWebRequest.Get(url);
		www.SendWebRequest();
		if (isPlaying)
		{
			helper.StartCoroutine(WaitResponse());
		}
	}

	public RealWorldTerrainWWW(string url, RequestType type, string reqID)
	{
		this.type = type;
		_url = url;
		_id = reqID;
		if (type == RequestType.www)
		{
			www = UnityWebRequest.Get(url);
			www.SendWebRequest();
			if (isPlaying)
			{
				helper.StartCoroutine(WaitResponse());
			}
		}
	}

	private void CheckWWWComplete()
	{
		if (www.isDone)
		{
			Finish();
		}
	}

	public void Dispose()
	{
		if (www != null && !www.isDone)
		{
			www.Dispose();
		}
		www = null;
		customData = null;
		if (waitResponse != null)
		{
			helper.StopCoroutine(waitResponse);
		}
		if (isPlaying)
		{
			UnityEngine.Object.Destroy(helper.gameObject);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(helper.gameObject);
		}
	}

	public static string EscapeURL(string s)
	{
		return UnityWebRequest.EscapeURL(s);
	}

	private void Finish()
	{
		if (OnComplete != null)
		{
			OnComplete(this);
		}
		Dispose();
	}

	private Encoding GetTextEncoder()
	{
		if (responseHeaders.TryGetValue("CONTENT-TYPE", out var value))
		{
			int num = value.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
			if (num > -1)
			{
				int num2 = value.IndexOf('=', num);
				if (num2 > -1)
				{
					char[] trimChars = new char[2] { '\'', '"' };
					string text = value.Substring(num2 + 1).Trim().Trim(trimChars)
						.Trim();
					int num3 = text.IndexOf(';');
					if (num3 > -1)
					{
						text = text.Substring(0, num3);
					}
					try
					{
						return Encoding.GetEncoding(text);
					}
					catch (Exception)
					{
						Debug.Log("Unsupported encoding: '" + text + "'");
					}
				}
			}
		}
		return Encoding.UTF8;
	}

	public void LoadImageIntoTexture(Texture2D tex)
	{
		if (tex == null)
		{
			throw new Exception("Texture is null");
		}
		if (type == RequestType.www)
		{
			tex.LoadImage(www.downloadHandler.data);
		}
		else
		{
			tex.LoadImage(_bytes);
		}
	}

	internal static Dictionary<string, string> ParseHTTPHeaderString(string input)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (string.IsNullOrEmpty(input))
		{
			return dictionary;
		}
		StringReader stringReader = new StringReader(input);
		int num = 0;
		while (true)
		{
			string text = stringReader.ReadLine();
			if (text == null)
			{
				break;
			}
			if (num++ == 0 && text.StartsWith("HTTP"))
			{
				dictionary["STATUS"] = text;
				continue;
			}
			int num2 = text.IndexOf(": ");
			if (num2 != -1)
			{
				string key = text.Substring(0, num2).ToUpper();
				string value = text.Substring(num2 + 2);
				dictionary[key] = value;
			}
		}
		return dictionary;
	}

	public void SetBytes(string responseHeadersString, byte[] _bytes)
	{
		if (type == RequestType.www)
		{
			throw new Exception("RealWorldTerrainWWW.SetBytes available only for type = direct.");
		}
		this.responseHeadersString = responseHeadersString;
		this._bytes = _bytes;
		_isDone = true;
		Finish();
	}

	public void SetError(string errorStr)
	{
		if (type == RequestType.www)
		{
			throw new Exception("RealWorldTerrainWWW.SetError available only for type = direct.");
		}
		_error = errorStr;
		_isDone = true;
		Finish();
	}

	private IEnumerator WaitResponse()
	{
		yield return www;
		waitResponse = null;
		Finish();
	}
}
