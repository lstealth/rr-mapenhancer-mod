using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace InfinityCode.RealWorldTerrain.OSM;

public class RealWorldTerrainOSMWay : RealWorldTerrainOSMBase
{
	public List<string> nodeRefs;

	public List<RealWorldTerrainOSMWay> holes;

	public RealWorldTerrainOSMWay()
	{
	}

	public RealWorldTerrainOSMWay(BinaryReader br)
	{
		id = br.ReadInt64().ToString();
		nodeRefs = new List<string>();
		tags = new List<RealWorldTerrainOSMTag>();
		int num = br.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			nodeRefs.Add(br.ReadInt64().ToString());
		}
		int num2 = br.ReadInt32();
		for (int j = 0; j < num2; j++)
		{
			tags.Add(new RealWorldTerrainOSMTag(br));
		}
	}

	public RealWorldTerrainOSMWay(XmlNode node)
	{
		id = node.Attributes["id"].Value;
		nodeRefs = new List<string>();
		tags = new List<RealWorldTerrainOSMTag>();
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.Name == "nd")
			{
				nodeRefs.Add(childNode.Attributes["ref"].Value);
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
		bw.Write(nodeRefs.Count);
		foreach (string nodeRef in nodeRefs)
		{
			bw.Write(long.Parse(nodeRef));
		}
		bw.Write(tags.Count);
		foreach (RealWorldTerrainOSMTag tag in tags)
		{
			tag.Write(bw);
		}
	}
}
