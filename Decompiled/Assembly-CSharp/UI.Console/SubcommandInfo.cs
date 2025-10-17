using System.Reflection;
using System.Text;

namespace UI.Console;

internal readonly struct SubcommandInfo
{
	public string Name { get; }

	public string Description { get; }

	public MethodInfo Method { get; }

	public ParameterInfo[] Parameters { get; }

	public SubcommandInfo(string name, string description, MethodInfo method)
	{
		Name = name.ToLower();
		Description = description;
		Method = method;
		Parameters = method.GetParameters();
	}

	public string GetUsage(string command)
	{
		StringBuilder stringBuilder = new StringBuilder("/" + command + " " + Name);
		ParameterInfo[] parameters = Parameters;
		foreach (ParameterInfo parameterInfo in parameters)
		{
			stringBuilder.Append(" <" + parameterInfo.Name + ">");
		}
		if (!string.IsNullOrEmpty(Description))
		{
			stringBuilder.AppendLine();
			stringBuilder.Append("    " + Description);
		}
		return stringBuilder.ToString();
	}
}
