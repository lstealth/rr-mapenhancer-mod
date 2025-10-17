using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Model.Definition;
using Model.Definition.Data;
using UI.CarEditor.Cells;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CarEditor;

public class ObjectEditorSecondaryPanel : MonoBehaviour
{
	public struct ModelHierarchyEntry
	{
		public string Text { get; set; }

		public string[] Path { get; set; }
	}

	private enum Change
	{
		Value,
		Structure
	}

	[SerializeField]
	private RectTransform scrollContent;

	[Header("Cell Prototypes")]
	[SerializeField]
	private EditorHeaderCell headerCellPrototype;

	[SerializeField]
	private EditorTextFieldCell textFieldCellPrototype;

	[SerializeField]
	private EditorFloatCell floatCellPrototype;

	[SerializeField]
	private EditorVector3Cell vector3CellPrototype;

	[SerializeField]
	private EditorArrayCell arrayCellPrototype;

	[SerializeField]
	private EditorNestedCell nestedCellPrototype;

	[SerializeField]
	private EditorDropdownCell dropdownCellPrototype;

	[SerializeField]
	private EditorCheckboxCell checkboxCellPrototype;

	private object _object;

	private Action _requestRefresh;

	private ModelMapNames _mapNames;

	private List<ModelHierarchyEntry> _modelHierarchy = new List<ModelHierarchyEntry>();

	private EditorHeaderCell _titleCell;

	private List<UnityEngine.Component> CellPrototypes => new List<UnityEngine.Component> { headerCellPrototype, textFieldCellPrototype, floatCellPrototype, vector3CellPrototype, arrayCellPrototype, nestedCellPrototype, dropdownCellPrototype, checkboxCellPrototype };

	private void Awake()
	{
		foreach (UnityEngine.Component cellPrototype in CellPrototypes)
		{
			cellPrototype.gameObject.SetActive(value: false);
		}
	}

	public void Configure(object obj, Action requestRefresh)
	{
		_object = obj;
		_requestRefresh = requestRefresh;
		Rebuild();
	}

	public void Rebuild()
	{
		scrollContent.DestroyChildrenExcept(CellPrototypes);
		if (_object != null)
		{
			Debug.Log("Rebuild");
			_titleCell = CreateHeaderCell(HeaderTitleForObject(_object), scrollContent);
			AddEditorCells(_object, scrollContent);
		}
	}

	private void DidChange(Change change)
	{
		_requestRefresh?.Invoke();
		_titleCell?.Configure(HeaderTitleForObject(_object));
		switch (change)
		{
		case Change.Structure:
			Rebuild();
			break;
		default:
			throw new ArgumentOutOfRangeException("change", change, null);
		case Change.Value:
			break;
		}
	}

	private string HeaderTitleForObject(object obj)
	{
		if (obj is Model.Definition.Component component)
		{
			if (!string.IsNullOrEmpty(component.Name))
			{
				return component.Name;
			}
			return component.GetType().ToString();
		}
		return obj.GetType().ToString();
	}

	private void AddEditorCells(object obj, RectTransform parent)
	{
		foreach (PropertyInfo item in obj.GetType().GetProperties().ToList())
		{
			try
			{
				CreatePropertyCell(obj, item, parent);
			}
			catch (Exception exception)
			{
				Debug.LogError("Error creating property cell for " + item.Name + ":");
				Debug.LogException(exception);
			}
		}
	}

