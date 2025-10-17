using Game.State;
using UnityEngine;

namespace Game;

public class GameBehaviour : MonoBehaviour
{
	protected virtual int EnablePriority => 0;

	protected virtual void OnEnable()
	{
		RestoreNotifier.Shared.RegisterForRestore(EnablePriority, this, OnEnableWithProperties);
	}

	protected virtual void OnDisable()
	{
		RestoreNotifier.Shared?.Unregister(this);
	}

	protected virtual void OnEnableWithProperties()
	{
	}
}
