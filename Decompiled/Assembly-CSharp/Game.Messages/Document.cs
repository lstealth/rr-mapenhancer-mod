using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct Document
{
	public DocumentKind Kind;

	public IDocumentContent Content;
}
