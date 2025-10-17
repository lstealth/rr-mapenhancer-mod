using System;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UI.InputRebind;

[Serializable]
public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
{
}
