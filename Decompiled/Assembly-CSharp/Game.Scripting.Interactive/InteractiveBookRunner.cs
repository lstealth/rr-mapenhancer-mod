using System;
using System.Collections;
using System.IO;
using Character;
using KeyValue.Runtime;
using MoonSharp.Interpreter;
using Serilog;
using UnityEngine;

namespace Game.Scripting.Interactive;

public class InteractiveBookRunner : MonoBehaviour
{
	[MoonSharpUserData]
	private class BookContext
	{
		public readonly IPageUI ui;

		public ScriptProperties properties;

		public ScriptWorld world;

		public Action request_rerun;

		public Action mark_complete;

		public BookContext(IPageUI ui, ScriptProperties properties, ScriptWorld world, Action request_rerun, Action mark_complete)
		{
			this.ui = ui;
			this.properties = properties;
			this.world = world;
			this.request_rerun = request_rerun;
			this.mark_complete = mark_complete;
		}
	}

	private string _basePath;

	private string _bookName;

	private string _bookFilename;

	private IPageUI _pageUI;

	private IKeyValueObject _keyValueObject;

	private ScriptManager _script;

	private DateTime _lastModifiedTime;

	private UnityEngine.Coroutine _bookCoroutine;

	private Closure _runClosure;

	public string BookTitle { get; private set; }

	public string CloseMessage { get; private set; }

	public event Action OnWillReload;

	private void Awake()
	{
		UserData.RegisterType<IPageUI>(InteropAccessMode.Default, "Book.PageUI");
		UserData.RegisterType<BookContext>(InteropAccessMode.Default, "Book.Context");
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		StopBookCoroutine();
	}

	private void OnDestroy()
	{
		_script?.Dispose();
		_script = null;
	}

	private string GetLuaFilePath()
	{
		return Path.Combine(_basePath, _bookFilename);
	}

	private string[] GetModulePaths()
	{
		return new string[2]
		{
			Path.Combine(_basePath, "?.lua"),
			ScriptManager.BuiltInModulesPath
		};
	}

	public bool Open(string basePath, string bookName, IPageUI pageUI, IKeyValueObject keyValueObject)
	{
		_basePath = basePath;
		_bookName = bookName;
		_bookFilename = bookName + ".lua";
		_pageUI = pageUI;
		_keyValueObject = keyValueObject;
		return PostOpen();
	}

	private bool PostOpen()
	{
		string luaFilePath = GetLuaFilePath();
		if (!TryLoadFromFile(luaFilePath))
		{
			_pageUI.say("An error occurred while opening " + _bookName + ". Please submit a bug report including your log file!");
			Log.Error("Can't open book");
			return false;
		}
		StopStart();
		return true;
	}

	public void Close()
	{
		StopBookCoroutine();
	}

	public bool ReloadIfModified()
	{
		if (_bookFilename == null)
		{
			return false;
		}
		if (File.GetLastWriteTime(GetLuaFilePath()) != _lastModifiedTime)
		{
			return Reload();
		}
		return false;
	}

	public bool Reload()
	{
		this.OnWillReload?.Invoke();
		StopBookCoroutine();
		_script?.Dispose();
		_script = null;
		return PostOpen();
	}

	private void MarkComplete()
	{
		_keyValueObject["complete"] = true;
	}

	private void StopStart()
	{
		StopBookCoroutine();
		_bookCoroutine = StartCoroutine(RunBook());
	}

	private void StopBookCoroutine()
	{
		if (_bookCoroutine != null)
		{
			StopCoroutine(_bookCoroutine);
		}
		_bookCoroutine = null;
	}

	private IEnumerator RunBook()
	{
		_pageUI.clear();
		yield return WaitForStartup();
		Log.Debug("Running...");
		_script.ClearLastRunError();
		BookContext bookContext = new BookContext(_pageUI, new ScriptProperties(_keyValueObject, _script), ScriptWorld.Shared, StopStart, MarkComplete);
		yield return _script.Run(_runClosure, bookContext);
		Log.Debug("Run completed.");
		ScriptManager.ErrorInfo? lastRunError = _script.LastRunError;
		if (lastRunError.HasValue)
		{
			string text = lastRunError.GetValueOrDefault().DecoratedMessage.Replace(_basePath, ".");
			_pageUI.say("<sprite name=Warning> An error occurred. Please try again and if the problem persists report this bug and include your log file. Thank you!\n\nError Details: `" + text + "`");
			_pageUI.reload_button();
		}
		_bookCoroutine = null;
	}

	private static IEnumerator WaitForStartup()
	{
		PlayerController playerController = CameraSelector.shared.character;
		while (playerController.character.IsInAir)
		{
			yield return new WaitForSeconds(1f);
		}
	}

	private void PrepareScriptIfNeeded()
	{
		if (_script == null)
		{
			string[] modulePaths = GetModulePaths();
			_script = new ScriptManager(null, this, modulePaths);
		}
	}

	private bool TryLoadFromFile(string luaFile)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(luaFile);
		_lastModifiedTime = File.GetLastWriteTime(luaFile);
		_runClosure = null;
		PrepareScriptIfNeeded();
		try
		{
			string source = File.ReadAllText(luaFile);
			DynValue dynValue = _script.Load(source, fileNameWithoutExtension);
			Table table = dynValue.Table;
			if (table == null)
			{
				throw new Exception("Expected table from module, got: " + dynValue.Type);
			}
			BookTitle = (string)table["title"];
			CloseMessage = (string)table["close_message"];
			string text = (string)table["extension_type"];
			_runClosure = (Closure)table["run"];
			if (_runClosure == null)
			{
				throw new Exception("Module has no 'run' closure");
			}
			Debug.Log("Loaded book: " + base.name + " with type " + text);
		}
		catch (SyntaxErrorException ex)
		{
			Debug.LogError("Syntax error in book: " + ex.DecoratedMessage);
			Log.Error(ex, "Syntax error in book: {e}", ex.DecoratedMessage);
			return false;
		}
		catch (ScriptRuntimeException ex2)
		{
			Debug.LogError("Runtime error in book: " + ex2.DecoratedMessage);
			Log.Error(ex2, "Runtime error in book: {e}", ex2.DecoratedMessage);
			return false;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to load book: {suite}", luaFile);
			return false;
		}
		return true;
	}

	internal static bool TryRun(Closure closure, string debugHint)
	{
		try
		{
			Log.Information("TryRun start \"{hint}\"", debugHint);
			closure.Call();
			Log.Information("TryRun end");
			return true;
		}
		catch (SyntaxErrorException ex)
		{
			if (Application.isPlaying)
			{
				Log.Error(ex, "Syntax error: {e}", ex.DecoratedMessage);
			}
			else
			{
				Debug.LogError("Syntax error: " + ex.DecoratedMessage);
			}
		}
		catch (ScriptRuntimeException ex2)
		{
			if (Application.isPlaying)
			{
				Log.Error(ex2, "Runtime error: {e}", ex2.DecoratedMessage);
			}
			else
			{
				Debug.LogError($"Runtime error: {ex2.DecoratedMessage}\n{ex2}");
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Unknown error in Lua stack");
			return false;
		}
		return true;
	}
}
