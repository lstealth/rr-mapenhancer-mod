using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.State;
using Model;
using UI.Common;
using UI.LazyScrollList;
using UnityEngine;

namespace UI.EngineRoster;

public class EngineRosterPanel : MonoBehaviour
{
	[SerializeField]
	private UI.LazyScrollList.LazyScrollList lazyScrollList;

	[SerializeField]
	private RectTransform emptyStateRect;

	private Window _window;

	private HashSet<string> _cachedFavorites;

	private Coroutine _coroutine;

	private int _lastHash;

	private static PlayerPropertiesManager PropertiesManager => PlayerPropertiesManager.Shared;

	public static EngineRosterPanel Shared => WindowManager.Shared.GetWindow<EngineRosterPanel>();

	public static void Show()
	{
		Shared._Show();
	}

	private void _Show()
	{
		_window.Title = "Engine Roster";
		_window.ShowWindow();
	}

	public static void Toggle()
	{
		if (Shared._window.IsShown)
		{
			Shared._window.CloseWindow();
		}
		else
		{
			Shared._Show();
		}
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
		_window.OnShownWillChange += WindowShownWillChange;
		Vector2Int vector2Int = new Vector2Int(560, 150);
		_window.SetInitialPositionSize("EngineRoster", vector2Int, Window.Position.LowerRight, Window.Sizing.Resizable(vector2Int, new Vector2Int(650, Screen.height)));
	}

	private void WindowShownWillChange(bool shown)
	{
		if (shown)
		{
			if (_coroutine == null)
			{
				_coroutine = StartCoroutine(UpdateCoroutine());
			}
			return;
		}
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	private IEnumerator UpdateCoroutine()
	{
		while (true)
		{
			Rebuild();
			yield return new WaitForSecondsRealtime(1f);
		}
	}

	private void Rebuild()
	{
		TrainController shared = TrainController.Shared;
		if (_cachedFavorites == null)
		{
			_cachedFavorites = PropertiesManager.MyProperties.FavoriteEngineIds;
			bool flag = false;
			foreach (string cachedFavorite in _cachedFavorites)
			{
				if (!shared.TryGetCarForId(cachedFavorite, out var _))
				{
					flag = true;
					_cachedFavorites.Remove(cachedFavorite);
				}
			}
			if (flag)
			{
				SaveCachedFavorites();
			}
		}
		Car selectedCar = shared.SelectedCar;
		List<RosterRowData> list = (from BaseLocomotive e in shared.Cars.Where((Car c) => c is BaseLocomotive)
			orderby (!_cachedFavorites.Contains(e.id)) ? 1 : 0, e.SortName
			select new RosterRowData(e, _cachedFavorites.Contains(e.id), selectedCar == e, this)).ToList();
		int rowDataHash = GetRowDataHash(list);
		if (rowDataHash != _lastHash)
		{
			Populate(list);
			_lastHash = rowDataHash;
			return;
		}
		foreach (EngineRosterRow visibleCell in lazyScrollList.VisibleCells)
		{
			visibleCell.Refresh();
		}
	}

	private void Populate(List<RosterRowData> rows)
	{
		lazyScrollList.SetData(rows.Cast<object>().ToList());
		if (!_window.HasUserResized)
		{
			int a = rows.Count((RosterRowData r) => r.IsFavorite);
			int count = rows.Count;
			int num = Mathf.Clamp(Mathf.Max(a, Mathf.Min(count, 5)), 3, 10);
			_window.SetContentHeight(30 * num + 4);
		}
		emptyStateRect.gameObject.SetActive(rows.Count == 0);
	}

	public void ToggleFavorite(BaseLocomotive engine)
	{
		if (!_cachedFavorites.Add(engine.id))
		{
			_cachedFavorites.Remove(engine.id);
		}
		SaveCachedFavorites();
		Rebuild();
	}

	private void SaveCachedFavorites()
	{
		PropertiesManager.UpdateMyProperties(delegate(PlayerProperties props)
		{
			props.FavoriteEngineIds = _cachedFavorites;
			return props;
		});
	}

	public void SelectEngine(BaseLocomotive engine, bool select)
	{
		TrainController.Shared.SelectedCar = (select ? engine : null);
		Rebuild();
	}

	private static int GetRowDataHash(IEnumerable<RosterRowData> data)
	{
		int num = 19;
		foreach (RosterRowData datum in data)
		{
			num = num * 31 + datum.GetHashCode();
		}
		return num;
	}
}
