using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack.Unity.Extension;

namespace Game.Messages;

public static class MessagepackSupport
{
	private static bool _hasSetupMessagepack;

	public static void Setup()
	{
		if (!_hasSetupMessagepack)
		{
			_hasSetupMessagepack = true;
			StaticCompositeResolver.Instance.Register(UnityResolver.Instance, UnityBlitWithPrimitiveArrayResolver.Instance, StandardResolver.Instance);
			MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
		}
	}
}
