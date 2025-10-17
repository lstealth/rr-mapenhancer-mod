using System.Globalization;

namespace InfinityCode.RealWorldTerrain;

public static class RealWorldTerrainCultureInfo
{
	public static CultureInfo cultureInfo => CultureInfo.InvariantCulture;

	public static NumberFormatInfo numberFormat => cultureInfo.NumberFormat;
}
