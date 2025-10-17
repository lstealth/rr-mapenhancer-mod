using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter;
using Serilog;
using UnityEngine;

namespace Game.Scripting.Testing;

public class ScriptTestRunner
{
	public class TestSuite
	{
		public string Name { get; set; }

		public string ScriptPath { get; set; }

		public DateTime LastModifiedTime { get; set; }

		public ScriptManager Script { get; set; }

		public List<Test> Tests { get; set; }

		public string SyntaxErrorMessage { get; set; }

		public string SetupClosureName { get; set; }

		public string TeardownClosureName { get; set; }

		public TestSuite(string name, string scriptPath, DateTime lastModifiedTime, ScriptManager script, List<Test> tests, string syntaxErrorMessage = null)
		{
			Name = name;
			ScriptPath = scriptPath;
			LastModifiedTime = lastModifiedTime;
			Script = script;
			Tests = tests;
			SyntaxErrorMessage = syntaxErrorMessage;
		}
	}

	public class Test
	{
		public readonly string Name;

		public readonly string ClosureName;

		public readonly TestSuite Suite;

		public TestStatus Status;

		public string FailureMessage;

		public float DurationUnscaled { get; set; }

		public float DurationScaled { get; set; }

		public Test(string name, string closureName, TestSuite suite)
		{
			Name = name;
			ClosureName = closureName;
			Suite = suite;
			Status = TestStatus.Unknown;
		}
	}

	public enum TestStatus
	{
		Unknown,
		Pending,
		Passed,
		Failed
	}

	private readonly string _testPath;

	private readonly string[] _modulePaths;

	private readonly MonoBehaviour _hostComponent;

	private List<TestSuite> _testSuites = new List<TestSuite>();

	public IReadOnlyList<TestSuite> TestSuites => _testSuites;

	public event Action<Test> OnTestStatusChange;

	public event Action<int, int> OnRunComplete;

	public ScriptTestRunner(string testPath, MonoBehaviour hostComponent)
	{
		_testPath = testPath;
		_hostComponent = hostComponent;
		_modulePaths = new string[2]
		{
			Path.Combine(_testPath, "?.lua"),
			ScriptManager.BuiltInModulesPath
		};
	}

	public void LoadTests()
	{
		if (!Directory.Exists(_testPath))
		{
			Log.Warning("Test path does not exist: {path}", _testPath);
			return;
		}
		string[] files = Directory.GetFiles(_testPath, "test_*.lua");
		List<TestSuite> list = new List<TestSuite>();
		string[] array = files;
		foreach (string luaFile in array)
		{
			if (TryLoadTestSuite(luaFile, out var suite))
			{
				list.Add(suite);
			}
		}
		_testSuites = list;
	}

