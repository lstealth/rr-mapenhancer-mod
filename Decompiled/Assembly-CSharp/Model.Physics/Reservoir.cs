namespace Model.Physics;

public class Reservoir
{
	public enum Pipe
	{
		Line,
		Feed,
		HalfInch
	}

	public readonly string Name;

	public float Pressure;

	public readonly float Volume;

	public Reservoir(string name, float volume, float pressure)
	{
		Name = name;
		Volume = volume;
		Pressure = pressure;
	}

	public static void Equalize(Reservoir a, Reservoir b)
	{
		float pressure = b.Pressure;
		float num = a.Pressure - pressure;
		float num2 = a.Volume / b.Volume;
		float num3 = num / (num2 + 1f);
		a.Pressure -= num3;
		b.Pressure += num3;
	}
}
