using System;

namespace GalaSoft.MvvmLight.Messaging;

public interface IMessenger
{
	void Register<TMessage>(object recipient, Action<TMessage> action);

	void Register<TMessage>(object recipient, bool receiveDerivedMessagesToo, Action<TMessage> action);

	void Send<TMessage>(TMessage message);

	void Send<TMessage, TTarget>(TMessage message);

	void Unregister(object recipient);

	void Unregister<TMessage>(object recipient);

	void Unregister<TMessage>(object recipient, Action<TMessage> action);
}
