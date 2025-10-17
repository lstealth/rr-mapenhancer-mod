using System;
using System.Collections.Generic;
using InfinityCode.RealWorldTerrain.XML;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Utils;

public class RealWorldTerrainGPXObject
{
	public class Copyright
	{
		public string author;

		public int? year;

		public string license;

		public Copyright(string author)
		{
			this.author = author;
		}

		public Copyright(RealWorldTerrainXML node)
		{
			author = node.A("author");
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "year")
				{
					year = item.Value<int>();
				}
				else if (item.name == "license")
				{
					license = item.Value();
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			node.A("author", author);
			if (year.HasValue)
			{
				node.Create("year", year.Value);
			}
			if (!string.IsNullOrEmpty(license))
			{
				node.Create("license", license);
			}
		}
	}

	public class Bounds
	{
		private double _minlat;

		private double _minlon;

		private double _maxlat;

		private double _maxlon;

		public double minlat
		{
			get
			{
				return _minlat;
			}
			set
			{
				_minlat = RealWorldTerrainUtils.Clip(value, -90.0, 90.0);
			}
		}

		public double minlon
		{
			get
			{
				return _minlon;
			}
			set
			{
				_minlon = RealWorldTerrainUtils.Repeat(value, -180.0, 180.0);
			}
		}

		public double maxlat
		{
			get
			{
				return _maxlat;
			}
			set
			{
				_maxlat = RealWorldTerrainUtils.Clip(value, -90.0, 90.0);
			}
		}

		public double maxlon
		{
			get
			{
				return _maxlon;
			}
			set
			{
				_maxlon = RealWorldTerrainUtils.Repeat(value, -180.0, 180.0);
			}
		}

		public Bounds(RealWorldTerrainXML node)
		{
			minlat = node.A<double>("minlat");
			minlon = node.A<double>("minlon");
			maxlat = node.A<double>("maxlat");
			maxlon = node.A<double>("maxlon");
		}

		public Bounds(double minlon, double minlat, double maxlon, double maxlat)
		{
			this.minlat = minlat;
			this.minlon = minlon;
			this.maxlat = maxlat;
			this.maxlon = maxlon;
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			node.A("minlat", minlat);
			node.A("minlon", minlon);
			node.A("maxlat", maxlat);
			node.A("maxlon", maxlon);
		}
	}

	public class EMail
	{
		public string id;

		public string domain;

		public EMail(string id, string domain)
		{
			this.id = id;
			this.domain = domain;
		}

		public EMail(RealWorldTerrainXML node)
		{
			id = node.A("id");
			domain = node.A("domain");
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			node.A("id", id);
			node.A("domain", domain);
		}
	}

	public class Link
	{
		public string href;

		public string text;

		public string type;

		public Link(string href)
		{
			this.href = href;
		}

		public Link(RealWorldTerrainXML node)
		{
			href = node.A("href");
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "text")
				{
					text = item.Value();
				}
				else if (item.name == "type")
				{
					type = item.Value();
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			node.A("href", href);
			if (!string.IsNullOrEmpty(text))
			{
				node.Create("text", text);
			}
			if (!string.IsNullOrEmpty(type))
			{
				node.Create("type", type);
			}
		}
	}

	public class Meta
	{
		public string name;

		public string description;

		public Person author;

		public Copyright copyright;

		public List<Link> links;

		public DateTime? time;

		public string keywords;

		public Bounds bounds;

		public RealWorldTerrainXML extensions;

		public Meta()
		{
			links = new List<Link>();
		}

		public Meta(RealWorldTerrainXML node)
			: this()
		{
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "name")
				{
					name = item.Value();
				}
				else if (item.name == "desc")
				{
					description = item.Value();
				}
				else if (item.name == "author")
				{
					author = new Person(item);
				}
				else if (item.name == "copyright")
				{
					copyright = new Copyright(item);
				}
				else if (item.name == "link")
				{
					links.Add(new Link(item));
				}
				else if (item.name == "time")
				{
					time = DateTime.Parse(item.Value());
				}
				else if (item.name == "keywords")
				{
					keywords = item.Value();
				}
				else if (item.name == "bounds")
				{
					bounds = new Bounds(item);
				}
				else if (item.name == "extensions")
				{
					extensions = item;
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			if (!string.IsNullOrEmpty(name))
			{
				node.Create("name", name);
			}
			if (!string.IsNullOrEmpty(description))
			{
				node.Create("desc", description);
			}
			if (author != null)
			{
				author.AppendToNode(node);
			}
			if (copyright != null)
			{
				copyright.AppendToNode(node.Create("copyright"));
			}
			if (links != null && links.Count > 0)
			{
				foreach (Link link in links)
				{
					link.AppendToNode(node.Create("link"));
				}
			}
			if (time.HasValue)
			{
				node.Create("time", time.Value.ToUniversalTime().ToString("s") + "Z");
			}
			if (!string.IsNullOrEmpty(keywords))
			{
				node.Create("keywords", keywords);
			}
			if (bounds != null)
			{
				bounds.AppendToNode(node.Create("bounds"));
			}
			if (extensions != null)
			{
				node.AppendChild(extensions);
			}
		}
	}