	private void CreatePropertyCell(object obj, PropertyInfo prop, RectTransform parent)
	{
		object value = prop.GetValue(obj);
		Type propertyType = prop.PropertyType;
		DefinitionPropertyAttribute definitionPropertyAttribute = new DefinitionPropertyAttribute();
		DefinitionPropertyAttribute obj2 = prop.GetCustomAttributes<DefinitionPropertyAttribute>().FirstOrDefault() ?? definitionPropertyAttribute;
		bool editable = obj2.Editable && prop.CanWrite;
		if (obj2.Hidden)
		{
			return;
		}
		if (propertyType == typeof(string))
		{
			CreateTextFieldCell(parent).Configure(prop.Name, (string)value, editable, delegate(string newValue)
			{
				prop.SetValue(obj, newValue);
				DidChange(Change.Value);
			});
		}
		else if (propertyType == typeof(int))
		{
			CreateTextFieldCell(parent).Configure(prop.Name, (int)value, editable, delegate(int newValue)
			{
				prop.SetValue(obj, newValue);
				DidChange(Change.Value);
			});
		}
		else if (propertyType == typeof(float))
		{
			CreateFloatCell(parent).Configure(prop.Name, (float)value, delegate(float newValue)
			{
				prop.SetValue(obj, newValue);
				DidChange(Change.Value);
			});
		}
		else if (propertyType == typeof(bool))
		{
			CreateCheckboxCell(parent).Configure(prop.Name, (bool)value, delegate(bool newValue)
			{
				prop.SetValue(obj, newValue);
				DidChange(Change.Value);
			});
		}
		else if (propertyType == typeof(Vector3))
		{
			CreateVector3Cell(parent).Configure(prop.Name, (Vector3)value, delegate(Vector3 newValue)
			{
				prop.SetValue(obj, newValue);
				DidChange(Change.Value);
			});
		}
		else if (propertyType == typeof(Vector2))
		{
			CreateVector3Cell(parent).Configure(prop.Name, (Vector2)value, delegate(Vector2 newValue)
			{
				prop.SetValue(obj, newValue);
				DidChange(Change.Value);
			});
		}
		else if (propertyType.IsArray)
		{
			Debug.LogError($"Unsupported type for property {prop.Name}: {prop.PropertyType}");
		}
		else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
		{
			CreateListCell(obj, prop, parent);
		}
		else if (propertyType == typeof(TransformReference))
		{
			CreateTransformReferenceCell(obj, prop, parent);
		}
		else if (propertyType == typeof(AnimationReference))
		{
			CreateAnimationReferenceCell(obj, prop, parent);
		}
		else if (propertyType == typeof(MaterialReference))
		{
			CreateMaterialReferenceCell(obj, prop, parent);
		}
		else if (propertyType == typeof(PositionRotationScale))
		{
			CreatePositionRotationScaleCell(obj, prop, parent);
		}
		else if (propertyType.IsEnum)
		{
			CreateEnumCell(obj, prop, parent);
		}
		else
		{
			EditorNestedCell editorNestedCell = CreateNestedCell(parent);
			editorNestedCell.Configure(prop.Name);
			AddEditorCells(value, editorNestedCell.elementCellsRect);
		}
	}

	private void CreateTransformReferenceCell(object obj, PropertyInfo prop, RectTransform parent)
	{
		TransformReference tref = (TransformReference)prop.GetValue(obj);
		CreateTransformReferenceCell(prop.Name, tref, parent, delegate(TransformReference newTref)
		{
			prop.SetValue(obj, newTref);
			DidChange(Change.Value);
		});
	}

	private void CreateTransformReferenceCell(string label, TransformReference tref, RectTransform parent, Action<TransformReference> onChange)
	{
		if (_modelHierarchy == null)
		{
			CreateTextFieldCell(parent).Configure(label, tref?.ToString(), editable: false, delegate
			{
			});
			return;
		}
		int num = ((tref != null && tref.Path != null) ? _modelHierarchy.FindIndex((ModelHierarchyEntry entry) => entry.Path.SequenceEqual(tref.Path)) : (-1));
		List<string> list = _modelHierarchy.Select((ModelHierarchyEntry entry) => entry.Text).ToList();
		list.Insert(0, "<None>");
		num++;
		CreateDropdownCell(parent).Configure(label, list, num, delegate(int index)
		{
			if (index == 0)
			{
				tref = null;
			}
			else
			{
				if (tref == null)
				{
					tref = new TransformReference(new string[0]);
				}
				tref.Path = _modelHierarchy[index - 1].Path;
			}
			onChange(tref);
			DidChange(Change.Value);
		});
	}

