using System;
using System.Text.RegularExpressions;

namespace InfinityCode.RealWorldTerrain;

public static class RealWorldTerrainUTM
{
	private const double flattening = 298.257223563;

	private const double equatorialRadius = 6378137.0;

	private static double DegToRad(double deg)
	{
		double num = Math.PI;
		return deg / 180.0 * num;
	}

	private static double FootpointLatitude(double y)
	{
		double num = 6367444.65712259 * (1.0 + Math.Pow(0.0016792203863837205, 2.0) / 4.0 + Math.Pow(0.0016792203863837205, 4.0) / 64.0);
		double num2 = y / num;
		double num3 = 0.002518830579575581 + -27.0 * Math.Pow(0.0016792203863837205, 3.0) / 32.0 + 269.0 * Math.Pow(0.0016792203863837205, 5.0) / 512.0;
		double num4 = 21.0 * Math.Pow(0.0016792203863837205, 2.0) / 16.0 + -55.0 * Math.Pow(0.0016792203863837205, 4.0) / 32.0;
		double num5 = 151.0 * Math.Pow(0.0016792203863837205, 3.0) / 96.0 + -417.0 * Math.Pow(0.0016792203863837205, 5.0) / 128.0;
		double num6 = 1097.0 * Math.Pow(0.0016792203863837205, 4.0) / 512.0;
		return num2 + num3 * Math.Sin(2.0 * num2) + num4 * Math.Sin(4.0 * num2) + num5 * Math.Sin(6.0 * num2) + num6 * Math.Sin(8.0 * num2);
	}

	private static void Geodetic_To_UPS(double lng, double lat, out string latZone, out int lngZone, out double easting, out double northing)
	{
		if (Math.Abs(lng + 180.0) < double.Epsilon)
		{
			lng = 180.0;
		}
		double value = lat * Math.PI / 180.0;
		double num = lng * Math.PI / 180.0;
		double num2 = Math.Sqrt(0.006705621329494961 - Math.Pow(0.0033528106647474805, 2.0));
		double num3 = 12799187.251516987 * Math.Pow((1.0 - num2) / (1.0 + num2), num2 / 2.0) * Math.Tan(Math.PI / 4.0 - Math.Abs(value) / 2.0) * Math.Pow((1.0 + num2 * Math.Sin(Math.Abs(value))) / (1.0 - num2 * Math.Sin(Math.Abs(value))), num2 / 2.0);
		double num4 = 2000000.0 + 0.994 * num3 * Math.Sin(num);
		double num5 = ((lat <= 0.0) ? (2000000.0 + 0.994 * num3 * Math.Cos(num)) : (2000000.0 - 0.994 * num3 * Math.Cos(num)));
		string text = ((!(lat < 0.0)) ? ((lng >= 0.0 || lng == -180.0 || lat == 90.0) ? "Z" : "Y") : ((lng >= 0.0 || lng == -180.0 || lat == -90.0) ? "B" : "A"));
		easting = num4;
		northing = num5;
		lngZone = 0;
		latZone = text;
	}

	private static double RadToDeg(double rad)
	{
		return rad / Math.PI * 180.0;
	}

	public static void ToLngLat(string latZone, int lngZone, double easting, double northing, out double lng, out double lat)
	{
		if (new Regex("[AaBbYyZz]").IsMatch(latZone))
		{
			UPS_To_Geodetic(latZone, easting, northing, out lng, out lat);
			return;
		}
		bool num = new Regex("[CcDdEeFfGgHhJjKkLlMm]").IsMatch(latZone);
		double x = (easting - 500000.0) / 0.9996;
		if (num)
		{
			northing -= 10000000.0;
		}
		UTMtoLatLong(x, northing / 0.9996, UTMCentralMeridian(lngZone), out lng, out lat);
	}