	public class Person
	{
		public string name;

		public EMail email;

		public Link link;

		public Person()
		{
		}

		public Person(RealWorldTerrainXML node)
		{
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "name")
				{
					name = item.Value();
				}
				else if (item.name == "email")
				{
					email = new EMail(item);
				}
				else if (item.name == "link")
				{
					link = new Link(item);
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			if (!string.IsNullOrEmpty(name))
			{
				node.Create("name", name);
			}
			if (email != null)
			{
				email.AppendToNode(node.Create("email"));
			}
			if (link != null)
			{
				link.AppendToNode(node.Create("link"));
			}
		}
	}

	public class Route
	{
		public string name;

		public string comment;

		public string description;

		public string source;

		public List<Link> links;

		public uint? number;

		public string type;

		public List<Waypoint> points;

		public RealWorldTerrainXML extensions;

		public Route()
		{
			links = new List<Link>();
			points = new List<Waypoint>();
		}

		public Route(RealWorldTerrainXML node)
			: this()
		{
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "name")
				{
					name = item.Value();
				}
				else if (item.name == "cmt")
				{
					comment = item.Value();
				}
				else if (item.name == "desc")
				{
					description = item.Value();
				}
				else if (item.name == "src")
				{
					source = item.Value();
				}
				else if (item.name == "link")
				{
					links.Add(new Link(item));
				}
				else if (item.name == "number")
				{
					number = item.Value<uint>();
				}
				else if (item.name == "type")
				{
					type = item.Value();
				}
				else if (item.name == "rtept")
				{
					points.Add(new Waypoint(item));
				}
				else if (item.name == "extensions")
				{
					extensions = item;
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			if (!string.IsNullOrEmpty(name))
			{
				node.Create("name", name);
			}
			if (!string.IsNullOrEmpty(comment))
			{
				node.Create("cmt", comment);
			}
			if (!string.IsNullOrEmpty(description))
			{
				node.Create("desc", description);
			}
			if (!string.IsNullOrEmpty(source))
			{
				node.Create("src", source);
			}
			if (links != null)
			{
				foreach (Link link in links)
				{
					link.AppendToNode(node.Create("link"));
				}
			}
			if (number.HasValue)
			{
				node.Create("number", number.Value);
			}
			if (!string.IsNullOrEmpty(type))
			{
				node.Create("type", type);
			}
			foreach (Waypoint point in points)
			{
				point.AppendToNode(node.Create("rtept"));
			}
			if (extensions != null)
			{
				node.AppendChild(extensions);
			}
		}
	}

	public class Track
	{
		public string name;

		public string comment;

		public string description;

		public string source;

		public List<Link> links;

		public uint? number;

		public string type;

		public List<TrackSegment> segments;

		public RealWorldTerrainXML extensions;

		public Track()
		{
			links = new List<Link>();
			segments = new List<TrackSegment>();
		}

		public Track(RealWorldTerrainXML node)
			: this()
		{
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "name")
				{
					name = item.Value();
				}
				else if (item.name == "cmt")
				{
					comment = item.Value();
				}
				else if (item.name == "desc")
				{
					description = item.Value();
				}
				else if (item.name == "src")
				{
					source = item.Value();
				}
				else if (item.name == "link")
				{
					links.Add(new Link(item));
				}
				else if (item.name == "number")
				{
					number = item.Value<uint>();
				}
				else if (item.name == "type")
				{
					type = item.Value();
				}
				else if (item.name == "trkseg")
				{
					segments.Add(new TrackSegment(item));
				}
				else if (item.name == "extensions")
				{
					extensions = item;
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			if (!string.IsNullOrEmpty(name))
			{
				node.Create("name", name);
			}
			if (!string.IsNullOrEmpty(comment))
			{
				node.Create("cmt", comment);
			}
			if (!string.IsNullOrEmpty(description))
			{
				node.Create("desc", description);
			}
			if (!string.IsNullOrEmpty(source))
			{
				node.Create("src", source);
			}
			if (links != null)
			{
				foreach (Link link in links)
				{
					link.AppendToNode(node.Create("link"));
				}
			}
			if (number.HasValue)
			{
				node.Create("number", number.Value);
			}
			if (!string.IsNullOrEmpty(type))
			{
				node.Create("type", type);
			}
			foreach (TrackSegment segment in segments)
			{
				segment.AppendToNode(node.Create("trkseg"));
			}
			if (extensions != null)
			{
				node.AppendChild(extensions);
			}
		}
	}

