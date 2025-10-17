using Game.AccessControl;
using MessagePack;
using Model.AI;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Crew)]
[MessagePackObject(false)]
public struct AutoEngineerContextualOrder : IGameMessage
{
	[Key(0)]
	public string LocomotiveId { get; set; }

	[Key(1)]
	public ContextualOrder.OrderValue Order { get; set; }

	[Key(2)]
	public string Context { get; set; }

	public AutoEngineerContextualOrder(string locomotiveId, ContextualOrder.OrderValue order, string context)
	{
		LocomotiveId = locomotiveId;
		Order = order;
		Context = context;
	}
}
