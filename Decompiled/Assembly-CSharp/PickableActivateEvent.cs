public struct PickableActivateEvent
{
	public bool IsControlDown { get; set; }

	public bool IsShiftDown { get; set; }

	public PickableActivation Activation { get; set; }
}
