using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Model.Ops.Timetable;
using Serilog;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Timetable;

public class TimetableEditorWindow : MonoBehaviour, IProgrammaticWindow, IBuilderWindow
{
	private Window _window;

	private UIPanel _panel;

	private TimetableController _timetableController;

	private UIPanelBuilder? _rootBuilder;

	private VisualTimetableEditor _visualEditor;

	public string WindowIdentifier => "TimetableEditor";

	public Vector2Int DefaultSize => new Vector2Int(700, 550);

	public Window.Position DefaultPosition => Window.Position.Center;

	public Window.Sizing Sizing => Window.Sizing.Resizable(DefaultSize);

	public UIBuilderAssets BuilderAssets { get; set; }

	public static TimetableEditorWindow Shared => WindowManager.Shared.GetWindow<TimetableEditorWindow>();

	public void Show()
	{
		Populate();
		_window.ShowWindow();
		_window.DelegateRequestClose = HandleWindowRequestClose;
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnEnable()
	{
		Messenger.Default.Register<TimetableDidChange>(this, OnTimetableDidChange);
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		_panel?.Dispose();
		_panel = null;
		_visualEditor?.Dispose();
		_visualEditor = null;
	}

	private void Populate()
	{
		if ((object)_timetableController == null)
		{
			_timetableController = TimetableController.Shared;
		}
		_window.Title = "Timetable Editor";
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, Build);
	}

	private void Build(UIPanelBuilder builder)
	{
		_rootBuilder = builder;
		if (_visualEditor == null)
		{
			_visualEditor = new VisualTimetableEditor(_timetableController, this);
		}
		_visualEditor.Build(builder);
	}

	private void HandleWindowRequestClose()
	{
		if (_visualEditor == null)
		{
			_window.CloseWindow();
			return;
		}
		_visualEditor.HandleWindowRequestClose(delegate
		{
			_window.CloseWindow();
		});
	}

	private void OnTimetableDidChange(TimetableDidChange change)
	{
		if (!_window.IsShown)
		{
			return;
		}
		Log.Information("TimetableDidChange with unsaved changes = {hasUnsavedChanges} ", _visualEditor.HasUnsavedChanges);
		if (_visualEditor.HasUnsavedChanges)
		{
			ModalAlertController.Present("Timetable Changed", "Another player has changed the timetable. Do you want to ignore these changes, or discard your changes and reload?", new List<(bool, string)>
			{
				(false, "Ignore"),
				(true, "Discard & Reload")
			}, delegate(bool reload)
			{
				if (reload)
				{
					Rebuild();
				}
			});
		}
		else
		{
			Rebuild();
		}
	}

	private void Rebuild()
	{
		_rootBuilder.Value.Rebuild();
	}
}
