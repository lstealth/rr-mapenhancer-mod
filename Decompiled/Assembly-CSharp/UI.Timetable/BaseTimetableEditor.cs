using System;
using Model.Ops.Timetable;

namespace UI.Timetable;

public abstract class BaseTimetableEditor : IDisposable
{
	protected readonly TimetableController TimetableController;

	protected readonly TimetableEditorWindow EditorWindow;

	protected BaseTimetableEditor(TimetableController timetableController, TimetableEditorWindow timetableEditorWindow)
	{
		TimetableController = timetableController;
		EditorWindow = timetableEditorWindow;
	}

	public virtual void Dispose()
	{
	}
}
