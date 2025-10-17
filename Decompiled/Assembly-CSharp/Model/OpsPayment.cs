namespace Model;

public struct OpsPayment
{
	public int Time;

	public int Amount;

	public string Description;

	public OpsPayment(int time, int amount, string description)
	{
		Time = time;
		Amount = amount;
		Description = description;
	}
}
