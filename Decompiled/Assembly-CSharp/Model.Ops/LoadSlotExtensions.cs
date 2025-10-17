using Model.Definition.Data;
using Model.Ops.Definition;

namespace Model.Ops;

public static class LoadSlotExtensions
{
	public static bool LoadRequirementsMatch(this LoadSlot sd, Load load)
	{
		if (string.IsNullOrEmpty(sd.RequiredLoadIdentifier))
		{
			return true;
		}
		if (load == null)
		{
			return false;
		}
		return sd.RequiredLoadIdentifier == load.id;
	}

	public static bool LoadRequirementsMatch(this LoadSlot sd, string loadId)
	{
		if (string.IsNullOrEmpty(sd.RequiredLoadIdentifier))
		{
			return true;
		}
		return sd.RequiredLoadIdentifier == loadId;
	}
}
