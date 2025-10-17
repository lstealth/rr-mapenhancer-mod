using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UI.Menu;

public class GrammarRailroadNameGenerator
{
	private readonly Dictionary<string, List<string>> _grammar = new Dictionary<string, List<string>>();

	private readonly Dictionary<string, HashSet<string>> _used = new Dictionary<string, HashSet<string>>();

	private string _builtinGrammar = "\r\n_root:\r\n<noun> <rr>\r\n<noun> & <noun> <rr>\r\n<noun> & <noun> <rr>\r\n<noun> & <noun> <rr>\r\n<noun>, <noun> & <noun> <rr>\r\n\r\nnoun:\r\n<place>\r\n<place>\r\n<place>\r\n<place>\r\n<place>\r\n<place>\r\n<dir> <place>\r\n<dir> <place>\r\n<dirern>\r\n\r\nplace:\r\nAlarka\r\nAlmond\r\nAndrews\r\nAlleghany\r\nAppalachian\r\nAsheville\r\nBalsam\r\nBlack Mountain\r\nBlue Ridge\r\nBryson\r\nCanton\r\nCarolina\r\nCherokee\r\nCounty Line\r\nCumberland\r\nDillard\r\nDillsboro\r\nDucktown\r\nFontana\r\nHickory\r\nJupiter\r\nLeConte\r\nMarble\r\nMarion\r\nMaryland\r\nMercer\r\nMineral Bluff\r\nMurphy\r\nNantahala\r\nPisgah\r\nRobbinsville\r\nSaluda\r\nSmokey Mountain\r\nSpruce Pine\r\nSulphur Springs\r\nSylva\r\nTallulah Falls\r\nTennessee\r\nTopton\r\nToxaway\r\nTuckasegee\r\nVirginia\r\n\r\nrr:\r\nRailroad\r\nRailroad\r\nRailroad\r\nRailway\r\nRailway\r\nLines\r\n\r\ndir:\r\nWest\r\nEast\r\nNorth\r\nSouth\r\n\r\ndirern:\r\nWestern\r\nNorthwestern\r\nSouthwestern\r\nEastern\r\nNortheastern\r\nSoutheastern\r\nNorthern\r\nSouthern\r\n";

	public GrammarRailroadNameGenerator()
	{
		LoadGrammar(_builtinGrammar);
	}

	public void LoadGrammar(string grammar)
	{
		_grammar.Clear();
		IEnumerable<string> enumerable = from line in grammar.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
			select line.Trim() into line
			where line.Length > 0
			select line;
		string key = "";
		foreach (string item in enumerable)
		{
			if (item.EndsWith(':'))
			{
				key = item.Substring(0, item.Length - 1);
				continue;
			}
			if (!_grammar.ContainsKey(key))
			{
				_grammar[key] = new List<string>();
			}
			_grammar[key].Add(item);
		}
	}

	public string Generate()
	{
		while (true)
		{
			_used.Clear();
			string text = Random("_root");
			do
			{
				if (text.Contains('<'))
				{
					int num = text.IndexOf('<');
					int num2 = text.IndexOf('>');
					string key = text.Substring(num + 1, num2 - num - 1);
					string value = Random(key);
					StringBuilder stringBuilder = new StringBuilder(text);
					stringBuilder.Remove(num, num2 - num + 1);
					stringBuilder.Insert(num, value);
					text = stringBuilder.ToString();
					continue;
				}
				return text;
			}
			while (text.Split(" ").Count((string word) => word != "&") <= 5);
		}
	}

	private string Random(string key)
	{
		List<string> list = _grammar[key];
		string text = "";
		do
		{
			text = list[UnityEngine.Random.Range(0, list.Count)];
		}
		while (_used.ContainsKey(key) && _used[key].Contains(text) && _used[key].Count < list.Count);
		if (!_used.ContainsKey(key))
		{
			_used[key] = new HashSet<string>();
		}
		_used[key].Add(text);
		return text;
	}
}
