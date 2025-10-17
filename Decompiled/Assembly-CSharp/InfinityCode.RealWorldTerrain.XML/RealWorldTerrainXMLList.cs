using System.Collections;
using System.Xml;

namespace InfinityCode.RealWorldTerrain.XML;

public class RealWorldTerrainXMLList : IEnumerable
{
	private readonly XmlNodeList _list;

	public int count
	{
		get
		{
			if (_list == null)
			{
				return 0;
			}
			return _list.Count;
		}
	}

	public XmlNodeList list => _list;

	public RealWorldTerrainXML this[int index]
	{
		get
		{
			if (_list == null || index < 0 || index >= _list.Count)
			{
				return new RealWorldTerrainXML();
			}
			return new RealWorldTerrainXML(_list[index] as XmlElement);
		}
	}

	public RealWorldTerrainXMLList()
	{
	}

	public RealWorldTerrainXMLList(XmlNodeList list)
	{
		_list = list;
	}

	public IEnumerator GetEnumerator()
	{
		for (int i = 0; i < count; i++)
		{
			yield return this[i];
		}
	}
}
