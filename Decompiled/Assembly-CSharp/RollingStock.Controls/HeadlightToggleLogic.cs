using System;
using Effects;
using Game.Messages;
using KeyValue.Runtime;

namespace RollingStock.Controls;

public static class HeadlightToggleLogic
{
	public enum State
	{
		Off,
		ForwardDim,
		ForwardOn,
		RearDim,
		RearOn,
		BothFull,
		BothDim
	}

	private static readonly State[] BidirectionalStates = new State[5]
	{
		State.RearOn,
		State.RearDim,
		State.Off,
		State.ForwardDim,
		State.ForwardOn
	};

	public static State SetHeadlightStateOffset(KeyValueObject keyValueObject, int direction)
	{
		State headlightState = GetHeadlightState(keyValueObject);
		State state = OffsetState(direction, wrap: true, headlightState);
		SetHeadlightState(keyValueObject, state);
		return state;
	}

	private static State OffsetState(int direction, bool wrap, State currentState)
	{
		State[] bidirectionalStates = BidirectionalStates;
		int i = IndexOf(bidirectionalStates, currentState) + direction;
		if (i < 0 || i >= bidirectionalStates.Length)
		{
			if (!wrap)
			{
				return currentState;
			}
			for (; i < 0; i += bidirectionalStates.Length)
			{
			}
			i %= bidirectionalStates.Length;
		}
		return bidirectionalStates[i];
	}

	private static int IndexOf(State[] states, State target)
	{
		for (int i = 0; i < states.Length; i++)
		{
			if (states[i] == target)
			{
				return i;
			}
		}
		return -1;
	}

	public static string TextForState(State state)
	{
		return state switch
		{
			State.Off => "Off", 
			State.ForwardDim => "Front Dim", 
			State.ForwardOn => "Front Full", 
			State.RearDim => "Rear Dim", 
			State.RearOn => "Rear Full", 
			State.BothDim => "Both Dim", 
			State.BothFull => "Both Full", 
			_ => "?", 
		};
	}

	public unsafe static void SetHeadlightState(KeyValueObject kvObject, State state)
	{
		string key = PropertyChange.KeyForControl(PropertyChange.Control.Headlight);
		object obj = state switch
		{
			State.Off => (HeadlightController.State.Off, HeadlightController.State.Off), 
			State.ForwardDim => (HeadlightController.State.Dim, HeadlightController.State.Off), 
			State.ForwardOn => (HeadlightController.State.On, HeadlightController.State.Off), 
			State.RearDim => (HeadlightController.State.Off, HeadlightController.State.Dim), 
			State.RearOn => (HeadlightController.State.Off, HeadlightController.State.On), 
			State.BothDim => (HeadlightController.State.Dim, HeadlightController.State.Dim), 
			State.BothFull => (HeadlightController.State.On, HeadlightController.State.On), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		HeadlightController.State item = ((ValueTuple<HeadlightController.State, HeadlightController.State>*)(&obj))->Item1;
		HeadlightController.State item2 = ((ValueTuple<HeadlightController.State, HeadlightController.State>*)(&obj))->Item2;
		int value = HeadlightStateLogic.IntFromStates(item, item2);
		kvObject.Set(key, Value.Int(value));
	}

	public static State GetHeadlightState(KeyValueObject kvObject)
	{
		string key = PropertyChange.KeyForControl(PropertyChange.Control.Headlight);
		var (state, state2) = HeadlightStateLogic.StatesFromInt(kvObject.Get(key).IntValue);
		switch (state)
		{
		case HeadlightController.State.Off:
			if (state2 != HeadlightController.State.Off)
			{
				goto default;
			}
			return State.Off;
		case HeadlightController.State.Dim:
			if (state2 == HeadlightController.State.Dim)
			{
				return State.BothDim;
			}
			return State.ForwardDim;
		case HeadlightController.State.On:
			if (state2 == HeadlightController.State.On)
			{
				return State.BothFull;
			}
			return State.ForwardOn;
		default:
			return state2 switch
			{
				HeadlightController.State.On => State.RearOn, 
				HeadlightController.State.Dim => State.RearDim, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
	}
}
