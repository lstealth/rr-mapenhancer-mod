using System.IO;
using System.Xml;

namespace InfinityCode.RealWorldTerrain.OSM;

public class RealWorldTerrainOSMTag
{
	public readonly string key;

	public readonly string value;

	public RealWorldTerrainOSMTag(BinaryReader br)
	{
		key = br.ReadString();
		value = br.ReadString();
	}

	public RealWorldTerrainOSMTag(XmlNode node)
	{
		key = node.Attributes["k"].Value;
		value = node.Attributes["v"].Value;
	}

	public void Write(BinaryWriter bw)
	{
		bw.Write(key);
		bw.Write(value);
	}
}
