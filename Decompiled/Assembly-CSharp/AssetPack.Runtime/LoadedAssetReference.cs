using System;

namespace AssetPack.Runtime;

public class LoadedAssetReference<T> : IDisposable
{
	private readonly AssetPackRuntimeStore _store;

	private readonly string _identifier;

	public T Asset { get; private set; }

	public LoadedAssetReference(T asset, AssetPackRuntimeStore store, string identifier)
	{
		Asset = asset;
		_store = store;
		_identifier = identifier;
	}

	public void Dispose()
	{
		_store.DecrementReferenceCount(_identifier);
	}
}
