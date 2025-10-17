using MoonSharp.Interpreter;

namespace Game.Scripting.Interactive;

public interface IPageUI
{
	void say(string text);

	void clear();

	int start_goal(string title, string message, string style);

	void update_goal(int goalId, object value, string customDisplay);

	void finish_goal(int goalId);

	void reload_button();

	void button(string text, Closure closure);

	void nav_button(string text, Closure closure);

	void remove_last();

	int add_arrow_overlay(object locator, string color);

	void remove_arrow_overlay(int arrowId);
}
