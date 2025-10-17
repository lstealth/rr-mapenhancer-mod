using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Core.Diagnostics;
using GalaSoft.MvvmLight.Messaging;
using Model.Ops.Timetable;
using TMPro;
using UI.Builder;
using UnityEngine;

namespace UI.Timetable;

public class TextTimetableEditor : BaseTimetableEditor
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TimetableEditorRefresh
	{
	}

	private string _content;

	private string _status;

	private TooltipInfo _statusTooltip;

	private bool _canSave;

	private Coroutine _refreshCoroutine;

	public TextTimetableEditor(TimetableController timetableController, TimetableEditorWindow timetableEditorWindow)
		: base(timetableController, timetableEditorWindow)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
		if (_refreshCoroutine != null)
		{
			EditorWindow.StopCoroutine(_refreshCoroutine);
			_refreshCoroutine = null;
		}
	}

	public void Build(UIPanelBuilder builder)
	{
		_content = TimetableController.CurrentDocument.Source;
		RefreshTimetableForTextEditor();
		UIPanelBuilder rootBuilder = builder;
		builder.AddMultilineTextEditor(_content, "Enter Timetable Text", TimetableTextDidChange, delegate
		{
		}).FlexibleHeight();
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.RebuildOnEvent<TimetableEditorRefresh>();
			uIPanelBuilder.AddLabel(_status).TextWrap(TextOverflowModes.Ellipsis, TextWrappingModes.Normal).VerticalTextAlignment(VerticalAlignmentOptions.Middle)
				.FlexibleWidth()
				.Tooltip(_statusTooltip.Title, _statusTooltip.Text);
		}).Height(30f);
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.RebuildOnEvent<TimetableEditorRefresh>();
			uIPanelBuilder.AddButton("Apply Timetable", Save).Tooltip("Apply", "Immediately make this the current timetable.").Disable(!_canSave);
			uIPanelBuilder.Spacer();
			uIPanelBuilder.AddDropdown(new List<string> { "Extras", "Append Sample" }, 0, delegate(int index)
			{
				if (index != 0 && index == 1)
				{
					string text = _content + "\n\n// Sample Timetable\n\n17 W 1P: SY 7:30, DB +5, WM +11, WH +10, EL +6, BR +14 (18), HW +9, AJ +4, AM +15, NA +25, TO +11, RH +8, AN +12\n\n18 E 1P: AN 6:50, RH +12, TO +8, NA +13, AM +21, AJ +15, HW +4, BR +14 (17), EL +9, WH +8, WM +8, DB +11, SY +5";
					TimetableController.SetCurrent(text.Trim());
					rootBuilder.Rebuild();
				}
			}).Width(200f);
			uIPanelBuilder.AddLabel("<u>Station Codes</u>").VerticalTextAlignment(VerticalAlignmentOptions.Middle).Tooltip(GetStationsTooltip);
		}).Height(30f);
	}

	private TooltipInfo GetStationsTooltip()
	{
		IReadOnlyList<TimetableStation> allStations = TimetableController.GetAllStations();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (TimetableStation item in allStations)
		{
			stringBuilder.AppendLine($"{item}: {item.DisplayName}");
		}
		return new TooltipInfo("Stations", stringBuilder.ToString());
	}

	private void TimetableTextDidChange(string str)
	{
		_content = str;
		if (_refreshCoroutine != null)
		{
			EditorWindow.StopCoroutine(_refreshCoroutine);
		}
		_refreshCoroutine = EditorWindow.StartCoroutine(Refresh());
		IEnumerator Refresh()
		{
			yield return new WaitForSecondsRealtime(1f);
			RefreshTimetableForTextEditor();
			_refreshCoroutine = null;
		}
	}

	private void RefreshTimetableForTextEditor()
	{
		StringDiagnosticCollector stringDiagnosticCollector = new StringDiagnosticCollector();
		if (TimetableController.TryRead(_content, out var output, stringDiagnosticCollector))
		{
			_status = $"{output.Trains.Count} trains";
			_statusTooltip = TooltipInfo.Empty;
			_canSave = true;
		}
		else
		{
			string text = stringDiagnosticCollector.ToString();
			_status = "<sprite name=Warning> " + text.Replace("\n", "; ");
			_statusTooltip = new TooltipInfo("Timetable Issues", text);
			_canSave = false;
		}
		if (_content != TimetableController.CurrentDocument.Source)
		{
			_status = "Unsaved changes. " + _status;
		}
		Messenger.Default.Send(default(TimetableEditorRefresh));
	}

	private void Save()
	{
		TimetableController.SetCurrent(_content);
	}
}
