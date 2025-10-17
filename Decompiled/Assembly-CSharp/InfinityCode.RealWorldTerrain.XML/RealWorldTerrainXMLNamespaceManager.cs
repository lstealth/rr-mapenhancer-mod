using System.Xml;

namespace InfinityCode.RealWorldTerrain.XML;

public class RealWorldTerrainXMLNamespaceManager : XmlNamespaceManager
{
	public RealWorldTerrainXMLNamespaceManager(XmlNameTable table)
		: base(table)
	{
	}
}
