using Model.Ops;

namespace UI;

public struct DropdownLocationPickerRowData
{
	public string Title;

	public string Subtitle;

	public DropdownLocationPickerRowData(string title, string subtitle)
	{
		Title = title;
		Subtitle = subtitle;
	}

	public static DropdownLocationPickerRowData From(IndustryComponent industryComponent, Area area)
	{
		if (industryComponent == null)
		{
			return new DropdownLocationPickerRowData("None", "Clear Selection");
		}
		return new DropdownLocationPickerRowData(TitleForComponent(industryComponent), area.name);
		static string TitleForComponent(IndustryComponent ic)
		{
			if (ic is InterchangedIndustryLoader interchangedIndustryLoader)
			{
				return "Buy " + interchangedIndustryLoader.load.description + " via " + interchangedIndustryLoader.DisplayName;
			}
			if (ic is IndustryLoaderBase industryLoaderBase)
			{
				return "Load " + industryLoaderBase.load.description + " at " + industryLoaderBase.DisplayName;
			}
			if (ic is IndustryUnloader industryUnloader)
			{
				return "Unload " + industryUnloader.load.description + " at " + industryUnloader.DisplayName;
			}
			return ic.DisplayName;
		}
	}
}