	private void CreateAnimationReferenceCell(object obj, PropertyInfo prop, RectTransform parent)
	{
		AnimationReference aref = (AnimationReference)prop.GetValue(obj);
		CreateAnimationReferenceCell(prop.Name, aref, parent, delegate(AnimationReference newAref)
		{
			prop.SetValue(obj, newAref);
			DidChange(Change.Value);
		});
	}

	private void CreateAnimationReferenceCell(string label, AnimationReference aref, RectTransform parent, Action<AnimationReference> onChange)
	{
		EditorDropdownCell editorDropdownCell = CreateDropdownCell(parent);
		List<string> animationNames = _mapNames.Animation;
		if (animationNames == null)
		{
			editorDropdownCell.Configure(label, new List<string> { "Loading..." }, 0, delegate
			{
			});
			return;
		}
		int num = ((aref == null) ? (-1) : animationNames.IndexOf(aref.ClipName));
		List<string> list = new List<string>(animationNames);
		list.Insert(0, "<None>");
		num++;
		editorDropdownCell.Configure(label, list, num, delegate(int index)
		{
			if (index == 0)
			{
				aref = null;
			}
			else
			{
				if (aref == null)
				{
					aref = new AnimationReference();
				}
				aref.ClipName = animationNames[index - 1];
			}
			onChange(aref);
		});
	}

	private void CreateMaterialReferenceCell(object obj, PropertyInfo prop, RectTransform parent)
	{
		MaterialReference aref = (MaterialReference)prop.GetValue(obj);
		CreateMaterialReferenceCell(prop.Name, aref, parent, delegate(MaterialReference newAref)
		{
			prop.SetValue(obj, newAref);
			DidChange(Change.Value);
		});
	}

	private void CreateMaterialReferenceCell(string label, MaterialReference aref, RectTransform parent, Action<MaterialReference> onChange)
	{
		EditorDropdownCell editorDropdownCell = CreateDropdownCell(parent);
		List<string> materialNames = _mapNames.Material;
		if (materialNames == null)
		{
			editorDropdownCell.Configure(label, new List<string> { "Loading..." }, 0, delegate
			{
			});
			return;
		}
		int num = ((aref == null) ? (-1) : materialNames.IndexOf(aref.MaterialName));
		List<string> list = new List<string>(materialNames);
		list.Insert(0, "<None>");
		num++;
		editorDropdownCell.Configure(label, list, num, delegate(int index)
		{
			if (index == 0)
			{
				aref = null;
			}
			else
			{
				if (aref == null)
				{
					aref = new MaterialReference();
				}
				aref.MaterialName = materialNames[index - 1];
			}
			onChange(aref);
		});
	}

