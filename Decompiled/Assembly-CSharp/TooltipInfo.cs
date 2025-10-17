public struct TooltipInfo
{
	public string Title;

	public string Text;

	public static readonly TooltipInfo Empty;

	public bool IsEmpty
	{
		get
		{
			if (string.IsNullOrEmpty(Title))
			{
				return string.IsNullOrEmpty(Text);
			}
			return false;
		}
	}

	public TooltipInfo(string title, string text)
	{
		Title = title;
		Text = text;
	}

	public override string ToString()
	{
		if (!IsEmpty)
		{
			return "TooltipInfo(\"" + Title + "\", \"" + Text + "\")";
		}
		return "TooltipInfo()";
	}

	public bool Equals(TooltipInfo other)
	{
		if (Title == other.Title)
		{
			return Text == other.Text;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is TooltipInfo other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((Title != null) ? Title.GetHashCode() : 0) * 397) ^ ((Text != null) ? Text.GetHashCode() : 0);
	}
}
