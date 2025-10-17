using System;
using UnityEngine.Events;

namespace UI.InputRebind;

[Serializable]
public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
{
}
