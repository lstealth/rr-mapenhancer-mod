using System.Globalization;
using System.Threading;

namespace Helpers;

public abstract class CurrencySymbolHelper
{
	public static void SetCurrencySymbol(string symbol)
	{
		CultureInfo cultureInfo = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
		NumberFormatInfo numberFormatInfo = (NumberFormatInfo)cultureInfo.NumberFormat.Clone();
		numberFormatInfo.CurrencySymbol = symbol;
		cultureInfo.NumberFormat = numberFormatInfo;
		Thread.CurrentThread.CurrentCulture = cultureInfo;
	}
}
