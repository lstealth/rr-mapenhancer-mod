using UnityEngine;

public class GraphItDataInternal
{
	public float[] mDataPoints;

	public float mCounter;

	public float mMin;

	public float mMax;

	public float mAvg;

	public float mFastAvg;

	public Color mColor;

	public GraphItDataInternal(int subgraph_index)
	{
		mDataPoints = new float[2048];
		mCounter = 0f;
		mMin = 0f;
		mMax = 0f;
		mAvg = 0f;
		mFastAvg = 0f;
		switch (subgraph_index)
		{
		case 0:
			mColor = new Color(0f, 0.85f, 1f, 1f);
			break;
		case 1:
			mColor = Color.yellow;
			break;
		case 2:
			mColor = Color.green;
			break;
		case 3:
			mColor = Color.red;
			break;
		default:
			mColor = Color.green;
			break;
		}
	}
}
