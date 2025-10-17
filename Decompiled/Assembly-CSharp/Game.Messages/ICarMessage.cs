namespace Game.Messages;

public interface ICarMessage : IGameMessage
{
	string carId { get; }
}
