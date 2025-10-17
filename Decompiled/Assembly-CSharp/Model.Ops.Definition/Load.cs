using System;
using Model.Definition.Data;
using UnityEngine;

namespace Model.Ops.Definition;

[CreateAssetMenu(fileName = "Load", menuName = "Train Game/Ops/Car Load", order = 8)]
public class Load : ScriptableObject
{
	[Tooltip("Display name.")]
	public string description;

	public LoadUnits units;

	[Tooltip("Density in pounds per cubic foot.")]
	public float density = 62.4f;

	[Tooltip("For discrete loads (logs, tractors, etc.), weight in pounds per item.")]
	public float unitWeightInPounds;

	[Tooltip("True if this load can be source from off-railroad. Raw materials like ore and logs are usually not sourced off-railroad.")]
	public bool importable = true;

	[Tooltip("Amount paid per quantity upon waybill completion. Used with non-importable loads.")]
	public float payPerQuantity;

	[Tooltip("For orderable loads, such as coal, the amount per unit to charge.")]
	public float costPerUnit;

	public const float ZeroDeltaThreshold = 1E-07f;

	public string id => base.name;

	public float NominalQuantityPerCarLoad => units switch
	{
		LoadUnits.Pounds => 100000, 
		LoadUnits.Gallons => 8000, 
		LoadUnits.Quantity => 3, 
		_ => throw new ArgumentOutOfRangeException(), 
	};

	public float ZeroThreshold => units switch
	{
		LoadUnits.Pounds => 0.1f, 
		LoadUnits.Gallons => 0.01f, 
		LoadUnits.Quantity => 0.001f, 
		_ => throw new ArgumentOutOfRangeException(), 
	};

	public float Pounds(float quantity)
	{
		return units switch
		{
			LoadUnits.Pounds => quantity, 
			LoadUnits.Gallons => quantity * 0.133681f * density, 
			LoadUnits.Quantity => quantity * unitWeightInPounds, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public string QuantityString(float quantity)
	{
		return units.QuantityString(quantity) + " " + description;
	}
}
