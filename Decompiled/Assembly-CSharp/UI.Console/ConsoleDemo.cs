using System.Collections;
using UnityEngine;

namespace UI.Console;

[RequireComponent(typeof(Console))]
public class ConsoleDemo : MonoBehaviour
{
	private Console _console;

	private string _lipsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

	private void Awake()
	{
		_console = GetComponent<Console>();
	}

	private void OnEnable()
	{
		StartCoroutine("Demo");
	}

	private void OnDisable()
	{
		StopCoroutine("Demo");
	}

	private IEnumerator Demo()
	{
		int i = 0;
		while (true)
		{
			int length = Random.Range(10, _lipsum.Length / 3);
			string arg = _lipsum.Substring(0, length);
			_console.AddLine($"{Time.time:F2} {arg}");
			yield return new WaitForSeconds(1f);
			i++;
		}
	}
}
