namespace InfinityCode.RealWorldTerrain;

public enum RealWorldTerrainRoadType
{
	motorway = 1,
	trunk = 2,
	primary = 4,
	secondary = 8,
	tertiary = 0x10,
	unclassified = 0x20,
	residential = 0x40,
	service = 0x80,
	motorway_link = 0x100,
	trunk_link = 0x200,
	primary_link = 0x400,
	secondary_link = 0x800,
	tertiary_link = 0x1000,
	living_street = 0x2000,
	pedestrian = 0x4000,
	track = 0x8000,
	bus_guideway = 0x10000,
	raceway = 0x20000,
	road = 0x40000,
	footway = 0x80000,
	cycleway = 0x100000,
	bridleway = 0x200000,
	steps = 0x400000,
	path = 0x800000
}