	private void CreateListCell(object obj, PropertyInfo prop, RectTransform parent)
	{
		object propValue = prop.GetValue(obj);
		Type propertyType = prop.PropertyType;
		IList list = ((IList)propValue) ?? new List<object>();
		Type elementType = propertyType.GetGenericArguments()[0];
		EditorArrayCell cell = CreateArrayCell(parent);
		cell.Configure(prop.Name, list.Count, delegate
		{
			object value2 = CreateDefaultInstance(elementType);
			if (propValue == null)
			{
				Type type = typeof(List<>).MakeGenericType(elementType);
				list = (IList)Activator.CreateInstance(type);
				prop.SetValue(obj, list);
			}
			list.Add(value2);
			DidChange(Change.Structure);
		}, delegate
		{
			list.RemoveAt(list.Count - 1);
			DidChange(Change.Structure);
		});
		if (elementType == typeof(string))
		{
			List<string> stringList = list.Cast<string>().ToList();
			for (int num = 0; num < stringList.Count; num++)
			{
				string value = stringList[num];
				EditorTextFieldCell editorTextFieldCell = CreateTextFieldCell(cell.elementCellsRect);
				int strIndex = num;
				editorTextFieldCell.Configure($"[{num}]", value, editable: true, delegate(string newValue)
				{
					stringList[strIndex] = newValue;
					prop.SetValue(obj, stringList);
					DidChange(Change.Value);
				});
			}
			return;
		}
		if (elementType == typeof(PositionRotationScale))
		{
			List<PositionRotationScale> typedList = list.Cast<PositionRotationScale>().ToList();
			for (int num2 = 0; num2 < typedList.Count; num2++)
			{
				int typedIndex = num2;
				CreatePositionRotationScaleCell($"[{num2}]", typedList[num2], cell.elementCellsRect, delegate(PositionRotationScale newPrs)
				{
					typedList[typedIndex] = newPrs;
					prop.SetValue(obj, typedList);
					DidChange(Change.Value);
				});
			}
			return;
		}
		if (elementType == typeof(AnimationReference))
		{
			List<AnimationReference> typedList2 = list.Cast<AnimationReference>().ToList();
			for (int num3 = 0; num3 < typedList2.Count; num3++)
			{
				int typedIndex2 = num3;
				CreateAnimationReferenceCell($"[{num3}]", typedList2[num3], cell.elementCellsRect, delegate(AnimationReference newAref)
				{
					typedList2[typedIndex2] = newAref;
					prop.SetValue(obj, typedList2);
					DidChange(Change.Value);
				});
			}
			return;
		}
		if (elementType == typeof(TransformReference))
		{
			List<TransformReference> typedList3 = list.Cast<TransformReference>().ToList();
			for (int num4 = 0; num4 < typedList3.Count; num4++)
			{
				int typedIndex3 = num4;
				CreateTransformReferenceCell($"[{num4}]", typedList3[num4], cell.elementCellsRect, delegate(TransformReference newAref)
				{
					typedList3[typedIndex3] = newAref;
					prop.SetValue(obj, typedList3);
					DidChange(Change.Value);
				});
			}
			return;
		}
		if (elementType.IsClass)
		{
			foreach (object item in list)
			{
				AddEditorCells(item, cell.elementCellsRect);
				IList list2 = list;
				if (item != list2[list2.Count - 1])
				{
					AddSpacer();
				}
			}
			return;
		}
		Debug.LogError($"Unsupported type for list element of {prop.Name}: {prop.PropertyType} with element type {elementType}");
		void AddSpacer()
		{
			GameObject obj2 = new GameObject("Spacer");
			obj2.transform.SetParent(cell.elementCellsRect, worldPositionStays: false);
			obj2.AddComponent<LayoutElement>().preferredHeight = 8f;
		}
	}

	private static object CreateDefaultInstance(Type elementType)
	{
		if (elementType == typeof(PositionRotationScale))
		{
			return PositionRotationScale.Default;
		}
		if (!(elementType == typeof(string)))
		{
			return Activator.CreateInstance(elementType);
		}
		return "";
	}

	private void CreateEnumCell(object obj, PropertyInfo prop, RectTransform parent)
	{
		object value = prop.GetValue(obj);
		Type propertyType = prop.PropertyType;
		List<int> dropdownValueInts = new List<int>();
		List<string> list = new List<string>();
		int selected = -1;
		int num = 0;
		foreach (object enumValue in propertyType.GetEnumValues())
		{
			list.Add(enumValue.ToString());
			dropdownValueInts.Add((int)enumValue);
			if ((int)enumValue == (int)value)
			{
				selected = num;
			}
			num++;
		}
		CreateDropdownCell(parent).Configure(prop.Name, list, selected, delegate(int newSelectionIndex)
		{
			prop.SetValue(obj, (newSelectionIndex >= 0) ? dropdownValueInts[newSelectionIndex] : 0);
			DidChange(Change.Value);
		});
	}

	private void CreatePositionRotationScaleCell(string label, PositionRotationScale prs, RectTransform parent, Action<PositionRotationScale> onChange)
	{
		CreateVector3Cell(parent).Configure(label, prs.Position, delegate(Vector3 newPos)
		{
			prs.Position = newPos;
			onChange(prs);
		});
		CreateVector3Cell(parent).Configure(null, prs.Rotation.eulerAngles, delegate(Vector3 newEuler)
		{
			prs.Rotation = Quaternion.Euler(newEuler);
			onChange(prs);
		});
		CreateVector3Cell(parent).Configure(null, prs.Scale, delegate(Vector3 newScale)
		{
			prs.Scale = newScale;
			onChange(prs);
		});
	}

