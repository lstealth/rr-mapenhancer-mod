namespace RollingStock;

public interface ICarMovementListener
{
	void CarDidMove(MovementInfo info);
}