	public class TrackSegment
	{
		public List<Waypoint> points;

		public RealWorldTerrainXML extensions;

		public TrackSegment()
		{
			points = new List<Waypoint>();
		}

		public TrackSegment(RealWorldTerrainXML node)
			: this()
		{
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "trkpt")
				{
					points.Add(new Waypoint(item));
				}
				else if (item.name == "extensions")
				{
					extensions = item;
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			foreach (Waypoint point in points)
			{
				point.AppendToNode(node.Create("trkpt"));
			}
			if (extensions != null)
			{
				node.AppendChild(extensions);
			}
		}
	}

	public class Waypoint
	{
		public double? elevation;

		public DateTime? time;

		public double? geoidheight;

		public string name;

		public string comment;

		public string description;

		public string source;

		public List<Link> links;

		public string symbol;

		public string type;

		public string fix;

		public uint? sat;

		public double? hdop;

		public double? vdop;

		public double? pdop;

		public double? ageofdgpsdata;

		public RealWorldTerrainXML extensions;

		private double _lat;

		private double _lon;

		private double? _magvar;

		private short? _dgpsid;

		public double lat
		{
			get
			{
				return _lat;
			}
			set
			{
				_lat = RealWorldTerrainUtils.Clip(value, -90.0, 90.0);
			}
		}

		public double lon
		{
			get
			{
				return _lon;
			}
			set
			{
				_lon = RealWorldTerrainUtils.Repeat(value, -180.0, 180.0);
			}
		}

		public double? magvar
		{
			get
			{
				return _magvar;
			}
			set
			{
				if (value.HasValue)
				{
					_magvar = RealWorldTerrainUtils.Clip(value.Value, 0.0, 360.0);
				}
				else
				{
					_magvar = null;
				}
			}
		}

		public short? dgpsid
		{
			get
			{
				return _dgpsid;
			}
			set
			{
				if (value.HasValue)
				{
					if (value.Value < 0)
					{
						_dgpsid = 0;
					}
					else if (value.Value > 1023)
					{
						_dgpsid = (short)1023;
					}
					else
					{
						_dgpsid = value.Value;
					}
				}
				else
				{
					_dgpsid = null;
				}
			}
		}

		public Waypoint(double lon, double lat)
		{
			links = new List<Link>();
			this.lat = lat;
			this.lon = lon;
		}

		public Waypoint(RealWorldTerrainXML node)
		{
			links = new List<Link>();
			lat = node.A<double>("lat");
			lon = node.A<double>("lon");
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "ele")
				{
					elevation = item.Value<double>();
				}
				else if (item.name == "time")
				{
					time = DateTime.Parse(item.Value());
				}
				else if (item.name == "magvar")
				{
					magvar = item.Value<double>();
				}
				else if (item.name == "geoidheight")
				{
					geoidheight = item.Value<double>();
				}
				else if (item.name == "name")
				{
					name = item.Value();
				}
				else if (item.name == "cmt")
				{
					comment = item.Value();
				}
				else if (item.name == "desc")
				{
					description = item.Value();
				}
				else if (item.name == "src")
				{
					source = item.Value();
				}
				else if (item.name == "link")
				{
					links.Add(new Link(item));
				}
				else if (item.name == "sym")
				{
					symbol = item.Value();
				}
				else if (item.name == "type")
				{
					type = item.Value();
				}
				else if (item.name == "fix")
				{
					fix = item.Value();
				}
				else if (item.name == "sat")
				{
					sat = item.Value<uint>();
				}
				else if (item.name == "hdop")
				{
					hdop = item.Value<double>();
				}
				else if (item.name == "vdop")
				{
					vdop = item.Value<double>();
				}
				else if (item.name == "pdop")
				{
					pdop = item.Value<double>();
				}
				else if (item.name == "ageofdgpsdata")
				{
					ageofdgpsdata = item.Value<double>();
				}
				else if (item.name == "dgpsid")
				{
					dgpsid = item.Value<short>();
				}
				else if (item.name == "extensions")
				{
					extensions = item;
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}

