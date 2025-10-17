namespace Audio.DynamicChuff;

public interface IDynamicChuffDelegate
{
	void ScheduleNextChuff(float delay, float chuffDuration);
}