	public static void ToUTM(double lng, double lat, out string latZone, out int lngZone, out double easting, out double northing)
	{
		if (lat < -80.0 || lat > 84.0)
		{
			Geodetic_To_UPS(lng, lat, out latZone, out lngZone, out easting, out northing);
			return;
		}
		int num = (int)Math.Floor(lng / 6.0 + 31.0);
		string text = ((!(lat >= -72.0)) ? "C" : ((!(lat >= -64.0)) ? "D" : ((!(lat >= -56.0)) ? "E" : ((!(lat >= -48.0)) ? "F" : ((!(lat >= -40.0)) ? "G" : ((!(lat >= -32.0)) ? "H" : ((!(lat >= -24.0)) ? "J" : ((!(lat >= -16.0)) ? "K" : ((!(lat >= -8.0)) ? "L" : ((!(lat >= 0.0)) ? "M" : ((!(lat >= 8.0)) ? "N" : ((!(lat >= 16.0)) ? "P" : ((!(lat >= 24.0)) ? "Q" : ((!(lat >= 32.0)) ? "R" : ((!(lat >= 40.0)) ? "S" : ((!(lat >= 48.0)) ? "T" : ((!(lat >= 56.0)) ? "U" : ((!(lat >= 64.0)) ? "V" : ((lat >= 72.0) ? "X" : "W")))))))))))))))))));
		double num2 = Math.Sqrt(1.0 - Math.Pow(6356752.314245179, 2.0) / Math.Pow(6378137.0, 2.0));
		double num3 = lat * (Math.PI / 180.0);
		double num4 = 3.0 + 6.0 * (1.0 + Math.Floor((lng + 180.0) / 6.0) - 1.0) - 180.0;
		double num5 = num2 * num2 / (1.0 - Math.Pow(num2, 2.0));
		double num6 = 6378137.0 / Math.Sqrt(1.0 - Math.Pow(num2 * Math.Sin(num3), 2.0));
		double num7 = Math.Pow(Math.Tan(num3), 2.0);
		double num8 = num5 * Math.Pow(Math.Cos(num3), 2.0);
		double num9 = (lng - num4) * (Math.PI / 180.0) * Math.Cos(num3);
		double num10 = (num3 * 0.9983242984527952 - Math.Sin(2.0 * num3) * 0.00251460706051871 + Math.Sin(4.0 * num3) * 2.6390465943376327E-06 - Math.Sin(6.0 * num3) * 3.418046086595819E-09) * 6378137.0;
		double num11 = 0.9996 * num6 * num9 * (1.0 + num9 * num9 * ((1.0 - num7 + num8) / 6.0 + num9 * num9 * (5.0 - 18.0 * num7 + num7 * num7 + 72.0 * num8 - 58.0 * num5) / 120.0)) + 500000.0;
		double num12 = 0.9996 * (num10 + num6 * Math.Tan(num3) * (num9 * num9 * (0.5 + num9 * num9 * ((5.0 - num7 + 9.0 * num8 + 4.0 * num8 * num8) / 24.0 + num9 * num9 * (61.0 - 58.0 * num7 + num7 * num7 + 600.0 * num8 - 330.0 * num5) / 720.0))));
		latZone = text;
		lngZone = ((num == 61) ? 1 : num);
		easting = num11;
		northing = ((num12 < 0.0) ? (10000000.0 + num12) : num12);
	}

	private static void UPS_To_Geodetic(string latZone, double easting, double northing, out double lng, out double lat)
	{
		double num = Math.Sqrt(0.006705621329494961 - Math.Pow(0.0033528106647474805, 2.0));
		double num2 = (easting - 2000000.0) / 0.994;
		double num3 = (northing - 2000000.0) / 0.994;
		double num4 = num3;
		if (num3 == 0.0)
		{
			num3 = 1.0;
		}
		bool flag = latZone.ToUpper() != "Z" && latZone.ToUpper() != "Y";
		double num5;
		double d;
		if (flag)
		{
			num5 = Math.PI + Math.Atan(num2 / num4);
			d = Math.PI + Math.Atan(num2 / num3);
		}
		else
		{
			num5 = Math.PI - Math.Atan(num2 / num4);
			d = Math.PI - Math.Atan(num2 / num3);
		}
		double num6 = 2.0 * Math.Pow(6378137.0, 2.0) / 6356752.314245179 * Math.Pow((1.0 - num) / (1.0 + num), num / 2.0);
		double num7 = Math.Abs(num3);
		double num8 = Math.Abs(Math.Cos(d));
		double num9 = num6 * num8;
		double num10 = Math.Log(num7 / num9) / Math.Log(Math.E) * -1.0;
		double num11 = 2.0 * Math.Atan(Math.Pow(Math.E, num10)) - Math.PI / 2.0;
		double num12 = 0.0;
		while (Math.Abs(num11 - num12) > 1E-07 && !double.IsInfinity(num11))
		{
			num12 = num11;
			double d2 = (1.0 + Math.Sin(num11)) / (1.0 - Math.Sin(num11)) * Math.Pow((1.0 - num * Math.Sin(num11)) / (1.0 + num * Math.Sin(num11)), num);
			double num13 = 0.0 - num10 + 0.5 * Math.Log(d2);
			double num14 = (1.0 - Math.Pow(num, 2.0)) / ((1.0 - Math.Pow(num, 2.0) * Math.Pow(Math.Sin(num11), 2.0)) * Math.Cos(num11));
			num11 -= num13 / num14;
		}
		if (!double.IsInfinity(num11))
		{
			num12 = num11;
		}
		lat = ((!double.IsNaN(num12)) ? (num12 * (180.0 / Math.PI)) : 90.0);
		if (flag)
		{
			lat *= -1.0;
		}
		lng = ((!double.IsNaN(num5)) ? (num5 * (180.0 / Math.PI)) : 0.0);
		if (easting < 2000000.0)
		{
			lng = (180.0 - lng % 180.0) * -1.0;
		}
		else if (lng > 180.0)
		{
			lng -= 180.0;
		}
		else if (lng < -180.0)
		{
			lng += 180.0;
		}
		if ((((!(northing < 2000000.0) && num2 == 0.0) ? 1u : 0u) & (uint)(flag ? 1 : 0)) != 0)
		{
			lng = 0.0;
		}
		if (northing < 2000000.0 && num2 == 0.0 && !flag)
		{
			lng = 0.0;
		}
	}

