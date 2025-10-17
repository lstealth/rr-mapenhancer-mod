using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct CarSetIdent : IGameMessage
{
	[Key(0)]
	public string CarId { get; set; }

	[Key(1)]
	public string ReportingMark { get; set; }

	[Key(2)]
	public string RoadNumber { get; set; }

	public CarSetIdent(string carId, string reportingMark, string roadNumber)
	{
		CarId = carId;
		ReportingMark = reportingMark;
		RoadNumber = roadNumber;
	}
}
