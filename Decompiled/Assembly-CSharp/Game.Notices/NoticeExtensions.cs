using Model;
using Serilog;

namespace Game.Notices;

public static class NoticeExtensions
{
	public static void PostNotice(this Car car, string key, string content)
	{
		NoticeManager shared = NoticeManager.Shared;
		if (shared == null)
		{
			if (!string.IsNullOrEmpty(content))
			{
				Log.Warning("Can't post notice without NoticeManager instance.");
			}
		}
		else
		{
			shared.PostEphemeral(new EntityReference(EntityType.Car, car.id), key, content);
		}
	}
}
