using Model.Ops.Definition;
using UnityEngine;

namespace Model;

[CreateAssetMenu(fileName = "CarPrototypeLibrary", menuName = "Railroader/Car Prototype Library")]
public class CarPrototypeLibrary : ScriptableObject
{
	public Load[] opsLoads;

	public static CarPrototypeLibrary instance;

	public void AutoPopulate()
	{
	}

	public Load LoadForId(string loadId)
	{
		if (string.IsNullOrEmpty(loadId))
		{
			return null;
		}
		Load[] array = opsLoads;
		foreach (Load load in array)
		{
			if (load.id == loadId)
			{
				return load;
			}
		}
		return null;
	}
}
