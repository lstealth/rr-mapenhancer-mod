using System.Collections.Generic;
using System.Linq;
using System.Text;
using KeyValue.Runtime;

namespace Game.Reputation;

public struct ReputationReport
{
	public struct Component
	{
		private const string KeyRatio = "ratio";

		private const string KeyCategory = "category";

		private const string KeyText = "text";

		private const string KeyScore = "score";

		public float Ratio { get; set; }

		public string Category { get; set; }

		public string Text { get; set; }

		public float Score { get; set; }

		public Component(float ratio, string category, string text, float score)
		{
			Ratio = ratio;
			Category = category;
			Text = text;
			Score = score;
		}

		public static Component FromValue(Value value)
		{
			return new Component(value["ratio"].FloatValue, value["category"].StringValue, value["text"].StringValue, value["score"].FloatValue);
		}

		public Value ToValue()
		{
			return Value.Dictionary(new Dictionary<string, Value>
			{
				{
					"ratio",
					Value.Float(Ratio)
				},
				{
					"category",
					Value.String(Category)
				},
				{
					"text",
					Value.String(Text)
				},
				{
					"score",
					Value.Float(Score)
				}
			});
		}
	}

	private const string KeyComponents = "components";

	public List<Component> Components;

	public ReputationReport(List<Component> components)
	{
		Components = components;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Component component in Components)
		{
			stringBuilder.AppendLine($"{component.Category}: score = {component.Score:P1}, ratio = {component.Ratio:P1}, {component.Text}");
		}
		return stringBuilder.ToString();
	}

	public static ReputationReport FromValue(Value value)
	{
		return new ReputationReport(value["components"].ArrayValue.Select(Component.FromValue).ToList());
	}

	public Value ToValue()
	{
		return Value.Dictionary(new Dictionary<string, Value> { 
		{
			"components",
			Value.Array(Components.Select((Component c) => c.ToValue()).ToList())
		} });
	}

	public void AddComponent(Component component)
	{
		if (Components == null)
		{
			Components = new List<Component>();
		}
		Components.Add(component);
	}

	public float CalculateOverallReputation()
	{
		float num = 0f;
		float num2 = 0f;
		foreach (Component component in Components)
		{
			num2 += component.Ratio;
			num += component.Ratio * component.Score;
		}
		return num;
	}
}
