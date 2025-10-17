using Model.Definition;

namespace UI.CarEditor;

internal class CarMetaProxy
{
	public ObjectMetadata Metadata { get; set; }

	public string Identifier { get; set; }

	public CarMetaProxy(string identifier, ObjectMetadata metadata)
	{
		Identifier = identifier;
		Metadata = metadata;
	}
}
