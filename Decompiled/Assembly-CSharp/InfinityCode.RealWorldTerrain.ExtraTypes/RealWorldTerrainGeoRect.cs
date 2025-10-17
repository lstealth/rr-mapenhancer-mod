namespace InfinityCode.RealWorldTerrain.ExtraTypes;

public class RealWorldTerrainGeoRect
{
	public double left;

	public double right;

	public double top;

	public double bottom;

	public RealWorldTerrainGeoRect()
	{
	}

	public RealWorldTerrainGeoRect(double left, double top, double right, double bottom)
	{
		this.left = left;
		this.top = top;
		this.right = right;
		this.bottom = bottom;
	}
}
