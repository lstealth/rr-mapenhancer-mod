using Map.Runtime;

namespace Game.Scripting.Testing;

public class ScriptTestsController : GameBehaviour
{
	private ScriptTestsWindow _window;

	private ScriptTestRunner _runner;

	protected override void OnEnableWithProperties()
	{
		base.OnEnableWithProperties();
		MapManager.Instance.ForceDisableTrees = true;
		string testPath = "TestScripts";
		_runner = new ScriptTestRunner(testPath, this);
		_runner.LoadTests();
		_window = ScriptTestsWindow.Shared;
		_window.Show(_runner);
	}
}
