using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace AssetPack.Runtime;

internal static class AsyncExtensionMethods
{
	public static TaskAwaiter<AssetBundle> GetAwaiter(this AssetBundleCreateRequest request)
	{
		TaskCompletionSource<AssetBundle> tcs = new TaskCompletionSource<AssetBundle>();
		request.completed += delegate(AsyncOperation obj)
		{
			if (obj is AssetBundleCreateRequest assetBundleCreateRequest && assetBundleCreateRequest.assetBundle != null)
			{
				tcs.SetResult(assetBundleCreateRequest.assetBundle);
			}
			else
			{
				tcs.SetException(new Exception("Failed to load asset bundle"));
			}
		};
		return tcs.Task.GetAwaiter();
	}

	public static TaskAwaiter<UnityEngine.Object> GetAwaiter(this AssetBundleRequest request)
	{
		TaskCompletionSource<UnityEngine.Object> tcs = new TaskCompletionSource<UnityEngine.Object>();
		request.completed += delegate(AsyncOperation obj)
		{
			if (obj is AssetBundleRequest assetBundleRequest && assetBundleRequest.asset != null)
			{
				tcs.SetResult(assetBundleRequest.asset);
			}
			else
			{
				tcs.SetException(new Exception("Failed to load asset from asset bundle"));
			}
		};
		return tcs.Task.GetAwaiter();
	}
}
