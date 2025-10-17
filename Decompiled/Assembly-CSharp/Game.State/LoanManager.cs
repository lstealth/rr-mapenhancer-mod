using System;
using System.Collections;
using System.Linq;
using Game.Messages;
using Model;
using Network;
using Serilog;
using UnityEngine;

namespace Game.State;

public class LoanManager : MonoBehaviour
{
	private GameStorage _gameStorage;

	private Coroutine _updateCoroutine;

	private const float InterestPercent = 0.1f;

	private const int InterestPaymentIntervalDays = 5;

	public int LoanAmount
	{
		get
		{
			return _gameStorage.LoanAmount;
		}
		private set
		{
			_gameStorage.LoanAmount = value;
		}
	}

	private GameDateTime? NextInterestDate
	{
		get
		{
			return _gameStorage.NextInterestDate;
		}
		set
		{
			_gameStorage.NextInterestDate = value;
		}
	}

	private int LoanNextInterestOffset
	{
		get
		{
			return _gameStorage.LoanNextInterestOffset;
		}
		set
		{
			_gameStorage.LoanNextInterestOffset = value;
		}
	}

	private static GameDateTime Now => TimeWeather.Now;

	public static bool CanRequestLoanChange => StateManager.CheckAuthorizedToSendMessage(new RequestLoanDelta(0));

	public void Configure(GameStorage gameStorage)
	{
		_gameStorage = gameStorage;
		StartIfNeeded();
	}

	private void OnEnable()
	{
		StartIfNeeded();
	}

	private void OnDisable()
	{
		if (_updateCoroutine != null)
		{
			StopCoroutine(_updateCoroutine);
		}
		_updateCoroutine = null;
	}

	private void StartIfNeeded()
	{
		if (StateManager.IsHost && _updateCoroutine == null && _gameStorage != null)
		{
			_updateCoroutine = StartCoroutine(UpdateCoroutine());
		}
	}

	private IEnumerator UpdateCoroutine()
	{
		StateManager.AssertIsHost();
		while (base.enabled)
		{
			PayInterestIfNeeded();
			yield return new WaitForSeconds(5f);
		}
	}

	private void PayInterestIfNeeded()
	{
		GameDateTime? nextInterestDate = NextInterestDate;
		if (nextInterestDate.HasValue)
		{
			GameDateTime now = Now;
			if (!(nextInterestDate.Value > now))
			{
				int loanAmount = LoanAmount;
				int num = CalculateInterestPayment(loanAmount);
				Log.Information("Withdrawing loan interest on {amount}: {interest}", loanAmount, num);
				StateManager.Shared.ApplyToBalance(-num, Ledger.Category.Loan, null);
				NextInterestDate = nextInterestDate.Value.AddingDays(5f);
				LoanNextInterestOffset = 0;
			}
		}
	}

	public int ApprovedLoanAmount()
	{
		return ValueOfAssets();
	}

	private int ValueOfAssets()
	{
		return TrainController.Shared.Cars.Where(EquipmentPurchase.CarCanBeSold).Sum((Func<Car, int>)EquipmentPurchase.TradeInValueForCar);
	}

