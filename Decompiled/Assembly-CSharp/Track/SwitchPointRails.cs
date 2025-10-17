using UnityEngine;

namespace Track;

public class SwitchPointRails : MonoBehaviour
{
	public enum State
	{
		Unknown,
		Normal,
		Reversed
	}

	private TrackNode _node;

	public State state;

	private GameObject _normalPointRail;

	private GameObject _reversedPointRail;

	private float _normalRot;

	private float _reversedRot;

	public void Configure(TrackNode node, GameObject normalPointRail, GameObject reversedPointRail, float normalRot, float reversedRot)
	{
		_node = node;
		_normalPointRail = normalPointRail;
		_reversedPointRail = reversedPointRail;
		_normalRot = normalRot;
		_reversedRot = reversedRot;
	}

	private void Update()
	{
		if (_node == null)
		{
			return;
		}
		State state = ((!_node.isThrown) ? State.Normal : State.Reversed);
		if (state != this.state)
		{
			bool num = this.state == State.Unknown;
			this.state = state;
			var (vector, vector2) = LocalEulerForState(this.state);
			if (num)
			{
				_normalPointRail.transform.localEulerAngles = vector;
				_reversedPointRail.transform.localEulerAngles = vector2;
				return;
			}
			LTSeq lTSeq = LeanTween.sequence();
			lTSeq.append(0.6f);
			lTSeq.append(LeanTween.rotateLocal(_normalPointRail, vector, 1.3f).setEaseInOutCubic());
			LTSeq lTSeq2 = LeanTween.sequence();
			lTSeq2.append(0.6f);
			lTSeq2.append(LeanTween.rotateLocal(_reversedPointRail, vector2, 1.3f).setEaseInOutCubic());
		}
	}

	private (Vector3, Vector3) LocalEulerForState(State state)
	{
		float y;
		float y2;
		switch (state)
		{
		case State.Normal:
			y = 0f;
			y2 = _reversedRot;
			break;
		case State.Reversed:
			y = _normalRot;
			y2 = 0f;
			break;
		default:
			y = 0f;
			y2 = 0f;
			break;
		}
		return (new Vector3(0f, y, 0f), new Vector3(0f, y2, 0f));
	}
}
