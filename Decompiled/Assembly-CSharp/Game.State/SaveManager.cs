using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using Game.Persistence;
using JetBrains.Annotations;
using Serilog;
using UI.Common;
using UnityEngine;

namespace Game.State;

public class SaveManager : MonoBehaviour
{
	[CanBeNull]
	private string _saveName;

	private Coroutine _autosaveCoroutine;

	public static SaveManager Shared => StateManager.Shared.SaveManager;

	public void WillUnloadMap()
	{
		if (_autosaveCoroutine != null)
		{
			StopCoroutine(_autosaveCoroutine);
		}
	}

	public void SetSaveNameForLaterLoading([CanBeNull] string saveName)
	{
		Log.Debug("Set SaveName: {saveName}", saveName);
		_saveName = saveName;
		RestartAutosave();
	}

	public void LoadFromSaveIfNeededOrInitialize()
	{
		string saveName = _saveName;
		if (!string.IsNullOrEmpty(saveName) && WorldStore.Exists(saveName))
		{
			Load(saveName);
			return;
		}
		Log.Debug("Load: (initialize new)");
		WorldStore.InitializeNew();
	}

	public void Load(string saveName)
	{
		Log.Debug("Load: {saveName}", saveName);
		try
		{
			WorldStore.Load(saveName);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			Log.Error(exception, "Error loading save");
			ModalAlertController.PresentOkay("Error loading save", "An error occurred while loading " + saveName + ".");
			throw;
		}
		RestartAutosave();
	}

	public void Save([CanBeNull] string saveName, string saveTag = null)
	{
		StateManager.DebugAssertIsHost();
		saveName = FinalizeSaveName(saveName, saveTag);
		try
		{
			WorldStore.Save(saveName);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			Log.Error(exception, "Error saving to {saveName}", saveName);
			ModalAlertController.PresentOkay("Error saving game", "An error occurred while saving to " + saveName);
		}
		RestartAutosave();
	}

	private string FinalizeSaveName([CanBeNull] string saveName, string saveTag)
	{
		if (string.IsNullOrWhiteSpace(saveName))
		{
			saveName = AutosaveLogic.ParseBaseName(_saveName);
			Log.Debug("Save: {saveName} (was empty)", saveName);
		}
		else
		{
			Log.Debug("Save: {saveName}", saveName);
		}
		if (string.IsNullOrWhiteSpace(saveName))
		{
			saveName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
		}
		saveName = ReplaceInvalidChars(saveName);
		if (string.IsNullOrWhiteSpace(saveTag))
		{
			return saveName;
		}
		return saveName + "_" + saveTag;
	}

	private static string ReplaceInvalidChars(string filename)
	{
		return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
	}

	private void RestartAutosave()
	{
		if (_autosaveCoroutine != null)
		{
			StopCoroutine(_autosaveCoroutine);
		}
		_autosaveCoroutine = StartCoroutine(AutosaveCoroutine());
	}

	private IEnumerator AutosaveCoroutine()
	{
		WaitForSeconds wait = new WaitForSeconds(300f);
		while (true)
		{
			yield return wait;
			Autosave();
		}
	}

	private void Autosave()
	{
		if (_saveName != null)
		{
			List<string> orderedSaveNames = (from info in WorldStore.FindSaveInfos()
				select info.Name).ToList();
			var (text, text2) = AutosaveLogic.MakeAutosaveTag(_saveName, orderedSaveNames);
			Log.Information("Autosave {saveName} -> {baseName}, tag = {tag}", _saveName, text, text2);
			Save(text, text2);
		}
	}

	public void GetLastSaveTimes(out DateTime? lastSaveTime, out DateTime? lastAutoSaveTime)
	{
		string text = FinalizeSaveName(null, null);
		lastSaveTime = WorldStore.TimestampForSave(text);
		if (text != null)
		{
			string saveName = FinalizeSaveName(text, "auto1");
			lastAutoSaveTime = WorldStore.TimestampForSave(saveName);
		}
		else
		{
			lastAutoSaveTime = null;
		}
	}
}