	private bool TryLoadTestSuite(string luaFile, out TestSuite suite)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(luaFile);
		string text = fileNameWithoutExtension.Substring(5, fileNameWithoutExtension.Length - 5);
		DateTime lastWriteTime = File.GetLastWriteTime(luaFile);
		suite = null;
		try
		{
			string source = File.ReadAllText(luaFile);
			ScriptManager scriptManager = new ScriptManager(null, _hostComponent, _modulePaths);
			scriptManager.Load(source, text);
			List<string> list = scriptManager.GetGlobalClosureNames().ToList();
			IEnumerable<string> enumerable = list.Where((string n) => n.StartsWith("test_"));
			List<Test> list2 = new List<Test>();
			suite = new TestSuite(text, luaFile, lastWriteTime, scriptManager, list2);
			foreach (string item2 in enumerable)
			{
				fileNameWithoutExtension = item2;
				Test item = new Test(fileNameWithoutExtension.Substring(5, fileNameWithoutExtension.Length - 5), item2, suite);
				list2.Add(item);
			}
			if (list.Contains("setup"))
			{
				suite.SetupClosureName = "setup";
			}
			if (list.Contains("teardown"))
			{
				suite.TeardownClosureName = "teardown";
			}
		}
		catch (SyntaxErrorException ex)
		{
			suite = new TestSuite(text, luaFile, lastWriteTime, null, null, ex.DecoratedMessage);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to load test suite: {suite}", luaFile);
			return false;
		}
		return true;
	}

	public IEnumerator RunAllTests()
	{
		List<Test> tests = _testSuites.SelectMany((TestSuite suite) => suite.Tests).ToList();
		yield return RunTests(tests);
	}

	public IEnumerator RunTests(List<Test> tests)
	{
		foreach (Test test in tests)
		{
			test.Status = TestStatus.Unknown;
			this.OnTestStatusChange?.Invoke(test);
		}
		foreach (Test test2 in tests)
		{
			yield return RunTest(test2);
		}
		this.OnRunComplete?.Invoke(tests.Count((Test t) => t.Status == TestStatus.Passed), tests.Count);
	}

	private IEnumerator RunTest(Test test)
	{
		TestSuite suite = test.Suite;
		test.Status = TestStatus.Pending;
		test.FailureMessage = null;
		test.DurationScaled = 0f;
		test.DurationUnscaled = 0f;
		this.OnTestStatusChange?.Invoke(test);
		float tStartScaled = Time.time;
		float tStartUnscaled = Time.unscaledTime;
		ScriptManager script = suite.Script;
		script.ClearLastRunError();
		ScriptWorld world = ScriptWorld.Shared;
		if (suite.SetupClosureName != null)
		{
			yield return script.RunFromCoroutine(suite.SetupClosureName, world);
		}
		yield return script.RunFromCoroutine(test.ClosureName, world);
		if (suite.TeardownClosureName != null)
		{
			yield return script.RunFromCoroutine(suite.TeardownClosureName, world);
		}
		ScriptManager.ErrorInfo? lastRunError = script.LastRunError;
		if (lastRunError.HasValue)
		{
			ScriptManager.ErrorInfo info = lastRunError.GetValueOrDefault();
			test.Status = TestStatus.Failed;
			test.FailureMessage = info.DecoratedMessage;
		}
		else
		{
			test.Status = TestStatus.Passed;
		}
		float time = Time.time;
		float unscaledTime = Time.unscaledTime;
		test.DurationUnscaled = unscaledTime - tStartUnscaled;
		test.DurationScaled = time - tStartScaled;
		Log.Information("Test {name} completed: {status}, in {unscaled}s ({scaled}s game) {message}", test.Name, test.Status, test.DurationUnscaled, test.DurationScaled, test.FailureMessage);
		this.OnTestStatusChange?.Invoke(test);
	}

	public string GetReport()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = _testSuites.Sum((TestSuite suite) => suite.Tests.Count);
		int num2 = _testSuites.Sum((TestSuite suite) => suite.Tests.Count((Test test) => test.Status == TestStatus.Passed));
		int num3 = _testSuites.Sum((TestSuite suite) => suite.Tests.Count((Test test) => test.Status == TestStatus.Failed));
		stringBuilder.AppendLine($"Test Report: {num2} passed, {num3} failed (of {num})");
		foreach (TestSuite testSuite in _testSuites)
		{
			foreach (Test test in testSuite.Tests)
			{
				TestStatus status = test.Status;
				string name = test.Name;
				string failureMessage = test.FailureMessage;
				string name2 = testSuite.Name;
				string text = name.Substring(5);
				string text2 = ((status == TestStatus.Passed) ? "PASSED" : "FAILED");
				stringBuilder.AppendLine("TEST: " + name2 + "." + text + " (" + text2 + ")");
				if (status == TestStatus.Failed)
				{
					string text3 = (failureMessage ?? "(empty failure message)").Replace("\n", "\n    ");
					stringBuilder.AppendLine("   " + text3);
				}
			}
		}
		return stringBuilder.ToString();
	}

	public static void ResetWorld()
	{
		ScriptWorld.reset();
	}

	public bool LoadTestsIfModified()
	{
		for (int i = 0; i < _testSuites.Count; i++)
		{
			TestSuite testSuite = _testSuites[i];
			if (testSuite.LastModifiedTime < File.GetLastWriteTime(testSuite.ScriptPath))
			{
				Log.Information("Reloading test suite: {suite}", testSuite.Name);
				if (TryLoadTestSuite(testSuite.ScriptPath, out var suite))
				{
					_testSuites[i] = suite;
					return true;
				}
			}
		}
		return false;
	}
}
