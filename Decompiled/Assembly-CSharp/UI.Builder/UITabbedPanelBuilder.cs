using System;
using UI.TabView;

namespace UI.Builder;

public readonly struct UITabbedPanelBuilder
{
	private readonly UI.TabView.TabView _tabView;

	public UITabbedPanelBuilder(UI.TabView.TabView tabView)
	{
		_tabView = tabView;
	}

	public UITabbedPanelBuilder AddTab(string title, string tabId, Action<UIPanelBuilder> closure)
	{
		_tabView.AddTab(title, tabId, closure);
		return this;
	}

	public void Finish()
	{
		_tabView.FinishedAddingTabs();
	}
}
