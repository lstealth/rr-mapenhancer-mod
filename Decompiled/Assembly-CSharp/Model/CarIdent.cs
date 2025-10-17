using System;

namespace Model;

public struct CarIdent
{
	public string ReportingMark;

	public string RoadNumber;

	public CarIdent(string reportingMark, string roadNumber)
	{
		ReportingMark = reportingMark;
		RoadNumber = roadNumber;
	}

	public override string ToString()
	{
		return ReportingMark + " " + RoadNumber;
	}

	public bool Equals(CarIdent other)
	{
		if (ReportingMark == other.ReportingMark)
		{
			return RoadNumber == other.RoadNumber;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is CarIdent other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(ReportingMark, RoadNumber);
	}
}
