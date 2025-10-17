using Game;
using Game.State;
using Helpers;
using Model;
using Model.Ops;
using UnityEngine;

public readonly struct Hyperlink
{
	public readonly string Address;

	public readonly string Text;

	public Hyperlink(string address, string text)
	{
		Address = address;
		Text = text;
	}

	public override string ToString()
	{
		return "<link=\"" + Address + "\"><style=ConsoleLink><noparse>" + Text + "</noparse></style></link>";
	}

	public static implicit operator string(Hyperlink v)
	{
		return v.ToString();
	}

	public static Hyperlink To(IPlayer player)
	{
		return new Hyperlink($"player:{player.PlayerId}", player.Name);
	}

	public static Hyperlink To(PlayerId playerId)
	{
		string text = StateManager.Shared.PlayersManager.NameForPlayerId(playerId);
		return new Hyperlink($"player:{playerId}", text);
	}

	public static Hyperlink To(Industry industry, string name = null)
	{
		return new Hyperlink("industry:" + industry.identifier, name ?? industry.name);
	}

	public static Hyperlink To(Car car)
	{
		return new Hyperlink("car:" + car.id, car.DisplayName);
	}

	public static Hyperlink To(Car car, string name)
	{
		return new Hyperlink("car:" + car.id, name ?? car.DisplayName);
	}

	public static Hyperlink To(IOpsCar car)
	{
		return new Hyperlink("car:" + car.Id, car.DisplayName);
	}

	public static Hyperlink To(Transform transform, string text)
	{
		Vector3 vector = transform.GamePosition();
		EntityReference entityReference = new EntityReference(EntityType.Position, new Vector4(vector.x, vector.y, vector.z, transform.rotation.eulerAngles.y));
		return new Hyperlink(entityReference.URI(), text);
	}

	public static Hyperlink To(EntityReference r)
	{
		string address = r.URI();
		string text = r.Text();
		return new Hyperlink(address, text);
	}

	public static Hyperlink To(PassengerStop passengerStop)
	{
		return To(passengerStop.GetComponentInParent<Industry>());
	}
}
