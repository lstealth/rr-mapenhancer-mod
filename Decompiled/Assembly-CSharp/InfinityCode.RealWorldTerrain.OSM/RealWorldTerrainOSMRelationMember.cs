using System.IO;
using System.Xml;

namespace InfinityCode.RealWorldTerrain.OSM;

public class RealWorldTerrainOSMRelationMember
{
	public readonly string reference;

	public readonly string role;

	public readonly string type;

	public RealWorldTerrainOSMRelationMember(BinaryReader br)
	{
		type = br.ReadString();
		reference = br.ReadInt64().ToString();
		role = br.ReadString();
	}

	public RealWorldTerrainOSMRelationMember(XmlNode node)
	{
		type = node.Attributes["type"].Value;
		reference = node.Attributes["ref"].Value;
		role = node.Attributes["role"].Value;
	}

	public void Write(BinaryWriter bw)
	{
		bw.Write(type);
		bw.Write(long.Parse(reference));
		bw.Write(role);
	}
}
