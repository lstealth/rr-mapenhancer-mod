using System.Linq;
using System.Text;
using Game.Progression;

namespace UI.Console.Commands;

[ConsoleCommand("/progression", null)]
public class ProgressionCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		Progression shared = Progression.Shared;
		if (shared == null)
		{
			return "No current progression.";
		}
		if (comps.Length < 2)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Available sections:");
			Section[] sections = shared.Sections;
			foreach (Section section in sections)
			{
				if (section.Available)
				{
					stringBuilder.AppendLine("  " + section.identifier + " - " + section.displayName);
				}
			}
			stringBuilder.Append("/progression advance|revert <id>");
			return stringBuilder.ToString();
		}
		switch (comps[1])
		{
		case "adv":
		case "advance":
		{
			if (comps.Length < 3)
			{
				return "/progression advance|revert <id>";
			}
			string id2 = comps[2];
			Section section3 = shared.Sections.FirstOrDefault((Section s) => s.identifier == id2);
			shared.Advance(section3);
			return null;
		}
		case "rev":
		case "revert":
		{
			if (comps.Length < 3)
			{
				return "/progression advance|revert <id>";
			}
			string id = comps[2];
			Section section2 = shared.Sections.FirstOrDefault((Section s) => s.identifier == id);
			shared.Revert(section2);
			return null;
		}
		default:
			return "/progression advance|revert <id>";
		}
	}
}
