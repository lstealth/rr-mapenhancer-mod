using Model.Ops;

namespace Game.Scripting;

public class ScriptWaybill
{
	private readonly Waybill _waybill;

	public bool completed => _waybill.Completed;

	internal ScriptWaybill(Waybill waybill)
	{
		_waybill = waybill;
	}
}
