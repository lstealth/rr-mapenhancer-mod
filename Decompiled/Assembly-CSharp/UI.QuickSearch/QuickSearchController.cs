using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using FuzzySharp;
using Game;
using Game.State;
using Helpers;
using Model;
using Model.Ops;
using Serilog;
using TMPro;
using UI.LazyScrollList;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.QuickSearch;

public class QuickSearchController : MonoBehaviour
{
	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private Image backdropImage;

	[SerializeField]
	private RectTransform panelRectTransform;

	[SerializeField]
	private UI.LazyScrollList.LazyScrollList resultsScrollList;

	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private InputActionAsset inputActions;

	private InputActionMap _actionMap;

	private InputAction _actionResultNext;

	private InputAction _actionResultPrev;

	private InputAction _actionActivate;

	private InputAction _actionJumpTo;

	private int _index;

	private List<QuickSearchResult> _results = new List<QuickSearchResult>();

	public static string BindingDisplayStringActivate = "";

	public static string BindingDisplayStringJumpTo = "";

	public static QuickSearchController Shared { get; private set; }

	private void Awake()
	{
		Shared = this;
		canvas.enabled = false;
	}

	private void OnDestroy()
	{
		if (Shared == this)
		{
			Shared = null;
		}
	}

	public void Show()
	{
	}

	public void Hide()
	{
		inputField.enabled = false;
		canvas.enabled = false;
		_actionMap.Disable();
		GameInput.shared.SetPaused(paused: false);
		GameInput.UnregisterEscapeHandler(GameInput.EscapeHandler.QuickSearch);
	}

	private void UpdateHighlightedRow()
	{
		ApplyResultsToList();
	}

	private void HandleQueryInputChanged(string query)
	{
		_results = (from r in SearchResults(query)
			orderby r.Score descending
			select r).ToList();
		_index = -1;
		ApplyResultsToList();
	}

	private void ApplyResultsToList()
	{
		resultsScrollList.SetData(_results.Select((QuickSearchResult r, int i) => new QuickSearchRow(r, i == _index)).Cast<object>().ToList());
		resultsScrollList.ScrollToCell(_index);
	}

	private IEnumerable<QuickSearchItem> AllItems()
	{
		foreach (Car car in TrainController.Shared.Cars)
		{
			yield return new QuickSearchItem(car.DisplayName, QuickSearchItemType.Equipment, car, QuickSearchAction.Inspect | QuickSearchAction.JumpTo);
		}
		OpsController shared = OpsController.Shared;
		foreach (Industry item in shared.AllIndustries.Where((Industry i) => !i.ProgressionDisabled))
		{
			yield return new QuickSearchItem(item.name, QuickSearchItemType.Location, item, QuickSearchAction.Inspect | QuickSearchAction.JumpTo);
		}
		foreach (SpawnPoint item2 in SpawnPoint.All)
		{
			yield return new QuickSearchItem(item2.name, QuickSearchItemType.PointOfInterest, item2, QuickSearchAction.JumpTo);
		}
		foreach (IPlayer allPlayer in StateManager.Shared.PlayersManager.AllPlayers)
		{
			yield return new QuickSearchItem(allPlayer.Name, QuickSearchItemType.Player, allPlayer, QuickSearchAction.Inspect | QuickSearchAction.JumpTo);
		}
	}

	private IEnumerable<QuickSearchResult> SearchResults(string query)
	{
		query = query.ToLower();
		foreach (QuickSearchItem item in AllItems())
		{
			int num = ScoreQuery(query, item.SearchString);
			if (num >= 50)
			{
				yield return new QuickSearchResult(num, item);
			}
		}
	}

	private int ScoreQuery(string needle, string haystack)
	{
		return Fuzz.WeightedRatio(needle, haystack);
	}

	private void HandleInputResultOffset(int offset)
	{
		_index = Mathf.Clamp(_index + offset, 0, _results.Count - 1);
		UpdateHighlightedRow();
	}

	private void HandleInputActivate()
	{
		if (!TryGetItem(out var item))
		{
			return;
		}
		if ((item.Actions & QuickSearchAction.Inspect) == 0)
		{
			Log.Warning("QuickSearch: Inspect is not supported for this result: {item}", item.Object);
			return;
		}
		Hide();
		Log.Debug("QuickSearch Activate {item}", item.Object);
		object obj = item.Object;
		Hyperlink link;
		if (!(obj is Industry industry))
		{
			if (!(obj is Car car))
			{
				if (!(obj is IPlayer player))
				{
					throw new ArgumentException($"Unknown object type: {item.Object}");
				}
				link = Hyperlink.To(player);
			}
			else
			{
				link = Hyperlink.To(car);
			}
		}
		else
		{
			link = Hyperlink.To(industry);
		}
		LinkDispatcher.Open(link);
	}

	private void HandleInputJumpTo()
	{
		if (!TryGetItem(out var item))
		{
			return;
		}
		if ((item.Actions & QuickSearchAction.JumpTo) == 0)
		{
			Log.Warning("QuickSearch: JumpTo is not supported for this result: {item}", item.Object);
			return;
		}
		Hide();
		object obj = item.Object;
		if (!(obj is Industry industry))
		{
			if (!(obj is Car car))
			{
				if (!(obj is IPlayer player))
				{
					if (!(obj is SpawnPoint spawnPoint))
					{
						throw new ArgumentException($"Unknown object type: {item.Object}");
					}
					CameraSelector.shared.JumpToPoint(spawnPoint.transform.GamePosition(), spawnPoint.transform.rotation);
				}
				else
				{
					CameraSelector.shared.JumpToPoint(player.GamePosition, Quaternion.identity);
				}
			}
			else
			{
				CameraSelector.shared.FollowCar(car);
			}
		}
		else
		{
			CameraSelector.shared.JumpToPoint(industry.transform.GamePosition(), Quaternion.identity);
		}
		Log.Debug("QuickSearch JumpTo {item}", item.Object);
	}

	private bool TryGetItem(out QuickSearchItem item)
	{
		if (_index < 0 || _index >= _results.Count)
		{
			Log.Warning("Index out of range {index}", _index);
			item = default(QuickSearchItem);
			return false;
		}
		item = _results[_index].Item;
		return true;
	}
}
