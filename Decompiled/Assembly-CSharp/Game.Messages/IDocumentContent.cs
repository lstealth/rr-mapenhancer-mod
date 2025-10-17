using MessagePack;

namespace Game.Messages;

[Union(1, typeof(SwitchList))]
public interface IDocumentContent
{
}
