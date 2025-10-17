using System.Collections;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using TMPro;
using UnityEngine;

namespace UI;

public class BalanceDisplay : MonoBehaviour
{
	[SerializeField]
	private TMP_Text text;

	[SerializeField]
	private AnimationCurve deltaToDuration = AnimationCurve.Linear(0f, 0.25f, 10000f, 1.5f);

	private int _lastBalance;

	private int _targetBalance;

	private Coroutine _coroutine;

	private void OnEnable()
	{
		Messenger.Default.Register<BalanceDidChange>(this, delegate
		{
			UpdateBalance(animate: true);
		});
		Messenger.Default.Register<PropertiesDidRestore>(this, delegate
		{
			UpdateBalance(animate: false);
		});
		UpdateBalance(animate: false);
	}

	private void OnDisable()
	{
		CancelAnimation();
		Messenger.Default.Unregister(this);
	}

	private void CancelAnimation()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
			_coroutine = null;
		}
	}

	private void UpdateBalance(bool animate)
	{
		int balance = StateManager.Shared.Balance;
		if (animate)
		{
			_targetBalance = balance;
			if (_coroutine == null)
			{
				_coroutine = StartCoroutine(AnimateBalance());
			}
		}
		else
		{
			CancelAnimation();
			ShowBalance(balance);
		}
	}

	private IEnumerator AnimateBalance()
	{
		float t0 = Time.unscaledTime;
		int startBalance = _lastBalance;
		switch (Mathf.Abs(_targetBalance - startBalance))
		{
		}
		float num = deltaToDuration.Evaluate(Mathf.Abs(_targetBalance - startBalance));
		float t1 = t0 + num;
		while (Time.unscaledTime < t1)
		{
			float unscaledTime = Time.unscaledTime;
			float t2 = Mathf.InverseLerp(t0, t1, unscaledTime);
			float num2 = Mathf.SmoothStep(startBalance, _targetBalance, t2);
			ShowBalance((num2 < (float)_targetBalance) ? Mathf.CeilToInt(num2) : Mathf.FloorToInt(num2));
			yield return null;
		}
		ShowBalance(_targetBalance);
		_coroutine = null;
	}

	private void ShowBalance(int balance)
	{
		_lastBalance = balance;
		text.text = $"{balance:C0}";
	}
}