	private void CreatePositionRotationScaleCell(object obj, PropertyInfo prop, RectTransform parent)
	{
		CreatePositionRotationScaleCell(prop.Name, (PositionRotationScale)prop.GetValue(obj), parent, delegate(PositionRotationScale newValue)
		{
			prop.SetValue(obj, newValue);
			DidChange(Change.Value);
		});
	}

	private EditorTextFieldCell CreateTextFieldCell(RectTransform parent)
	{
		EditorTextFieldCell editorTextFieldCell = UnityEngine.Object.Instantiate(textFieldCellPrototype, parent);
		editorTextFieldCell.gameObject.SetActive(value: true);
		return editorTextFieldCell;
	}

	private EditorHeaderCell CreateHeaderCell(string text, RectTransform parent)
	{
		EditorHeaderCell editorHeaderCell = UnityEngine.Object.Instantiate(headerCellPrototype, parent);
		editorHeaderCell.gameObject.SetActive(value: true);
		editorHeaderCell.Configure(text);
		return editorHeaderCell;
	}

	private EditorFloatCell CreateFloatCell(RectTransform parent)
	{
		EditorFloatCell editorFloatCell = UnityEngine.Object.Instantiate(floatCellPrototype, parent);
		editorFloatCell.gameObject.SetActive(value: true);
		return editorFloatCell;
	}

	private EditorCheckboxCell CreateCheckboxCell(RectTransform parent)
	{
		EditorCheckboxCell editorCheckboxCell = UnityEngine.Object.Instantiate(checkboxCellPrototype, parent);
		editorCheckboxCell.gameObject.SetActive(value: true);
		return editorCheckboxCell;
	}

	private EditorVector3Cell CreateVector3Cell(RectTransform parent)
	{
		EditorVector3Cell editorVector3Cell = UnityEngine.Object.Instantiate(vector3CellPrototype, parent);
		editorVector3Cell.gameObject.SetActive(value: true);
		return editorVector3Cell;
	}

	private EditorArrayCell CreateArrayCell(RectTransform parent)
	{
		EditorArrayCell editorArrayCell = UnityEngine.Object.Instantiate(arrayCellPrototype, parent);
		editorArrayCell.gameObject.SetActive(value: true);
		return editorArrayCell;
	}

	private EditorNestedCell CreateNestedCell(RectTransform parent)
	{
		EditorNestedCell editorNestedCell = UnityEngine.Object.Instantiate(nestedCellPrototype, parent);
		editorNestedCell.gameObject.SetActive(value: true);
		return editorNestedCell;
	}

	private EditorDropdownCell CreateDropdownCell(RectTransform parent)
	{
		EditorDropdownCell editorDropdownCell = UnityEngine.Object.Instantiate(dropdownCellPrototype, parent);
		editorDropdownCell.gameObject.SetActive(value: true);
		return editorDropdownCell;
	}

	public void SetModelMapNames(ModelMapNames mapNames)
	{
		_mapNames.Animation = ((mapNames.Animation == null) ? null : new List<string>(mapNames.Animation));
		_mapNames.Material = ((mapNames.Material == null) ? null : new List<string>(mapNames.Material));
		Rebuild();
	}

	public void SetModelHierarchyFromTransform(Transform root)
	{
		if (root == null)
		{
			_modelHierarchy = null;
		}
		else
		{
			_modelHierarchy = new List<ModelHierarchyEntry>();
			BuildHierarchy(root, new List<string>());
		}
		Rebuild();
		void BuildHierarchy(Transform cursor, List<string> parents)
		{
			string text = new string('-', parents.Count);
			for (int i = 0; i < cursor.childCount; i++)
			{
				Transform child = cursor.GetChild(i);
				List<string> list = new List<string>(parents) { child.name };
				_modelHierarchy.Add(new ModelHierarchyEntry
				{
					Text = text + " " + child.name,
					Path = list.ToArray()
				});
				BuildHierarchy(child, list);
			}
		}
	}
}
