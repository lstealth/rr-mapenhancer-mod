using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using Serilog;
using UnityEngine;

namespace Game.Scripting;

public class ScriptManager : IDisposable
{
	public readonly struct ErrorInfo
	{
		public readonly string Message;

		public readonly string DecoratedMessage;

		public ErrorInfo(string message, string decoratedMessage)
		{
			Message = message;
			DecoratedMessage = decoratedMessage;
		}
	}

	private Script _script;

	private readonly IPlayer _player;

	private readonly MonoBehaviour _hostComponent;

	private UnityEngine.Coroutine _coroutine;

	private readonly ScriptLoaderBase _scriptLoader;

	public ErrorInfo? LastRunError;

	private static Script _currentScript;

	private static bool _initialized;

	internal static Script CurrentScript
	{
		get
		{
			if (_currentScript == null)
			{
				throw new InvalidOperationException("Only available while ScriptManager is executing a script");
			}
			return _currentScript;
		}
	}

	public static string BuiltInModulesPath => Path.Combine(Application.streamingAssetsPath, "LuaModules", "?.lua");

	public ScriptManager(IPlayer player, MonoBehaviour hostComponent, string[] modulePaths = null)
	{
		_player = player;
		_hostComponent = hostComponent;
		_scriptLoader = ((modulePaths != null) ? new FileSystemScriptLoader
		{
			ModulePaths = modulePaths
		} : null);
		StaticInit();
	}

	public static implicit operator Script(ScriptManager sm)
	{
		return sm._script;
	}

	public void Dispose()
	{
		Stop();
	}

	public DynValue Load(string source, string filename)
	{
		Reset();
		try
		{
			_currentScript = _script;
			return _script.DoString(source, null, filename);
		}
		catch (SyntaxErrorException ex)
		{
			Debug.LogError("Syntax Error: " + ex.DecoratedMessage);
			Debug.LogException(ex);
			throw;
		}
		finally
		{
			_currentScript = null;
		}
	}

	public void Run(string closureName, params object[] args)
	{
		Stop();
		_coroutine = _hostComponent.StartCoroutine(RunFromCoroutine(closureName, args));
	}

	public void Stop()
	{
		if (_coroutine != null && _hostComponent != null)
		{
			_hostComponent.StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	public IEnumerator RunFromCoroutine(string closureName, params object[] args)
	{
		Log.Debug("Script: Start {closureName}", closureName);
		if (!(_script.Globals[closureName] is Closure closure))
		{
			Log.Error("Script: Error: {closureName} not found", closureName);
			yield break;
		}
		yield return Run(closure, args);
		Log.Debug("Script: Finish {closureName}", closureName);
	}

	public IEnumerator Run(Closure closure, params object[] args)
	{
		MoonSharp.Interpreter.Coroutine cor = _script.CreateCoroutine(closure).Coroutine;
		while (cor.State != CoroutineState.Dead)
		{
			DynValue dynValue;
			try
			{
				_currentScript = _script;
				dynValue = ((args != null) ? cor.Resume(args) : cor.Resume());
				args = null;
			}
			catch (ScriptRuntimeException ex)
			{
				LastRunError = new ErrorInfo(ex.Message, ex.DecoratedMessage);
				if (Application.isPlaying)
				{
					Log.Error(ex, "Runtime Error: {e}", ex.DecoratedMessage);
				}
				else
				{
					Debug.LogError("Runtime Error: " + ex.DecoratedMessage);
				}
				break;
			}
			catch (Exception ex2)
			{
				LastRunError = new ErrorInfo(ex2.Message, ex2.Message);
				if (Application.isPlaying)
				{
					Log.Error(ex2, "Unexpected exception during Run()");
				}
				else
				{
					Debug.LogError($"Unexpected exception during Run():\n{ex2}");
				}
				break;
			}
			finally
			{
				_currentScript = null;
			}
			if (dynValue != null && dynValue.Type == DataType.Number)
			{
				float seconds = (float)dynValue.Number;
				yield return new WaitForSeconds(seconds);
			}
			else
			{
				yield return null;
			}
		}
	}

	private void Reset()
	{
		CoreModules coreModules = CoreModules.Preset_SoftSandbox;
		if (_scriptLoader != null)
		{
			coreModules |= CoreModules.LoadMethods;
		}
		Script script = new Script(coreModules);
		script.Options.DebugPrint = delegate(string s)
		{
			Log.Information("Lua: {s}", s);
		};
		script.Options.ScriptLoader = _scriptLoader;
		object key = "Location";
		script.Globals[key] = typeof(ScriptLocation);
		_script = script;
		ScriptVector3.AddVec3Type(_script);
	}

	private static void StaticInit()
	{
		if (!_initialized)
		{
			Script.GlobalOptions.RethrowExceptionNested = true;
			UserData.RegisterType<ScriptWorld>(InteropAccessMode.Default, "World");
			UserData.RegisterType<ScriptCar>(InteropAccessMode.Default, "Car");
			UserData.RegisterType<ScriptBaseLocomotive>(InteropAccessMode.Default, "BaseLocomotive");
			UserData.RegisterType<ScriptCarAir>(InteropAccessMode.Default, "CarAir");
			UserData.RegisterType<ScriptLocation>(InteropAccessMode.Default, "Location");
			UserData.RegisterType<ScriptPassengerStop>(InteropAccessMode.Default, "PassengerStop");
			UserData.RegisterType<ScriptProperties>(InteropAccessMode.Default, "Properties");
			UserData.RegisterType<ScriptDisposable>(InteropAccessMode.Default, "Disposable");
			UserData.RegisterType<ScriptWaybill>(InteropAccessMode.Default, "Waybill");
			_initialized = true;
		}
	}

	public IEnumerable<string> GetGlobalClosureNames()
	{
		foreach (DynValue key in _script.Globals.Keys)
		{
			if (_script.Globals[key] is Closure)
			{
				yield return key.String;
			}
		}
	}

	public void ClearLastRunError()
	{
		LastRunError = null;
	}
}
