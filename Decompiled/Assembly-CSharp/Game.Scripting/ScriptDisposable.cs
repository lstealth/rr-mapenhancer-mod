using System;

namespace Game.Scripting;

public class ScriptDisposable
{
	private IDisposable _disposable;

	public ScriptDisposable(IDisposable disposable)
	{
		_disposable = disposable;
	}

	public void dispose()
	{
		_disposable?.Dispose();
		_disposable = null;
	}
}