	public void HandleOffsetLoanRequest(int loanDeltaAmount, IPlayer sender)
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		try
		{
			HandleOffsetLoan(loanDeltaAmount);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error from HandleOffsetLoan {amount}", loanDeltaAmount);
			if (ex is DisplayableException ex2)
			{
				Multiplayer.SendError(sender, ex2.DisplayMessage);
			}
			else
			{
				Multiplayer.SendError(sender, "Unable to adjust loan.");
			}
		}
	}

	private void HandleOffsetLoan(int loanDeltaAmount)
	{
		StateManager.AssertIsHost();
		if (loanDeltaAmount == 0)
		{
			return;
		}
		GameDateTime now = Now;
		int balance = StateManager.Shared.GetBalance();
		int loanAmount = LoanAmount;
		if (loanDeltaAmount < 0)
		{
			loanDeltaAmount = -Mathf.Min(-loanDeltaAmount, loanAmount);
			if (balance < -loanDeltaAmount)
			{
				throw new DisplayableException($"Insufficient balance to pay down loan {-loanDeltaAmount:C0}; balance is {balance:C0}.");
			}
		}
		else
		{
			int num = ApprovedLoanAmount();
			if (num - loanAmount < loanDeltaAmount)
			{
				throw new DisplayableException($"Insufficient capital to finance loan. Loan limit is {num:C0}, current is {loanAmount}.");
			}
		}
		GameDateTime? nextInterestDate = NextInterestDate;
		LoanNextInterestOffset = CalculateNextInterestOffset(loanAmount, loanAmount + loanDeltaAmount, now, nextInterestDate);
		LoanAmount += loanDeltaAmount;
		int loanAmount2 = LoanAmount;
		GameDateTime? gameDateTime = (NextInterestDate = (nextInterestDate.HasValue ? new GameDateTime?(nextInterestDate.Value) : ((loanAmount2 <= 0) ? ((GameDateTime?)null) : new GameDateTime?(now.StartOfDay.AddingDays(5f)))));
		StateManager.Shared.ApplyToBalance(loanDeltaAmount, Ledger.Category.Loan, null, null, 0, quiet: true);
		int num2 = CalculateInterestPayment(loanAmount2);
		string text = gameDateTime?.IntervalString(now) ?? "<error>";
		string message = ((loanDeltaAmount >= 0) ? $"Loan increased by {loanDeltaAmount:C0} to {loanAmount2:C0}. Interest payment of {num2:C0} due in {text}." : ((loanAmount2 != 0) ? $"Loan paid down by {-loanDeltaAmount:C0} to {loanAmount2:C0}. Interest payment of {num2:C0} due in {text}." : $"Loan paid down by {-loanDeltaAmount:C0} to {loanAmount2:C0}. Congratulations!"));
		Multiplayer.Broadcast(message);
	}

	private int CalculateNextInterestOffset(int existingLoanAmount, int newLoanAmount, GameDateTime now, GameDateTime? maybeExistingNextInterestDate)
	{
		int loanNextInterestOffset = LoanNextInterestOffset;
		return CalculateNextInterestOffset(existingLoanAmount, newLoanAmount, now, maybeExistingNextInterestDate, loanNextInterestOffset);
	}

	public static int CalculateNextInterestOffset(int existingLoanAmount, int newLoanAmount, GameDateTime now, GameDateTime? maybeExistingNextInterestDate, int existingOffset)
	{
		if (!maybeExistingNextInterestDate.HasValue)
		{
			return 0;
		}
		GameDateTime value = maybeExistingNextInterestDate.Value;
		if (value < now)
		{
			Debug.LogError($"existingNextInterestDate is before now: {value} vs {now}");
			return 0;
		}
		int num = Mathf.FloorToInt(value.DaysSince(now.StartOfDay));
		int num2 = 5 - num;
		int num3 = Mathf.RoundToInt((float)num2 * 0.02f * (float)existingLoanAmount);
		int num4 = Mathf.RoundToInt((float)num2 * 0.02f * (float)newLoanAmount);
		int num5 = existingOffset + (num3 - num4);
		Log.Debug("Calculated next interest offset {existingNextInterestDate}, {daysSinceInterestPayment}, {interestOnExisting}, {expectedFullInterest}, {existingOffset} -> {offset}", value, num2, num3, num4, existingOffset, num5);
		return num5;
	}

	public int CalculateInterestPayment(int loanAmount)
	{
		int num = Mathf.RoundToInt((float)loanAmount * 0.1f) + LoanNextInterestOffset;
		if (num < 0)
		{
			Log.Error("Calculated negative interest payment {payment} from {loanAmount}, {percent}, {nextInterestOffset}; returning 0.", num, loanAmount, 0.1f, LoanNextInterestOffset);
			return 0;
		}
		return num;
	}

	public (int nextPaymentAmount, string nextPaymentInterval) NextInterestPaymentInfo()
	{
		int item = CalculateInterestPayment(LoanAmount);
		GameDateTime? nextInterestDate = NextInterestDate;
		if (!nextInterestDate.HasValue)
		{
			return (nextPaymentAmount: item, nextPaymentInterval: "<error>");
		}
		return (nextPaymentAmount: item, nextPaymentInterval: nextInterestDate.Value.IntervalString(Now));
	}

	public void RequestLoanDelta(int deltaAmount)
	{
		StateManager.ApplyLocal(new RequestLoanDelta(deltaAmount));
	}
}
