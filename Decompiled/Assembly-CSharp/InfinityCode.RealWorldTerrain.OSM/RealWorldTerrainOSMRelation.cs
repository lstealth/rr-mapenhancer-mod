using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace InfinityCode.RealWorldTerrain.OSM;

public class RealWorldTerrainOSMRelation : RealWorldTerrainOSMBase
{
	public readonly List<RealWorldTerrainOSMRelationMember> members;

	public RealWorldTerrainOSMRelation(BinaryReader br)
	{
		id = br.ReadInt64().ToString();
		members = new List<RealWorldTerrainOSMRelationMember>();
		tags = new List<RealWorldTerrainOSMTag>();
		int num = br.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			members.Add(new RealWorldTerrainOSMRelationMember(br));
		}
		int num2 = br.ReadInt32();
		for (int j = 0; j < num2; j++)
		{
			tags.Add(new RealWorldTerrainOSMTag(br));
		}
	}

	public RealWorldTerrainOSMRelation(XmlNode node)
	{
		id = node.Attributes["id"].Value;
		members = new List<RealWorldTerrainOSMRelationMember>();
		tags = new List<RealWorldTerrainOSMTag>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "member")
			{
				members.Add(new RealWorldTerrainOSMRelationMember(childNode));
			}
			else if (childNode.Name == "tag")
			{
				tags.Add(new RealWorldTerrainOSMTag(childNode));
			}
		}
	}

	public void Write(BinaryWriter bw)
	{
		bw.Write(long.Parse(id));
		bw.Write(members.Count);
		foreach (RealWorldTerrainOSMRelationMember member in members)
		{
			member.Write(bw);
		}
		bw.Write(tags.Count);
		foreach (RealWorldTerrainOSMTag tag in tags)
		{
			tag.Write(bw);
		}
	}
}