		public void AppendToNode(RealWorldTerrainXML node)
		{
			node.A("lat", lat);
			node.A("lon", lon);
			if (elevation.HasValue)
			{
				node.Create("ele", elevation.Value);
			}
			if (time.HasValue)
			{
				node.Create("time", time.Value.ToUniversalTime().ToString("s") + "Z");
			}
			if (magvar.HasValue)
			{
				node.Create("magvar", magvar.Value);
			}
			if (geoidheight.HasValue)
			{
				node.Create("geoidheight", geoidheight.Value);
			}
			if (!string.IsNullOrEmpty(name))
			{
				node.Create("name", name);
			}
			if (!string.IsNullOrEmpty(comment))
			{
				node.Create("cmt", comment);
			}
			if (!string.IsNullOrEmpty(description))
			{
				node.Create("desc", description);
			}
			if (!string.IsNullOrEmpty(source))
			{
				node.Create("src", source);
			}
			if (links != null)
			{
				foreach (Link link in links)
				{
					link.AppendToNode(node.Create("link"));
				}
			}
			if (!string.IsNullOrEmpty(symbol))
			{
				node.Create("sym", symbol);
			}
			if (!string.IsNullOrEmpty(type))
			{
				node.Create("type", type);
			}
			if (!string.IsNullOrEmpty(fix))
			{
				node.Create("fix", fix);
			}
			if (sat.HasValue)
			{
				node.Create("sat", sat.Value);
			}
			if (hdop.HasValue)
			{
				node.Create("hdop", hdop.Value);
			}
			if (vdop.HasValue)
			{
				node.Create("vdop", vdop.Value);
			}
			if (pdop.HasValue)
			{
				node.Create("pdop", pdop.Value);
			}
			if (ageofdgpsdata.HasValue)
			{
				node.Create("ageofdgpsdata", ageofdgpsdata.Value);
			}
			if (dgpsid.HasValue)
			{
				node.Create("dgpsid", dgpsid.Value);
			}
			if (extensions != null)
			{
				node.AppendChild(extensions);
			}
		}
	}

	public string version = "1.1";

	public string creator = "RealWorldTerrain";

	public Meta metadata;

	public List<Waypoint> waypoints;

	public List<Route> routes;

	public List<Track> tracks;

	public RealWorldTerrainXML extensions;

	private RealWorldTerrainGPXObject()
	{
		waypoints = new List<Waypoint>();
		routes = new List<Route>();
		tracks = new List<Track>();
	}

	public RealWorldTerrainGPXObject(string creator, string version = "1.1")
		: this()
	{
		this.creator = creator;
		this.version = version;
	}

	public static RealWorldTerrainGPXObject Load(string content)
	{
		RealWorldTerrainGPXObject realWorldTerrainGPXObject = new RealWorldTerrainGPXObject();
		try
		{
			RealWorldTerrainXML realWorldTerrainXML = RealWorldTerrainXML.Load(content);
			realWorldTerrainGPXObject.version = realWorldTerrainXML.A("version");
			realWorldTerrainGPXObject.creator = realWorldTerrainXML.A("creator");
			foreach (RealWorldTerrainXML item in realWorldTerrainXML)
			{
				if (item.name == "wpt")
				{
					realWorldTerrainGPXObject.waypoints.Add(new Waypoint(item));
				}
				else if (item.name == "rte")
				{
					realWorldTerrainGPXObject.routes.Add(new Route(item));
				}
				else if (item.name == "trk")
				{
					realWorldTerrainGPXObject.tracks.Add(new Track(item));
				}
				else if (item.name == "metadata")
				{
					realWorldTerrainGPXObject.metadata = new Meta(item);
				}
				else if (item.name == "extensions")
				{
					realWorldTerrainGPXObject.extensions = item;
				}
				else
				{
					Debug.Log(item.name);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);
		}
		return realWorldTerrainGPXObject;
	}

	public RealWorldTerrainXML ToXML()
	{
		RealWorldTerrainXML realWorldTerrainXML = new RealWorldTerrainXML("gpx");
		realWorldTerrainXML.A("version", version);
		realWorldTerrainXML.A("creator", creator);
		if (metadata != null)
		{
			metadata.AppendToNode(realWorldTerrainXML.Create("metadata"));
		}
		if (waypoints != null)
		{
			foreach (Waypoint waypoint in waypoints)
			{
				waypoint.AppendToNode(realWorldTerrainXML.Create("wpt"));
			}
		}
		if (routes != null)
		{
			foreach (Route route in routes)
			{
				route.AppendToNode(realWorldTerrainXML.Create("rte"));
			}
		}
		if (tracks != null)
		{
			foreach (Track track in tracks)
			{
				track.AppendToNode(realWorldTerrainXML.Create("trk"));
			}
		}
		if (extensions != null)
		{
			realWorldTerrainXML.AppendChild(extensions);
		}
		return realWorldTerrainXML;
	}

	public override string ToString()
	{
		return ToXML().outerXml;
	}
}
