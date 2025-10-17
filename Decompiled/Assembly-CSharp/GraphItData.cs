using System;
using System.Collections.Generic;
using UnityEngine;

public class GraphItData
{
	public const int DEFAULT_SAMPLES = 2048;

	public const int RECENT_WINDOW_SIZE = 120;

	public Dictionary<string, GraphItDataInternal> mData = new Dictionary<string, GraphItDataInternal>();

	public string mName;

	public int mCurrentIndex;

	public bool mInclude0;

	public bool mReadyForUpdate;

	public bool mFixedUpdate;

	public int mWindowSize;

	public bool mFullArray;

	public bool mSharedYAxis;

	protected bool mHidden;

	protected float mHeight;

	public GraphItData(string name)
	{
		mName = name;
		mData = new Dictionary<string, GraphItDataInternal>();
		mCurrentIndex = 0;
		mInclude0 = true;
		mReadyForUpdate = false;
		mFixedUpdate = false;
		mWindowSize = 2048;
		mFullArray = false;
		mSharedYAxis = false;
		mHidden = false;
		mHeight = 175f;
		if (PlayerPrefs.HasKey(mName + "_height"))
		{
			SetHeight(PlayerPrefs.GetFloat(mName + "_height"));
		}
		if (PlayerPrefs.HasKey(mName + "_hidden"))
		{
			SetHidden(PlayerPrefs.GetInt(mName + "_hidden") == 1);
		}
	}

	public int GraphLength()
	{
		if (mFullArray)
		{
			return GraphFullLength();
		}
		return mCurrentIndex;
	}

	public int GraphFullLength()
	{
		return mWindowSize;
	}

	public float GetMin(string subgraph)
	{
		if (mSharedYAxis)
		{
			bool flag = false;
			float num = 0f;
			{
				foreach (KeyValuePair<string, GraphItDataInternal> mDatum in mData)
				{
					GraphItDataInternal value = mDatum.Value;
					if (!flag)
					{
						num = value.mMin;
						flag = true;
					}
					num = Math.Min(num, value.mMin);
				}
				return num;
			}
		}
		if (!mData.ContainsKey(subgraph))
		{
			mData[subgraph] = new GraphItDataInternal(mData.Count);
		}
		return mData[subgraph].mMin;
	}

	public float GetMax(string subgraph)
	{
		if (mSharedYAxis)
		{
			bool flag = false;
			float num = 0f;
			{
				foreach (KeyValuePair<string, GraphItDataInternal> mDatum in mData)
				{
					GraphItDataInternal value = mDatum.Value;
					if (!flag)
					{
						num = value.mMax;
						flag = true;
					}
					num = Math.Max(num, value.mMax);
				}
				return num;
			}
		}
		if (!mData.ContainsKey(subgraph))
		{
			mData[subgraph] = new GraphItDataInternal(mData.Count);
		}
		return mData[subgraph].mMax;
	}

	public float GetHeight()
	{
		return mHeight;
	}

	public void SetHeight(float height)
	{
		mHeight = height;
	}

	public void DoHeightDelta(float delta)
	{
		SetHeight(Mathf.Max(mHeight + delta, 50f));
		PlayerPrefs.SetFloat(mName + "_height", GetHeight());
	}

	public bool GetHidden()
	{
		return mHidden;
	}

	public void SetHidden(bool hidden)
	{
		mHidden = hidden;
		PlayerPrefs.SetInt(mName + "_hidden", GetHidden() ? 1 : 0);
	}
}
