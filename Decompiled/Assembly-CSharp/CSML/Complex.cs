using System;
using System.Text.RegularExpressions;

namespace CSML;

public class Complex
{
	private double re;

	private double im;

	public double Re
	{
		get
		{
			return re;
		}
		set
		{
			re = value;
		}
	}

	public double Im
	{
		get
		{
			return im;
		}
		set
		{
			im = value;
		}
	}

	public static Complex I => new Complex(0.0, 1.0);

	public static Complex Zero => new Complex(0.0, 0.0);

	public static Complex One => new Complex(1.0, 0.0);

	public Complex()
	{
		Re = 0.0;
		Im = 0.0;
	}

	public Complex(double real_part)
	{
		Re = real_part;
		Im = 0.0;
	}

	public Complex(double real_part, double imaginary_part)
	{
		Re = real_part;
		Im = imaginary_part;
	}

	public Complex(string s)
	{
		throw new NotImplementedException();
	}

	public static Match Test(string s)
	{
		string text = "([0-9]+[.]?[0-9]*|[.][0-9]+)";
		string text2 = "[-]?" + text;
		return new Regex("^(?<RePart>(" + text2 + ")[-+](?<ImPart>(" + text + "))[i])$").Match(s);
	}

	public static Complex operator +(Complex a, Complex b)
	{
		return new Complex(a.Re + b.Re, a.Im + b.Im);
	}

	public static Complex operator +(Complex a, double b)
	{
		return new Complex(a.Re + b, a.Im);
	}

	public static Complex operator +(double a, Complex b)
	{
		return new Complex(a + b.Re, b.Im);
	}

	public static Complex operator -(Complex a, Complex b)
	{
		return new Complex(a.Re - b.Re, a.Im - b.Im);
	}

	public static Complex operator -(Complex a, double b)
	{
		return new Complex(a.Re - b, a.Im);
	}

	public static Complex operator -(double a, Complex b)
	{
		return new Complex(a - b.Re, 0.0 - b.Im);
	}

	public static Complex operator -(Complex a)
	{
		return new Complex(0.0 - a.Re, 0.0 - a.Im);
	}

	public static Complex operator *(Complex a, Complex b)
	{
		return new Complex(a.Re * b.Re - a.Im * b.Im, a.Im * b.Re + a.Re * b.Im);
	}

	public static Complex operator *(Complex a, double d)
	{
		return new Complex(d) * a;
	}

	public static Complex operator *(double d, Complex a)
	{
		return new Complex(d) * a;
	}

	public static Complex operator /(Complex a, Complex b)
	{
		return a * Conj(b) * (1.0 / (Abs(b) * Abs(b)));
	}

	public static Complex operator /(Complex a, double b)
	{
		return a * (1.0 / b);
	}

	public static Complex operator /(double a, Complex b)
	{
		return a * Conj(b) * (1.0 / (Abs(b) * Abs(b)));
	}

	public static bool operator ==(Complex a, Complex b)
	{
		if (a.Re == b.Re)
		{
			return a.Im == b.Im;
		}
		return false;
	}

	public static bool operator ==(Complex a, double b)
	{
		return a == new Complex(b);
	}

	public static bool operator ==(double a, Complex b)
	{
		return new Complex(a) == b;
	}

	public static bool operator !=(Complex a, Complex b)
	{
		return !(a == b);
	}

	public static bool operator !=(Complex a, double b)
	{
		return !(a == b);
	}

	public static bool operator !=(double a, Complex b)
	{
		return !(a == b);
	}

	public static double Abs(Complex a)
	{
		return Math.Sqrt(a.Im * a.Im + a.Re * a.Re);
	}

	public static Complex Inv(Complex a)
	{
		return new Complex(a.Re / (a.Re * a.Re + a.Im * a.Im), (0.0 - a.Im) / (a.Re * a.Re + a.Im * a.Im));
	}

	public static Complex Tan(Complex a)
	{
		return Sin(a) / Cos(a);
	}

	public static Complex Cosh(Complex a)
	{
		return (Exp(a) + Exp(-a)) / 2.0;
	}

