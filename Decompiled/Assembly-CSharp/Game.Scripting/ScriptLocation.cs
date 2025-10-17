using MoonSharp.Interpreter.Interop;
using Track;

namespace Game.Scripting;

public class ScriptLocation
{
	[MoonSharpVisible(false)]
	public readonly Location Location;

	public object position => ScriptVector3.DictionaryRepresentation(Graph.Shared.GetPosition(Location));

	internal ScriptLocation(Location loc)
	{
		Location = loc;
	}

	public static ScriptLocation @new(string segmentId, float distance, int end)
	{
		Graph shared = Graph.Shared;
		TrackSegment.End end2 = ((end != 0) ? TrackSegment.End.B : TrackSegment.End.A);
		return new ScriptLocation((shared == null) ? new Location(null, distance, end2) : shared.MakeLocation(new SerializableLocation(segmentId, distance, end2)));
	}

	public static ScriptLocation @new(string str)
	{
		return new ScriptLocation(Graph.Shared.ResolveLocationString(str));
	}

	public ScriptLocation flipped()
	{
		return new ScriptLocation(Location.Flipped());
	}

	public static ScriptLocation operator +(ScriptLocation l, float distance)
	{
		return new ScriptLocation(Graph.Shared.LocationByMoving(l.Location, distance, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true));
	}

	public static ScriptLocation operator -(ScriptLocation l, float distance)
	{
		return new ScriptLocation(Graph.Shared.LocationByMoving(l.Location, 0f - distance, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true));
	}

	public override string ToString()
	{
		return Location.ToString();
	}

	protected bool Equals(ScriptLocation other)
	{
		return Location.Equals(other.Location);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((ScriptLocation)obj);
	}

	public override int GetHashCode()
	{
		return Location.GetHashCode();
	}
}
