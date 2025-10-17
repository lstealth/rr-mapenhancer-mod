using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace Game.Scripting.Testing;

public class ScriptTestsWindow : MonoBehaviour, IProgrammaticWindow, IBuilderWindow
{
	private Window _window;

	private UIPanel _panel;

	private ScriptTestRunner _runner;

	private Coroutine _coroutine;

	private Dictionary<ScriptTestRunner.Test, UIPanelBuilder> _testBuilders = new Dictionary<ScriptTestRunner.Test, UIPanelBuilder>();

	private Coroutine _refreshCoroutine;

	private UIPanelBuilder? _testsScrollViewBuilder;

	public string WindowIdentifier => "ScriptTests";

	public Vector2Int DefaultSize => new Vector2Int(500, 600);

	public Window.Position DefaultPosition => Window.Position.UpperLeft;

	public Window.Sizing Sizing => Window.Sizing.Fixed(DefaultSize);

	public UIBuilderAssets BuilderAssets { get; set; }

	public static ScriptTestsWindow Shared => WindowManager.Shared.GetWindow<ScriptTestsWindow>();

	public void Show(ScriptTestRunner runner)
	{
		if (_runner != null)
		{
			_runner.OnTestStatusChange -= RunnerOnTestStatusChange;
			_runner.OnRunComplete -= RunnerOnRunComplete;
		}
		_runner = runner;
		_runner.OnTestStatusChange += RunnerOnTestStatusChange;
		_runner.OnRunComplete += RunnerOnRunComplete;
		Populate();
		_window.ShowWindow();
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
		_window.DelegateRequestClose = delegate
		{
		};
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate()
	{
		_window.Title = "Script Tests";
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, Build);
		_window.OnShownDidChange += HandleShownDidChange;
	}

	private void HandleShownDidChange(bool shown)
	{
		if (shown)
		{
			_refreshCoroutine = StartCoroutine(RefreshLoop());
			return;
		}
		StopCoroutine(_refreshCoroutine);
		_refreshCoroutine = null;
	}

	private IEnumerator RefreshLoop()
	{
		while (true)
		{
			yield return new WaitForSecondsRealtime(1f);
			if (_runner.LoadTestsIfModified())
			{
				Reload();
			}
		}
	}

	private void RunnerOnTestStatusChange(ScriptTestRunner.Test test)
	{
		if (_testBuilders.TryGetValue(test, out var value))
		{
			value.Rebuild();
		}
	}

	private void RunnerOnRunComplete(int successful, int total)
	{
		if (successful == total || total > 1)
		{
			StopAndReset();
		}
	}

	private void Build(UIPanelBuilder builder)
	{
		builder.Spacing = 8f;
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddButton("Run All", RunAll);
			uIPanelBuilder.AddButton("Reload", Reload);
			uIPanelBuilder.AddButton("Stop", StopAndReset);
		});
		builder.VScrollView(delegate(UIPanelBuilder value)
		{
			_testsScrollViewBuilder = value;
			foreach (ScriptTestRunner.TestSuite testSuite in _runner.TestSuites)
			{
				if (testSuite.Tests == null && !string.IsNullOrEmpty(testSuite.SyntaxErrorMessage))
				{
					value.AddLabel(testSuite.Name + " Syntax Error:");
					value.AddLabel(testSuite.SyntaxErrorMessage);
				}
				else
				{
					value.HStack(delegate(UIPanelBuilder uIPanelBuilder)
					{
						uIPanelBuilder.AddButton("<sprite name=\"Destination\">", delegate
						{
							RunSuite(testSuite);
						});
						uIPanelBuilder.AddLabel(testSuite.Name);
					}).Height(30f);
					value.VStack(delegate(UIPanelBuilder uIPanelBuilder)
					{
						foreach (ScriptTestRunner.Test test in testSuite.Tests)
						{
							uIPanelBuilder.VStack(delegate(UIPanelBuilder value2)
							{
								_testBuilders[test] = value2;
								value2.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
								{
									uIPanelBuilder2.AddButtonCompact("<sprite name=\"Destination\">", delegate
									{
										RunTest(test);
									});
									uIPanelBuilder2.AddLabel(test.Name);
									uIPanelBuilder2.Spacer();
									string text = test.Status switch
									{
										ScriptTestRunner.TestStatus.Unknown => "Unknown", 
										ScriptTestRunner.TestStatus.Pending => "Pending".ColorYellow(), 
										ScriptTestRunner.TestStatus.Passed => "PASS".ColorGreen(), 
										ScriptTestRunner.TestStatus.Failed => "FAIL".ColorRed(), 
										_ => throw new ArgumentOutOfRangeException(), 
									};
									ScriptTestRunner.TestStatus status = test.Status;
									if (status == ScriptTestRunner.TestStatus.Passed || status == ScriptTestRunner.TestStatus.Failed)
									{
										text = $"<size=70%>{test.DurationUnscaled:0.0}s / {test.DurationScaled:0.0}s</size> {text}";
									}
									uIPanelBuilder2.AddLabel(text);
								}).Height(30f);
								if (test.Status == ScriptTestRunner.TestStatus.Failed)
								{
									value2.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
									{
										uIPanelBuilder2.Spacer(32f);
										uIPanelBuilder2.AddLabel(test.FailureMessage);
									});
								}
							});
						}
					}).padding = new RectOffset(32, 0, 0, 0);
				}
			}
			value.AddExpandingVerticalSpacer();
		});
	}

	private void StopAndReset()
	{
		StopTests();
		ScriptTestRunner.ResetWorld();
	}

	private void Reload()
	{
		StopTests();
		_runner.LoadTests();
		UIPanelBuilder? testsScrollViewBuilder = _testsScrollViewBuilder;
		if (testsScrollViewBuilder.HasValue)
		{
			testsScrollViewBuilder.GetValueOrDefault().Rebuild();
		}
		else
		{
			_panel.Rebuild();
		}
	}

	private void RunAll()
	{
		StopTests();
		_coroutine = StartCoroutine(_runner.RunAllTests());
	}

	private void RunSuite(ScriptTestRunner.TestSuite testSuite)
	{
		StopTests();
		_coroutine = StartCoroutine(_runner.RunTests(testSuite.Tests));
	}

	private void RunTest(ScriptTestRunner.Test test)
	{
		StopTests();
		List<ScriptTestRunner.Test> tests = new List<ScriptTestRunner.Test> { test };
		_coroutine = StartCoroutine(_runner.RunTests(tests));
	}

	private void StopTests()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}
}