	public static Complex Sinh(Complex a)
	{
		return (Exp(a) - Exp(-a)) / 2.0;
	}

	public static Complex Tanh(Complex a)
	{
		return (Exp(2.0 * a) - 1.0) / (Exp(2.0 * a) + 1.0);
	}

	public static Complex Coth(Complex a)
	{
		return (Exp(2.0 * a) + 1.0) / (Exp(2.0 * a) - 1.0);
	}

	public static Complex Sech(Complex a)
	{
		return Inv(Cosh(a));
	}

	public static Complex Csch(Complex a)
	{
		return Inv(Sinh(a));
	}

	public static Complex Cot(Complex a)
	{
		return Cos(a) / Sin(a);
	}

	public static Complex Conj(Complex a)
	{
		return new Complex(a.Re, 0.0 - a.Im);
	}

	public static Complex Sqrt(double d)
	{
		if (d >= 0.0)
		{
			return new Complex(Math.Sqrt(d));
		}
		return new Complex(0.0, Math.Sqrt(0.0 - d));
	}

	public static Complex Sqrt(Complex a)
	{
		return Pow(a, 0.5);
	}

	public static Complex Exp(Complex a)
	{
		return new Complex(Math.Exp(a.Re) * Math.Cos(a.Im), Math.Exp(a.Re) * Math.Sin(a.Im));
	}

	public static Complex Log(Complex a)
	{
		return new Complex(Math.Log(Abs(a)), Arg(a));
	}

	public static double Arg(Complex a)
	{
		if (a.Re < 0.0)
		{
			if (a.Im < 0.0)
			{
				return Math.Atan(a.Im / a.Re) - Math.PI;
			}
			return Math.PI - Math.Atan((0.0 - a.Im) / a.Re);
		}
		return Math.Atan(a.Im / a.Re);
	}

	public static Complex Cos(Complex a)
	{
		return 0.5 * (Exp(I * a) + Exp(-I * a));
	}

	public static Complex Sin(Complex a)
	{
		return (Exp(I * a) - Exp(-I * a)) / (2.0 * I);
	}

	public static Complex Pow(Complex a, Complex b)
	{
		return Exp(b * Log(a));
	}

	public static Complex Pow(double a, Complex b)
	{
		return Exp(b * Math.Log(a));
	}

	public static Complex Pow(Complex a, double b)
	{
		return Exp(b * Log(a));
	}

	public override string ToString()
	{
		if (this == Zero)
		{
			return "0";
		}
		string text = ((Im < 0.0) ? ((Re != 0.0) ? " - " : "-") : ((!(Im > 0.0) || Re == 0.0) ? "" : " + "));
		string text2 = ((Re != 0.0) ? Re.ToString() : "");
		string text3 = ((Im == 0.0) ? "" : ((Im != -1.0 && Im != 1.0) ? (Math.Abs(Im) + "i") : "i"));
		return text2 + text + text3;
	}

	public string ToString(string format)
	{
		if (this == Zero)
		{
			return "0";
		}
		if (double.IsInfinity(Re) || double.IsInfinity(Im))
		{
			return "oo";
		}
		if (double.IsNaN(Re) || double.IsNaN(Im))
		{
			return "?";
		}
		string text = Math.Abs(Im).ToString(format);
		string text2 = Re.ToString(format);
		string text3 = (text.StartsWith("-") ? ((!(text2 == "0")) ? " - " : "-") : ((!(text != "0") || !(text2 != "0")) ? "" : " + "));
		string text4 = ((text == "0") ? "" : ((!(text == "1")) ? (text + "i") : "i"));
		string text5 = ((!(text2 == "0")) ? text2 : ((!(text != "0")) ? "0" : ""));
		return text5 + text3 + text4;
	}

	public override bool Equals(object obj)
	{
		return obj.ToString() == ToString();
	}

	public override int GetHashCode()
	{
		return -1;
	}

	public bool IsReal()
	{
		return Im == 0.0;
	}

	public bool IsImaginary()
	{
		return Re == 0.0;
	}
}