	private static double UTMCentralMeridian(double zone)
	{
		return DegToRad(zone * 6.0 - 183.0);
	}

	private static void UTMtoLatLong(double x, double y, double zone, out double lng, out double lat)
	{
		double num = FootpointLatitude(y);
		double num2 = Math.Cos(num);
		double num3 = Math.Pow(num2, 2.0);
		double num4 = 0.006739496742276434 * num3;
		double num5 = Math.Pow(6378137.0, 2.0) / (6356752.314245179 * Math.Sqrt(num4 + 1.0));
		double num6 = num5;
		double num7 = Math.Tan(num);
		double num8 = num7 * num7;
		double num9 = num8 * num8;
		double num10 = 1.0 / (num6 * num2);
		double num11 = num6 * num5;
		double num12 = num7 / (2.0 * num11);
		double num13 = num11 * num5;
		double num14 = 1.0 / (6.0 * num13 * num2);
		double num15 = num13 * num5;
		double num16 = num7 / (24.0 * num15);
		double num17 = num15 * num5;
		double num18 = 1.0 / (120.0 * num17 * num2);
		double num19 = num17 * num5;
		double num20 = num7 / (720.0 * num19);
		double num21 = num19 * num5;
		double num22 = 1.0 / (5040.0 * num21 * num2);
		double num23 = num7 / (40320.0 * (num21 * num5));
		double num24 = -1.0 - num4;
		double num25 = -1.0 - 2.0 * num8 - num4;
		double num26 = 5.0 + 3.0 * num8 + 6.0 * num4 - 6.0 * num8 * num4 - 3.0 * (num4 * num4) - 9.0 * num8 * (num4 * num4);
		double num27 = 5.0 + 28.0 * num8 + 24.0 * num9 + 6.0 * num4 + 8.0 * num8 * num4;
		double num28 = -61.0 - 90.0 * num8 - 45.0 * num9 - 107.0 * num4 + 162.0 * num8 * num4;
		double num29 = -61.0 - 662.0 * num8 - 1320.0 * num9 - 720.0 * (num9 * num8);
		double num30 = 1385.0 + 3633.0 * num8 + 4095.0 * num9 + 1575.0 * (num9 * num8);
		double rad = num + num12 * num24 * (x * x) + num16 * num26 * Math.Pow(x, 4.0) + num20 * num28 * Math.Pow(x, 6.0) + num23 * num30 * Math.Pow(x, 8.0);
		double rad2 = zone + num10 * x + num14 * num25 * Math.Pow(x, 3.0) + num18 * num27 * Math.Pow(x, 5.0) + num22 * num29 * Math.Pow(x, 7.0);
		lat = RadToDeg(rad);
		lng = RadToDeg(rad2);
		if (lat > 90.0)
		{
			lat = 90.0;
		}
		else if (lat < -90.0)
		{
			lat = -90.0;
		}
		if (lng > 180.0)
		{
			lng = 180.0;
		}
		else if (lng < -180.0)
		{
			lng = -180.0;
		}
	}
}
