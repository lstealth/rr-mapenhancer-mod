using System;
using System.Collections;

namespace CSML;

public class Matrix
{
	public enum DefinitenessType
	{
		PositiveDefinite,
		PositiveSemidefinite,
		NegativeDefinite,
		NegativeSemidefinite,
		Indefinite
	}

	private ArrayList Values;

	private int rowCount;

	private int columnCount;

	public int RowCount => rowCount;

	public int ColumnCount => columnCount;

	public virtual Complex this[int i, int j]
	{
		get
		{
			if (i > 0 && i <= rowCount && j > 0 && j <= columnCount)
			{
				return (Complex)((ArrayList)Values[i - 1])[j - 1];
			}
			throw new ArgumentOutOfRangeException("Indices must not exceed size of matrix.");
		}
		set
		{
			if (i <= 0 || j <= 0)
			{
				throw new ArgumentOutOfRangeException("Indices must be real positive.");
			}
			if (i > rowCount)
			{
				for (int k = 0; k < i - rowCount; k++)
				{
					Values.Add(new ArrayList(columnCount));
					for (int l = 0; l < columnCount; l++)
					{
						((ArrayList)Values[rowCount + k]).Add(Complex.Zero);
					}
				}
				rowCount = i;
			}
			if (j > columnCount)
			{
				for (int m = 0; m < rowCount; m++)
				{
					for (int n = 0; n < j - columnCount; n++)
					{
						((ArrayList)Values[m]).Add(Complex.Zero);
					}
				}
				columnCount = j;
			}
			((ArrayList)Values[i - 1])[j - 1] = value;
		}
	}

	public virtual Complex this[int i]
	{
		get
		{
			if (RowCount == 1)
			{
				return (Complex)((ArrayList)Values[0])[i - 1];
			}
			if (ColumnCount == 1)
			{
				return (Complex)((ArrayList)Values[i - 1])[0];
			}
			throw new InvalidOperationException("General matrix acces requires double indexing.");
		}
		set
		{
			if (rowCount == 1)
			{
				if (i > columnCount)
				{
					for (int j = 0; j < i - columnCount; j++)
					{
						((ArrayList)Values[0]).Add(Complex.Zero);
					}
					columnCount = i;
				}
				((ArrayList)Values[0])[i - 1] = value;
				return;
			}
			if (columnCount == 1)
			{
				if (i > rowCount)
				{
					for (int k = 0; k < i - rowCount; k++)
					{
						Values.Add(new ArrayList(columnCount));
						((ArrayList)Values[rowCount + k]).Add(Complex.Zero);
					}
					rowCount = i;
				}
				((ArrayList)Values[i - 1])[0] = value;
				return;
			}
			throw new InvalidOperationException("Cannot access multidimensional matrix via single index.");
		}
	}

	public Matrix()
	{
		Values = new ArrayList();
		rowCount = 0;
		columnCount = 0;
	}

	public Matrix(int m, int n)
	{
		rowCount = m;
		columnCount = n;
		Values = new ArrayList(m);
		for (int i = 0; i < m; i++)
		{
			Values.Add(new ArrayList(n));
			for (int j = 0; j < n; j++)
			{
				((ArrayList)Values[i]).Add(Complex.Zero);
			}
		}
	}

	public Matrix(int n)
	{
		rowCount = n;
		columnCount = n;
		Values = new ArrayList(n);
		for (int i = 0; i < n; i++)
		{
			Values.Add(new ArrayList(n));
			for (int j = 0; j < n; j++)
			{
				((ArrayList)Values[i]).Add(Complex.Zero);
			}
		}
	}

	public Matrix(Complex x)
	{
		rowCount = 1;
		columnCount = 1;
		Values = new ArrayList(1);
		Values.Add(new ArrayList(1));
		((ArrayList)Values[0]).Add(x);
	}

	public Matrix(Complex[,] values)
	{
		if (values == null)
		{
			Values = new ArrayList();
			columnCount = 0;
			rowCount = 0;
		}
		rowCount = (int)values.GetLongLength(0);
		columnCount = (int)values.GetLongLength(1);
		Values = new ArrayList(rowCount);
		for (int i = 0; i < rowCount; i++)
		{
			Values.Add(new ArrayList(columnCount));
			for (int j = 0; j < columnCount; j++)
			{
				((ArrayList)Values[i]).Add(values[i, j]);
			}
		}
	}

	public Matrix(Complex[] values)
	{
		if (values == null)
		{
			Values = new ArrayList();
			columnCount = 0;
			rowCount = 0;
		}
		rowCount = values.Length;
		columnCount = 1;
		Values = new ArrayList(rowCount);
		for (int i = 0; i < rowCount; i++)
		{
			Values.Add(new ArrayList(1));
			((ArrayList)Values[i]).Add(values[i]);
		}
	}

	public Matrix(double x)
	{
		rowCount = 1;
		columnCount = 1;
		Values = new ArrayList(1);
		Values.Add(new ArrayList(1));
		((ArrayList)Values[0]).Add(new Complex(x));
	}

	public Matrix(double[,] values)
	{
		if (values == null)
		{
			Values = new ArrayList();
			columnCount = 0;
			rowCount = 0;
		}
		rowCount = (int)values.GetLongLength(0);
		columnCount = (int)values.GetLongLength(1);
		Values = new ArrayList(rowCount);
		for (int i = 0; i < rowCount; i++)
		{
			Values.Add(new ArrayList(columnCount));
			for (int j = 0; j < columnCount; j++)
			{
				((ArrayList)Values[i]).Add(new Complex(values[i, j]));
			}
		}
	}

	public Matrix(double[] values)
	{
		if (values == null)
		{
			Values = new ArrayList();
			columnCount = 0;
			rowCount = 0;
		}
		rowCount = values.Length;
		columnCount = 1;
		Values = new ArrayList(rowCount);
		for (int i = 0; i < rowCount; i++)
		{
			Values.Add(new ArrayList(1));
			((ArrayList)Values[i]).Add(new Complex(values[i]));
		}
	}

	public Matrix(string matrix_string)
	{
		matrix_string = matrix_string.Replace(" ", "");
		string[] array = matrix_string.Split(new char[1] { ';' });
		rowCount = array.Length;
		Values = new ArrayList(rowCount);
		columnCount = 0;
		for (int i = 0; i < rowCount; i++)
		{
			Values.Add(new ArrayList());
		}
		for (int j = 1; j <= rowCount; j++)
		{
			string[] array2 = array[j - 1].Split(new char[1] { ',' });
			for (int k = 1; k <= array2.Length; k++)
			{
				this[j, k] = new Complex(Convert.ToDouble(array2[k - 1]));
			}
		}
	}

	public static Matrix E(int n, int j)
	{
		Matrix matrix = Zeros(n, 1);
		matrix[j] = Complex.One;
		return matrix;
	}

	public static Complex KroneckerDelta(int i, int j)
	{
		return new Complex(Math.Min(Math.Abs(i - j), 1));
	}

	public static Matrix ChessboardMatrix(int m, int n, bool even)
	{
		Matrix matrix = new Matrix(m, n);
		if (even)
		{
			for (int i = 1; i <= m; i++)
			{
				for (int j = 1; j <= n; j++)
				{
					matrix[i, j] = KroneckerDelta((i + j) % 2, 0);
				}
			}
		}
		else
		{
			for (int k = 1; k <= m; k++)
			{
				for (int l = 1; l <= n; l++)
				{
					matrix[k, l] = KroneckerDelta((k + l) % 2, 1);
				}
			}
		}
		return matrix;
	}

	public static Matrix ChessboardMatrix(int n, bool even)
	{
		Matrix matrix = new Matrix(n);
		if (even)
		{
			for (int i = 1; i <= n; i++)
			{
				for (int j = 1; j <= n; j++)
				{
					matrix[i, j] = KroneckerDelta((i + j) % 2, 0);
				}
			}
		}
		else
		{
			for (int k = 1; k <= n; k++)
			{
				for (int l = 1; l <= n; l++)
				{
					matrix[k, l] = KroneckerDelta((k + l) % 2, 1);
				}
			}
		}
		return matrix;
	}

	public static Matrix Zeros(int m, int n)
	{
		return new Matrix(m, n);
	}

	public static Matrix Zeros(int n)
	{
		return new Matrix(n);
	}

	public static Matrix Ones(int m, int n)
	{
		Matrix matrix = new Matrix(m, n);
		for (int i = 0; i < m; i++)
		{
			for (int j = 0; j < n; j++)
			{
				((ArrayList)matrix.Values[i])[j] = Complex.One;
			}
		}
		return matrix;
	}

	public static Matrix Ones(int n)
	{
		Matrix matrix = new Matrix(n);
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < n; j++)
			{
				((ArrayList)matrix.Values[i])[j] = Complex.One;
			}
		}
		return matrix;
	}

	public static Matrix Identity(int n)
	{
		return Diag(Ones(n, 1));
	}

	public static Matrix Eye(int n)
	{
		return Identity(n);
	}

	public static Matrix VerticalConcat(Matrix A, Matrix B)
	{
		Matrix matrix = A.Column(1);
		for (int i = 2; i <= A.ColumnCount; i++)
		{
			matrix.InsertColumn(A.Column(i), i);
		}
		for (int j = 1; j <= B.ColumnCount; j++)
		{
			matrix.InsertColumn(B.Column(j), matrix.ColumnCount + 1);
		}
		return matrix;
	}

	public static Matrix VerticalConcat(Matrix[] A)
	{
		if (A == null)
		{
			throw new ArgumentNullException();
		}
		if (A.Length == 1)
		{
			return A[0];
		}
		Matrix matrix = VerticalConcat(A[0], A[1]);
		for (int i = 2; i < A.Length; i++)
		{
			matrix = VerticalConcat(matrix, A[i]);
		}
		return matrix;
	}

	public static Matrix HorizontalConcat(Matrix A, Matrix B)
	{
		Matrix matrix = A.Row(1);
		for (int i = 2; i <= A.RowCount; i++)
		{
			matrix.InsertRow(A.Row(i), i);
		}
		for (int j = 1; j <= B.RowCount; j++)
		{
			matrix.InsertRow(B.Row(j), matrix.RowCount + 1);
		}
		return matrix;
	}

	public static Matrix HorizontalConcat(Matrix[] A)
	{
		if (A == null)
		{
			throw new ArgumentNullException();
		}
		if (A.Length == 1)
		{
			return A[0];
		}
		Matrix matrix = HorizontalConcat(A[0], A[1]);
		for (int i = 2; i < A.Length; i++)
		{
			matrix = HorizontalConcat(matrix, A[i]);
		}
		return matrix;
	}

	public static Matrix Diag(Matrix diag_vector)
	{
		int num = diag_vector.VectorLength();
		if (num == 0)
		{
			throw new ArgumentException("diag_vector must be 1xN or Nx1");
		}
		Matrix matrix = new Matrix(num, num);
		for (int i = 1; i <= num; i++)
		{
			matrix[i, i] = diag_vector[i];
		}
		return matrix;
	}

	public static Matrix Diag(Matrix diag_vector, int offset)
	{
		int num = diag_vector.VectorLength();
		if (num == 0)
		{
			throw new ArgumentException("diag_vector must be 1xN or Nx1.");
		}
		Matrix matrix = new Matrix(num + Math.Abs(offset), num + Math.Abs(offset));
		num = matrix.RowCount;
		if (offset >= 0)
		{
			for (int i = 1; i <= num - offset; i++)
			{
				matrix[i, i + offset] = diag_vector[i];
			}
		}
		else
		{
			for (int j = 1; j <= num + offset; j++)
			{
				matrix[j - offset, j] = diag_vector[j];
			}
		}
		return matrix;
	}

	public static Matrix TriDiag(Complex l, Complex d, Complex u, int n)
	{
		if (n <= 1)
		{
			throw new ArgumentException("Matrix dimension must greater than one.");
		}
		return Diag(l * Ones(n - 1, 1), -1) + Diag(d * Ones(n, 1)) + Diag(u * Ones(n - 1, 1), 1);
	}

	public static Matrix TriDiag(Matrix l, Matrix d, Matrix u)
	{
		int num = l.VectorLength();
		int num2 = d.VectorLength();
		int num3 = u.VectorLength();
		if (num * num2 * num3 == 0)
		{
			throw new ArgumentException("At least one of the paramter matrices is not a vector.");
		}
		if (num != num3)
		{
			throw new ArgumentException("Lower and upper secondary diagonal must have the same length.");
		}
		if (num + 1 != num2)
		{
			throw new ArgumentException("Main diagonal must have exactly one element more than the secondary diagonals.");
		}
		return Diag(l, -1) + Diag(d) + Diag(u, 1);
	}

	public static Complex Dot(Matrix v, Matrix w)
	{
		int num = v.VectorLength();
		int num2 = w.VectorLength();
		if (num == 0 || num2 == 0)
		{
			throw new ArgumentException("Arguments need to be vectors.");
		}
		if (num != num2)
		{
			throw new ArgumentException("Vectors must be of the same length.");
		}
		Complex zero = Complex.Zero;
		for (int i = 1; i <= num; i++)
		{
			zero += v[i] * w[i];
		}
		return zero;
	}

	public static Complex Fib(int n)
	{
		Matrix matrix = Ones(2, 2);
		matrix[2, 2] = Complex.Zero;
		return (matrix ^ (n - 1))[1, 1];
	}

	public static Matrix RandomGraph(int n)
	{
		Matrix matrix = Random(n, n);
		return matrix - Diag(matrix.DiagVector());
	}

	public static Matrix RandomGraph(int n, double p)
	{
		Matrix matrix = new Matrix(n);
		Random random = new Random();
		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				if (i == j)
				{
					matrix[i, j] = Complex.Zero;
				}
				else if (random.NextDouble() < p)
				{
					matrix[i, j] = new Complex(random.NextDouble());
				}
				else
				{
					matrix[i, j] = new Complex(double.PositiveInfinity);
				}
			}
		}
		return matrix;
	}

	public static Matrix Random(int m, int n)
	{
		Matrix matrix = new Matrix(m, n);
		Random random = new Random();
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				matrix[i, j] = new Complex(random.NextDouble());
			}
		}
		return matrix;
	}

	public static Matrix Random(int n)
	{
		Matrix matrix = new Matrix(n);
		Random random = new Random();
		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				matrix[i, j] = new Complex(random.NextDouble());
			}
		}
		return matrix;
	}

	public static Matrix Random(int n, int lo, int hi)
	{
		Matrix matrix = new Matrix(n);
		Random random = new Random();
		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				matrix[i, j] = new Complex(random.Next(lo, hi));
			}
		}
		return matrix;
	}

	public static Matrix RandomZeroOne(int m, int n, double p)
	{
		Matrix matrix = new Matrix(m, n);
		Random random = new Random();
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				if (random.NextDouble() <= p)
				{
					matrix[i, j] = Complex.One;
				}
			}
		}
		return matrix;
	}

	public static Matrix RandomZeroOne(int n, double p)
	{
		Matrix matrix = new Matrix(n, n);
		Random random = new Random();
		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				if (random.NextDouble() <= p)
				{
					matrix[i, j] = Complex.One;
				}
			}
		}
		return matrix;
	}

	public static Matrix Random(int m, int n, int lo, int hi)
	{
		Matrix matrix = new Matrix(m, n);
		Random random = new Random();
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				matrix[i, j] = new Complex(random.Next(lo, hi));
			}
		}
		return matrix;
	}

	public static Matrix Vandermonde(Complex[] x)
	{
		if (x == null || x.Length < 1)
		{
			throw new ArgumentNullException();
		}
		int num = x.Length - 1;
		Matrix matrix = new Matrix(num + 1);
		for (int i = 0; i <= num; i++)
		{
			for (int j = 0; j <= num; j++)
			{
				matrix[i + 1, j + 1] = Complex.Pow(x[i], j);
			}
		}
		return matrix;
	}

	public static Matrix[] Floyd(Matrix adjacence_matrix)
	{
		if (!adjacence_matrix.IsSquare())
		{
			throw new ArgumentException("Expected square matrix.");
		}
		if (!adjacence_matrix.IsReal())
		{
			throw new ArgumentException("Adjacence matrices are expected to be real.");
		}
		int num = adjacence_matrix.RowCount;
		Matrix matrix = adjacence_matrix.Clone();
		Matrix matrix2 = new Matrix(num);
		for (int i = 1; i <= num; i++)
		{
			for (int j = 1; j <= num; j++)
			{
				for (int k = 1; k <= num; k++)
				{
					double num2 = matrix[j, i].Re + matrix[i, k].Re;
					if (num2 < matrix[j, k].Re)
					{
						matrix[j, k].Re = num2;
						matrix2[j, k].Re = i;
					}
				}
			}
		}
		return new Matrix[2] { matrix, matrix2 };
	}

	public static ArrayList FloydPath(Matrix P, int i, int j)
	{
		if (!P.IsSquare())
		{
			throw new ArgumentException("Path matrix must be square.");
		}
		if (!P.IsReal())
		{
			throw new ArgumentException("Adjacence matrices are expected to be real.");
		}
		ArrayList arrayList = new ArrayList();
		arrayList.Add(i);
		while (P[i, j] != 0.0)
		{
			i = Convert.ToInt32(P[i, j]);
			arrayList.Add(i);
		}
		arrayList.Add(j);
		return arrayList;
	}

	public static Matrix DFS(Matrix adjacence_matrix, int root)
	{
		if (!adjacence_matrix.IsSquare())
		{
			throw new ArgumentException("Adjacence matrices are expected to be square.");
		}
		if (!adjacence_matrix.IsReal())
		{
			throw new ArgumentException("Adjacence matrices are expected to be real.");
		}
		int num = adjacence_matrix.RowCount;
		if (root < 1 || root > num)
		{
			throw new ArgumentException("Root must be a vertex of the graph, e.i. in {1, ..., n}.");
		}
		Matrix matrix = new Matrix(num);
		bool[] array = new bool[num + 1];
		Stack stack = new Stack();
		stack.Push(root);
		array[root] = true;
		ArrayList[] array2 = new ArrayList[num + 1];
		for (int i = 1; i <= num; i++)
		{
			array2[i] = new ArrayList();
			for (int j = 1; j <= num; j++)
			{
				if (adjacence_matrix[i, j].Re != 0.0 && adjacence_matrix[i, j].Im != double.PositiveInfinity)
				{
					array2[i].Add(j);
				}
			}
		}
		while (stack.Count > 0)
		{
			int num2 = (int)stack.Peek();
			if (array2[num2].Count > 0)
			{
				int num3 = (int)array2[num2][0];
				if (!array[num3])
				{
					array[num3] = true;
					matrix[num2, num3].Re = 1.0;
					stack.Push(num3);
				}
				array2[num2].RemoveAt(0);
			}
			else
			{
				stack.Pop();
			}
		}
		return matrix;
	}

	public static Matrix BFS(Matrix adjacence_matrix, int root)
	{
		if (!adjacence_matrix.IsSquare())
		{
			throw new ArgumentException("Adjacence matrices are expected to be square.");
		}
		if (!adjacence_matrix.IsReal())
		{
			throw new ArgumentException("Adjacence matrices are expected to be real.");
		}
		int num = adjacence_matrix.RowCount;
		if (root < 1 || root > num)
		{
			throw new ArgumentException("Root must be a vertex of the graph, e.i. in {1, ..., n}.");
		}
		Matrix matrix = new Matrix(num);
		bool[] array = new bool[num + 1];
		Queue queue = new Queue();
		queue.Enqueue(root);
		array[root] = true;
		ArrayList[] array2 = new ArrayList[num + 1];
		for (int i = 1; i <= num; i++)
		{
			array2[i] = new ArrayList();
			for (int j = 1; j <= num; j++)
			{
				if (adjacence_matrix[i, j].Re != 0.0 && adjacence_matrix[i, j].Re != double.PositiveInfinity)
				{
					array2[i].Add(j);
				}
			}
		}
		while (queue.Count > 0)
		{
			int num2 = (int)queue.Peek();
			if (array2[num2].Count > 0)
			{
				int num3 = (int)array2[num2][0];
				if (!array[num3])
				{
					array[num3] = true;
					matrix[num2, num3].Re = 1.0;
					queue.Enqueue(num3);
				}
				array2[num2].RemoveAt(0);
			}
			else
			{
				queue.Dequeue();
			}
		}
		return matrix;
	}

	public static Matrix ZeroOneRandom(int m, int n, double p)
	{
		Random random = new Random();
		Matrix matrix = Zeros(m, n);
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				if (random.NextDouble() <= p)
				{
					matrix[i, j] = Complex.One;
				}
			}
		}
		return matrix;
	}

	public static Matrix ZeroOneRandom(int n, double p)
	{
		Random random = new Random();
		Matrix matrix = Zeros(n);
		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				if (random.NextDouble() <= p)
				{
					matrix[i, j] = Complex.One;
				}
			}
		}
		return matrix;
	}

	private static Matrix[] HouseholderVector(Matrix x)
	{
		int num = x.VectorLength();
		if (num == 0)
		{
			throw new InvalidOperationException("Expected vector as argument.");
		}
		Matrix matrix = x / x.Norm();
		Matrix matrix2 = matrix.Extract(2, num, 1, 1);
		Complex complex = Dot(matrix2, matrix2);
		Matrix matrix3 = Zeros(num, 1);
		matrix3[1] = Complex.One;
		matrix3.Insert(2, 1, matrix2);
		double x2 = 0.0;
		if (complex != 0.0)
		{
			Complex complex2 = Complex.Sqrt(matrix[1] * matrix[1] + complex);
			if (matrix[1].Re <= 0.0)
			{
				matrix3[1] = matrix[1] - complex2;
			}
			else
			{
				matrix3[1] = -complex / (matrix[1] + complex2);
			}
			x2 = 2.0 * matrix3[1].Re * matrix3[1].Re / (complex.Re + matrix3[1].Re * matrix3[1].Re);
			matrix3 /= matrix3[1];
		}
		return new Matrix[2]
		{
			matrix3,
			new Matrix(x2)
		};
	}

	public static Matrix BlockMatrix(Matrix A, Matrix B, Matrix C, Matrix D)
	{
		if (A.RowCount != B.RowCount || C.RowCount != D.RowCount || A.ColumnCount != C.ColumnCount || B.ColumnCount != D.ColumnCount)
		{
			throw new ArgumentException("Matrix dimensions must agree.");
		}
		Matrix matrix = new Matrix(A.RowCount + C.RowCount, A.ColumnCount + B.ColumnCount);
		for (int i = 1; i <= matrix.rowCount; i++)
		{
			for (int j = 1; j <= matrix.columnCount; j++)
			{
				if (i <= A.RowCount)
				{
					if (j <= A.ColumnCount)
					{
						matrix[i, j] = A[i, j];
					}
					else
					{
						matrix[i, j] = B[i, j - A.ColumnCount];
					}
				}
				else if (j <= C.ColumnCount)
				{
					matrix[i, j] = C[i - A.RowCount, j];
				}
				else
				{
					matrix[i, j] = D[i - A.RowCount, j - C.ColumnCount];
				}
			}
		}
		return matrix;
	}

	public static Matrix Solve(Matrix A, Matrix b)
	{
		Matrix matrix = A.Clone();
		Matrix matrix2 = b.Clone();
		if (!matrix.IsSquare())
		{
			throw new InvalidOperationException("Cannot uniquely solve non-square equation system.");
		}
		int n = matrix.RowCount;
		matrix2 = matrix.LUSafe() * matrix2;
		(matrix.ExtractLowerTrapeze() - Diag(matrix.DiagVector()) + Identity(n)).ForwardInsertion(matrix2);
		matrix.ExtractUpperTrapeze().BackwardInsertion(matrix2);
		return matrix2;
	}

	public Matrix Re()
	{
		Matrix matrix = new Matrix(rowCount, columnCount);
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				matrix[i, j] = new Complex(this[i, j].Re);
			}
		}
		return matrix;
	}

	public Matrix Im()
	{
		Matrix matrix = new Matrix(rowCount, columnCount);
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				matrix[i, j] = new Complex(this[i, j].Im);
			}
		}
		return matrix;
	}

	public Matrix[] HessenbergHouseholder()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot perform Hessenberg Householder decomposition of non-square matrix.");
		}
		int num = rowCount;
		Matrix matrix = Identity(num);
		Matrix matrix2 = Clone();
		Matrix[] array = new Matrix[2];
		for (int i = 1; i <= num - 2; i++)
		{
			array = HouseholderVector(matrix2.Extract(i + 1, num, i, i));
			Matrix a = Identity(i);
			Matrix matrix3 = Zeros(i, num - i);
			Matrix matrix4 = Identity(array[0].VectorLength()) - array[1][1, 1] * array[0] * array[0].Transpose();
			matrix2.Insert(i + 1, i, matrix4 * matrix2.Extract(i + 1, num, i, num));
			matrix2.Insert(1, i + 1, matrix2.Extract(1, num, i + 1, num) * matrix4);
			Matrix matrix5 = BlockMatrix(a, matrix3, matrix3.Transpose(), matrix4);
			matrix *= matrix5;
		}
		return new Matrix[2] { matrix2, matrix };
	}

	public Matrix Extract(int i1, int i2, int j1, int j2)
	{
		if (i2 < i1 || j2 < j1 || i1 <= 0 || j2 <= 0 || i2 > rowCount || j2 > columnCount)
		{
			throw new ArgumentException("Index exceeds matrix dimension.");
		}
		Matrix matrix = new Matrix(i2 - i1 + 1, j2 - j1 + 1);
		for (int k = i1; k <= i2; k++)
		{
			for (int l = j1; l <= j2; l++)
			{
				matrix[k - i1 + 1, l - j1 + 1] = this[k, l];
			}
		}
		return matrix;
	}

	public Matrix ExtractLowerTrapeze()
	{
		Matrix matrix = new Matrix(rowCount, columnCount);
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= i; j++)
			{
				matrix[i, j] = this[i, j];
			}
		}
		return matrix;
	}

	public Matrix ExtractUpperTrapeze()
	{
		Matrix matrix = new Matrix(rowCount, columnCount);
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = i; j <= columnCount; j++)
			{
				matrix[i, j] = this[i, j];
			}
		}
		return matrix;
	}

	public Matrix[] ColumnVectorize()
	{
		Matrix[] array = new Matrix[columnCount];
		for (int i = 1; i <= array.Length; i++)
		{
			array[i] = Column(i);
		}
		return array;
	}

	public Matrix[] RowVectorize()
	{
		Matrix[] array = new Matrix[rowCount];
		for (int i = 1; i <= array.Length; i++)
		{
			array[i] = Row(i);
		}
		return array;
	}

	public void VerticalFlip()
	{
		Values.Reverse();
	}

	public void HorizontalFlip()
	{
		for (int i = 0; i < rowCount; i++)
		{
			((ArrayList)Values[i]).Reverse();
		}
	}

	public void SwapColumns(int j1, int j2)
	{
		if (j1 <= 0 || j1 > columnCount || j2 <= 0 || j2 > columnCount)
		{
			throw new ArgumentException("Indices must be positive and <= number of cols.");
		}
		if (j1 != j2)
		{
			j1--;
			j2--;
			for (int i = 0; i < rowCount; i++)
			{
				object value = ((ArrayList)Values[i])[j1];
				((ArrayList)Values[i])[j1] = ((ArrayList)Values[i])[j2];
				((ArrayList)Values[i])[j2] = value;
			}
		}
	}

	public void SwapRows(int i1, int i2)
	{
		if (i1 <= 0 || i1 > rowCount || i2 <= 0 || i2 > rowCount)
		{
			throw new ArgumentException("Indices must be positive and <= number of rows.");
		}
		if (i1 != i2)
		{
			ArrayList value = (ArrayList)Values[--i1];
			Values[i1] = Values[--i2];
			Values[i2] = value;
		}
	}

	public void DeleteRow(int i)
	{
		if (i <= 0 || i > rowCount)
		{
			throw new ArgumentException("Index must be positive and <= number of rows.");
		}
		Values.RemoveAt(i - 1);
		rowCount--;
	}

	public void DeleteColumn(int j)
	{
		if (j <= 0 || j > columnCount)
		{
			throw new ArgumentException("Index must be positive and <= number of cols.");
		}
		for (int i = 0; i < rowCount; i++)
		{
			((ArrayList)Values[i]).RemoveAt(j - 1);
		}
		columnCount--;
	}

	public Matrix ExtractRow(int i)
	{
		Matrix result = Row(i);
		DeleteRow(i);
		return result;
	}

	public Matrix ExtractColumn(int j)
	{
		if (j <= 0 || j > columnCount)
		{
			throw new ArgumentException("Index must be positive and <= number of cols.");
		}
		Matrix result = Column(j);
		DeleteColumn(j);
		return result;
	}

	public void InsertRow(Matrix row, int i)
	{
		int num = row.VectorLength();
		if (num == 0)
		{
			throw new InvalidOperationException("Row must be a vector of length > 0.");
		}
		if (i <= 0)
		{
			throw new ArgumentException("Row index must be positive.");
		}
		if (i > rowCount)
		{
			this[i, num] = Complex.Zero;
		}
		else if (num > columnCount)
		{
			this[i, num] = Complex.Zero;
			rowCount++;
		}
		else
		{
			rowCount++;
		}
		Values.Insert(--i, new ArrayList(num));
		for (int j = 1; j <= num; j++)
		{
			((ArrayList)Values[i]).Add(row[j]);
		}
		for (int k = num; k < columnCount; k++)
		{
			((ArrayList)Values[i]).Add(Complex.Zero);
		}
	}

	public void Insert(int i, int j, Matrix M)
	{
		for (int k = 1; k <= M.rowCount; k++)
		{
			for (int l = 1; l <= M.columnCount; l++)
			{
				this[i + k - 1, j + l - 1] = M[k, l];
			}
		}
	}

	public void InsertColumn(Matrix col, int j)
	{
		int num = col.VectorLength();
		if (num == 0)
		{
			throw new InvalidOperationException("Row must be a vector of length > 0.");
		}
		if (j <= 0)
		{
			throw new ArgumentException("Row index must be positive.");
		}
		if (j > columnCount)
		{
			this[num, j] = Complex.Zero;
		}
		else
		{
			columnCount++;
		}
		if (num > rowCount)
		{
			this[num, j] = Complex.Zero;
		}
		j--;
		for (int i = 0; i < num; i++)
		{
			((ArrayList)Values[i]).Insert(j, col[i + 1]);
		}
		for (int k = num; k < rowCount; k++)
		{
			((ArrayList)Values[k]).Insert(j, 0);
		}
	}

	public Matrix Inverse()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot invert non-square matrix.");
		}
		Complex complex = Determinant();
		if (complex == Complex.Zero)
		{
			throw new InvalidOperationException("Cannot invert (nearly) singular matrix.");
		}
		int num = columnCount;
		if (num == 1)
		{
			return new Matrix(1.0 / complex);
		}
		if (IsReal() && IsOrthogonal())
		{
			return Transpose();
		}
		if (IsUnitary())
		{
			return ConjTranspose();
		}
		if (IsDiagonal())
		{
			Matrix matrix = DiagVector();
			for (int i = 1; i <= num; i++)
			{
				matrix[i] = 1.0 / matrix[i];
			}
			return Diag(matrix);
		}
		Complex[,] array = new Complex[num, num];
		for (int j = 0; j < num; j++)
		{
			for (int k = 0; k < num; k++)
			{
				array[j, k] = Math.Pow(-1.0, j + k) * Minor(k + 1, j + 1).Determinant();
			}
		}
		return new Matrix(array) / complex;
	}

	public Matrix InverseLeverrier()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot invert non-square matrix.");
		}
		int num = rowCount;
		Matrix matrix = Identity(num);
		Matrix matrix2 = matrix;
		Complex complex;
		for (int i = 1; i < num; i++)
		{
			Matrix matrix3 = this * matrix2;
			complex = 1.0 / (double)i * matrix3.Trace();
			matrix2 = complex * matrix - matrix3;
		}
		complex = (this * matrix2).Trace() / num;
		if (complex != Complex.Zero)
		{
			return matrix2 / complex;
		}
		throw new InvalidOperationException("WARNING: Matrix nearly singular or badly scaled.");
	}

	public Matrix Minor(int i, int j)
	{
		Matrix matrix = Clone();
		matrix.DeleteRow(i);
		matrix.DeleteColumn(j);
		return matrix;
	}

	public Matrix Clone()
	{
		Matrix matrix = new Matrix();
		matrix.rowCount = rowCount;
		matrix.columnCount = columnCount;
		for (int i = 0; i < rowCount; i++)
		{
			matrix.Values.Add(((ArrayList)Values[i]).Clone());
		}
		return matrix;
	}

	public Matrix DiagVector()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot get diagonal of non-square matrix.");
		}
		Matrix matrix = new Matrix(columnCount, 1);
		for (int i = 1; i <= columnCount; i++)
		{
			matrix[i] = this[i, i];
		}
		return matrix;
	}

	public Matrix Column(int j)
	{
		Matrix matrix = new Matrix(rowCount, 1);
		for (int i = 1; i <= rowCount; i++)
		{
			matrix[i] = this[i, j];
		}
		return matrix;
	}

	public Matrix Row(int i)
	{
		if (i <= 0 || i > rowCount)
		{
			throw new ArgumentException("Index exceed matrix dimension.");
		}
		Matrix matrix = new Matrix(columnCount, 1);
		for (int j = 1; j <= columnCount; j++)
		{
			matrix[j] = this[i, j];
		}
		return matrix;
	}

	public Matrix Transpose()
	{
		Matrix matrix = new Matrix(columnCount, rowCount);
		for (int i = 1; i <= columnCount; i++)
		{
			for (int j = 1; j <= rowCount; j++)
			{
				matrix[i, j] = this[j, i];
			}
		}
		return matrix;
	}

	public Matrix Conjugate()
	{
		Matrix matrix = new Matrix(rowCount, columnCount);
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				matrix[i, j] = Complex.Conj(this[i, j]);
			}
		}
		return matrix;
	}

	public Matrix ConjTranspose()
	{
		return Transpose().Conjugate();
	}

	public void LU()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot perform LU-decomposition of non-square matrix.");
		}
		int num = columnCount;
		for (int i = 1; i <= num; i++)
		{
			if (this[i, i] == 0.0)
			{
				throw new DivideByZeroException("Warning: Matrix badly scaled or close to singular. Try LUSafe() instead. Check if det != 0.");
			}
			for (int j = 1; j < i; j++)
			{
				for (int k = j + 1; k <= num; k++)
				{
					this[k, i] -= this[k, j] * this[j, i];
				}
			}
			for (int l = i + 1; l <= num; l++)
			{
				this[l, i] /= this[i, i];
			}
		}
	}

	public Matrix LUSafe()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot perform LU-decomposition of non-square matrix.");
		}
		int num = columnCount;
		Matrix matrix = Identity(num);
		for (int i = 1; i <= num; i++)
		{
			if (i < num)
			{
				int num2 = i;
				for (int j = i + 1; j <= num; j++)
				{
					if (Complex.Abs(this[j, i]) > Complex.Abs(this[num2, i]))
					{
						num2 = j;
					}
				}
				if (num2 > i)
				{
					matrix.SwapRows(i, num2);
					SwapRows(i, num2);
				}
				if (this[i, i] == 0.0)
				{
					throw new DivideByZeroException("Warning: Matrix close to singular.");
				}
			}
			for (int k = 1; k < i; k++)
			{
				for (int l = k + 1; l <= num; l++)
				{
					this[l, i] -= this[l, k] * this[k, i];
				}
			}
			for (int m = i + 1; m <= num; m++)
			{
				this[m, i] /= this[i, i];
			}
		}
		return matrix;
	}

	public void Cholesky()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot perform Cholesky decomposition of non-square matrix.");
		}
		if (!IsSPD())
		{
			throw new InvalidOperationException("Cannot perform Cholesky decomposition of matrix not being symmetric positive definite.");
		}
		int num = rowCount;
		for (int i = 1; i < num; i++)
		{
			this[i, i] = Complex.Sqrt(this[i, i]);
			for (int j = 1; j <= num - i; j++)
			{
				this[i + j, i] /= this[i, i];
			}
			for (int k = i + 1; k <= num; k++)
			{
				for (int l = 0; l <= num - k; l++)
				{
					this[k + l, k] -= this[k + l, i] * this[k, i];
				}
			}
		}
		this[num, num] = Complex.Sqrt(this[num, num]);
	}

	public void CholeskyUndo()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot undo cholesky decomposition on non-square matrix.");
		}
		this[1, 1] = Sqr(this[1, 1]);
		for (int i = 2; i <= rowCount; i++)
		{
			Complex zero = Complex.Zero;
			for (int j = 1; j <= i - 1; j++)
			{
				zero += Sqr(this[i, j]);
			}
			this[i, i] = Sqr(this[i, i]) + zero;
		}
		SymmetrizeDown();
	}

	public void ForwardInsertion(Matrix b)
	{
		if (!IsLowerTriangular())
		{
			throw new InvalidOperationException("Cannot perform forward insertion for matrix not being lower triangular.");
		}
		if (DiagProd() == 0.0)
		{
			throw new InvalidOperationException("Warning: Matrix is nearly singular.");
		}
		int num = rowCount;
		if (b.VectorLength() != num)
		{
			throw new ArgumentException("Parameter must vector of the same height as matrix.");
		}
		for (int i = 1; i <= num - 1; i++)
		{
			b[i] /= this[i, i];
			for (int j = 1; j <= num - i; j++)
			{
				b[i + j] -= b[i] * this[i + j, i];
			}
		}
		b[num] /= this[num, num];
	}

	public void BackwardInsertion(Matrix b)
	{
		if (!IsUpperTriangular())
		{
			throw new InvalidOperationException("Cannot perform backward insertion for matrix not being upper triangular.");
		}
		if (DiagProd() == 0.0)
		{
			throw new InvalidOperationException("Warning: Matrix is nearly singular.");
		}
		int num = rowCount;
		if (b.VectorLength() != num)
		{
			throw new ArgumentException("Parameter must vector of the same height as matrix.");
		}
		for (int num2 = num; num2 >= 2; num2--)
		{
			b[num2] /= this[num2, num2];
			for (int i = 1; i <= num2 - 1; i++)
			{
				b[i] -= b[num2] * this[i, num2];
			}
		}
		b[1] /= this[1, 1];
	}

	public void SymmetrizeDown()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot symmetrize non-square matrix.");
		}
		for (int i = 1; i <= columnCount; i++)
		{
			for (int j = i + 1; j <= columnCount; j++)
			{
				this[j, i] = this[i, j];
			}
		}
	}

	public void SymmetrizeUp()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot symmetrize non-square matrix.");
		}
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = i + 1; j <= columnCount; j++)
			{
				this[i, j] = this[j, i];
			}
		}
	}

	public Matrix[] QRGramSchmidt()
	{
		int num = rowCount;
		int num2 = columnCount;
		Matrix matrix = Clone();
		Matrix matrix2 = new Matrix(num, num2);
		Matrix matrix3 = new Matrix(num2, num2);
		for (int i = 1; i <= num; i++)
		{
			matrix2[i, 1] = matrix[i, 1];
		}
		matrix3[1, 1] = Complex.One;
		for (int j = 1; j <= num2; j++)
		{
			matrix3[j, j] = new Complex(matrix.Column(j).Norm());
			for (int k = 1; k <= num; k++)
			{
				matrix2[k, j] = matrix[k, j] / matrix3[j, j];
			}
			for (int l = j + 1; l <= num2; l++)
			{
				matrix3[j, l] = Dot(matrix2.Column(j), matrix.Column(l));
				for (int m = 1; m <= num; m++)
				{
					matrix[m, l] -= matrix2[m, j] * matrix3[j, l];
				}
			}
		}
		return new Matrix[2] { matrix2, matrix3 };
	}

	public Matrix Eigenvalues()
	{
		return QRIterationBasic(40).DiagVector();
	}

	public Matrix Eigenvector(Complex eigenvalue)
	{
		throw new NotImplementedException();
	}

	public Matrix SolveCG(Matrix b)
	{
		if (!IsSPD())
		{
			throw new InvalidOperationException("CG method only works for spd matrices.");
		}
		if (!IsReal())
		{
			throw new InvalidOperationException("CG method only works for real matrices.");
		}
		int m = rowCount;
		int num = 150;
		double num2 = 1E-06;
		Matrix matrix = Ones(m, 1);
		Matrix matrix2 = b - this * matrix;
		Matrix matrix3 = matrix2;
		double num3 = matrix2.Norm();
		num3 *= num3;
		num2 *= num2;
		Matrix matrix4 = Zeros(m, 1);
		if (num3 <= num2)
		{
			return matrix;
		}
		for (int i = 0; i < num; i++)
		{
			matrix4 = this * matrix3;
			double re = Dot(matrix4, matrix3).Re;
			if (Math.Abs(re) <= num2)
			{
				return matrix;
			}
			double num4 = num3 / re;
			matrix += num4 * matrix3;
			matrix2 -= num4 * matrix4;
			double num5 = num3;
			num3 = matrix2.Norm();
			num3 *= num3;
			if (num3 <= num2)
			{
				return matrix;
			}
			matrix3 = matrix2 + num3 / num5 * matrix3;
		}
		return matrix;
	}

	public Matrix QRIterationBasic(int max_iterations)
	{
		if (!IsReal())
		{
			throw new InvalidOperationException("Basic QR iteration is possible only for real matrices.");
		}
		Matrix matrix = Clone();
		Matrix[] array = new Matrix[2];
		for (int i = 0; i < max_iterations; i++)
		{
			array = matrix.QRGramSchmidt();
			matrix = array[1] * array[0];
		}
		return matrix;
	}

	public Matrix QRIterationHessenberg(int max_iterations)
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot perform QR iteration of non-square matrix.");
		}
		int num = RowCount;
		Matrix matrix = HessenbergHouseholder()[0];
		for (int i = 1; i <= max_iterations; i++)
		{
			Matrix[] array = matrix.QRGivens();
			matrix = array[1];
			for (int j = 1; j <= num - 1; j++)
			{
				matrix.Gacol(array[2][j], array[3][j], 1, j + 1, j, j + 1);
			}
		}
		return matrix;
	}

	public Matrix[] QRGivens()
	{
		Matrix matrix = Clone();
		int num = matrix.ColumnCount;
		Matrix matrix2 = Zeros(num - 1, 1);
		Matrix matrix3 = Zeros(num - 1, 1);
		for (int i = 1; i <= num - 1; i++)
		{
			Complex[] array = GivensCS(matrix[i, i], matrix[i + 1, i]);
			matrix2[i] = array[0];
			matrix3[i] = array[1];
			Garow(matrix2[i], matrix3[i], 1, i + 1, i, i + 1);
		}
		return new Matrix[4]
		{
			GivProd(matrix2, matrix3, num),
			matrix,
			matrix2,
			matrix3
		};
	}

	private Matrix GivProd(Matrix c, Matrix s, int n)
	{
		int num = n - 1;
		int num2 = n - 2;
		Matrix matrix = Eye(n);
		matrix[num, num] = c[num];
		matrix[n, n] = c[num];
		matrix[num, n] = s[num];
		matrix[n, num] = -s[num];
		for (int num3 = num2; num3 >= 1; num3--)
		{
			int num4 = num3 + 1;
			matrix[num3, num3] = c[num3];
			matrix[num4, num3] = -s[num3];
			Matrix matrix2 = matrix.Extract(num4, num4, num4, n);
			matrix.Insert(num3, num4, s[num3] * matrix2);
			matrix.Insert(num4, num4, c[num3] * matrix2);
		}
		return matrix;
	}

	private void Garow(Complex c, Complex s, int i, int k, int j1, int j2)
	{
		for (int l = j1; l <= j2; l++)
		{
			Complex complex = this[i, l];
			Complex complex2 = this[k, l];
			this[i, l] = c * complex - s * complex2;
			this[k, l] = s * complex + c * complex2;
		}
	}

	public void Gacol(Complex c, Complex s, int j1, int j2, int i, int k)
	{
		for (int l = j1; l <= j2; l++)
		{
			Complex complex = this[l, i];
			Complex complex2 = this[l, k];
			this[l, i] = c * complex - s * complex2;
			this[l, k] = s * complex + c * complex2;
		}
	}

	private Complex[] GivensCS(Complex xi, Complex xk)
	{
		Complex zero = Complex.Zero;
		Complex complex = Complex.Zero;
		if (xk == 0.0)
		{
			zero = Complex.One;
		}
		else if (Complex.Abs(xk) > Complex.Abs(xi))
		{
			Complex complex2 = -xi / xk;
			complex = 1.0 / Complex.Sqrt(1.0 + complex2 * complex2);
			zero = complex * complex2;
		}
		else
		{
			Complex complex3 = -xk / xi;
			zero = 1.0 / Complex.Sqrt(1.0 + complex3 * complex3);
			complex = zero * complex3;
		}
		return new Complex[2] { zero, complex };
	}

	public Complex Determinant()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot calc determinant of non-square matrix.");
		}
		if (columnCount == 1)
		{
			return this[1, 1];
		}
		if (IsTrapeze())
		{
			return DiagProd();
		}
		Matrix matrix = Clone();
		return matrix.LUSafe().Signum() * matrix.DiagProd();
	}

	public double ColumnSumNorm()
	{
		return TaxiNorm();
	}

	public double RowSumNorm()
	{
		return MaxNorm();
	}

	public Complex Permanent()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot compute permanent of non-square matrix.");
		}
		if (HasZeroRowOrColumn())
		{
			return Complex.Zero;
		}
		if (this == Ones(rowCount))
		{
			return new Complex(Factorial(rowCount));
		}
		if (IsPermutation())
		{
			return Complex.One;
		}
		Complex zero = Complex.Zero;
		int minRow = GetMinRow();
		int minColumn = GetMinColumn();
		if (AbsRowSum(minRow) < AbsColumnSum(minColumn))
		{
			for (int i = 1; i <= columnCount; i++)
			{
				if (this[minRow, i] != 0.0)
				{
					zero += this[minRow, i] * Minor(minRow, i).Permanent();
				}
			}
		}
		else
		{
			for (int j = 1; j <= rowCount; j++)
			{
				if (this[j, minColumn] != 0.0)
				{
					zero += this[j, minColumn] * Minor(j, minColumn).Permanent();
				}
			}
		}
		return zero;
	}

	public int GetMinRow()
	{
		double num = AbsRowSum(1);
		int result = 1;
		for (int i = 2; i <= rowCount; i++)
		{
			double num2 = AbsRowSum(i);
			if (num2 < num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	public int GetMinColumn()
	{
		double num = AbsColumnSum(1);
		int result = 1;
		for (int i = 2; i <= columnCount; i++)
		{
			double num2 = AbsColumnSum(i);
			if (num2 < num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	private double Factorial(int x)
	{
		double num = 1.0;
		for (int i = 2; i <= x; i++)
		{
			num *= (double)i;
		}
		return num;
	}

	public double Signum()
	{
		double num = 1.0;
		int num2 = rowCount;
		int num3 = 0;
		for (int i = 1; i < num2; i++)
		{
			double num4;
			for (num4 = 1.0; num4 < (double)num2 && this[i, (int)num4] != Complex.One; num4 += 1.0)
			{
				num3 = num3++;
			}
			for (int j = i + 1; j <= num2; j++)
			{
				double num5;
				for (num5 = 1.0; num5 <= (double)num2 && this[j, (int)num5] != Complex.One; num5 += 1.0)
				{
				}
				num *= (num4 - num5) / (double)(i - j);
			}
		}
		return num;
	}

	public double Condition()
	{
		return TaxiNorm() * Inverse().TaxiNorm();
	}

	public double Condition(int p)
	{
		return PNorm(p) * Inverse().PNorm(p);
	}

	public double ConditionFro()
	{
		return FrobeniusNorm() * Inverse().FrobeniusNorm();
	}

	public double PNorm(double p)
	{
		if (p <= 0.0)
		{
			throw new ArgumentException("Argument must be greater than zero.");
		}
		if (p == 1.0)
		{
			return TaxiNorm();
		}
		if (p == double.PositiveInfinity)
		{
			return MaxNorm();
		}
		int num = VectorLength();
		if (num == 0)
		{
			throw new InvalidOperationException("Cannot calc p-norm of matrix.");
		}
		double num2 = 0.0;
		for (int i = 1; i <= num; i++)
		{
			num2 += Math.Pow(Complex.Abs(this[i]), p);
		}
		return Math.Pow(num2, 1.0 / p);
	}

	public double Norm()
	{
		return PNorm(2.0);
	}

	public double FrobeniusNorm()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot compute frobenius norm of non-square matrix.");
		}
		int num = columnCount;
		double num2 = 0.0;
		for (int i = 1; i <= num; i++)
		{
			for (int j = 1; j <= num; j++)
			{
				num2 += (this[i, j] * Complex.Conj(this[i, j])).Re;
			}
		}
		return Math.Sqrt(num2);
	}

	public double TaxiNorm()
	{
		double num = 0.0;
		int num2 = VectorLength();
		if (num2 != 0)
		{
			for (int i = 1; i <= num2; i++)
			{
				num += Complex.Abs(this[i]);
			}
		}
		else
		{
			double num3 = 0.0;
			for (int j = 1; j <= columnCount; j++)
			{
				num3 = AbsColumnSum(j);
				if (num3 > num)
				{
					num = num3;
				}
			}
		}
		return num;
	}

	public double MaxNorm()
	{
		double num = 0.0;
		double num2 = 0.0;
		int num3 = VectorLength();
		if (num3 != 0)
		{
			for (int i = 1; i <= num3; i++)
			{
				num2 = Complex.Abs(this[i]);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		else
		{
			for (int j = 1; j <= rowCount; j++)
			{
				num2 = AbsRowSum(j);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		return num;
	}

	public Complex ColumnSum(int j)
	{
		if (j <= 0 || j > columnCount)
		{
			throw new ArgumentException("Index out of range.");
		}
		Complex zero = Complex.Zero;
		j--;
		for (int i = 0; i < rowCount; i++)
		{
			zero += (Complex)((ArrayList)Values[i])[j];
		}
		return zero;
	}

	public double AbsColumnSum(int j)
	{
		if (j <= 0 || j > columnCount)
		{
			throw new ArgumentException("Index out of range.");
		}
		double num = 0.0;
		for (int i = 1; i <= rowCount; i++)
		{
			num += Complex.Abs(this[i, j]);
		}
		return num;
	}

	public Complex RowSum(int i)
	{
		if (i <= 0 || i > rowCount)
		{
			throw new ArgumentException("Index out of range.");
		}
		Complex zero = Complex.Zero;
		ArrayList arrayList = (ArrayList)Values[i - 1];
		for (int j = 0; j < columnCount; j++)
		{
			zero += (Complex)arrayList[j];
		}
		return zero;
	}

	public double AbsRowSum(int i)
	{
		if (i <= 0 || i > rowCount)
		{
			throw new ArgumentException("Index out of range.");
		}
		double num = 0.0;
		for (int j = 1; j <= columnCount; j++)
		{
			num += Complex.Abs(this[i, j]);
		}
		return num;
	}

	public Complex DiagProd()
	{
		Complex one = Complex.One;
		int num = Math.Min(rowCount, columnCount);
		for (int i = 1; i <= num; i++)
		{
			one *= this[i, i];
		}
		return one;
	}

	public Complex Trace()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Cannot calc trace of non-square matrix.");
		}
		Complex zero = Complex.Zero;
		for (int i = 1; i <= rowCount; i++)
		{
			zero += this[i, i];
		}
		return zero;
	}

	private Complex Sqr(Complex x)
	{
		return x * x;
	}

	public bool IsNormal()
	{
		return this * ConjTranspose() == ConjTranspose() * this;
	}

	public bool IsUnitary()
	{
		if (!IsSquare())
		{
			return false;
		}
		return ConjTranspose() * this == Identity(rowCount);
	}

	public bool IsHermitian()
	{
		if (!IsSquare())
		{
			return false;
		}
		return ConjTranspose() == this;
	}

	public bool IsReal()
	{
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				if (!this[i, j].IsReal())
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool IsSymmetricPositiveDefinite()
	{
		if (IsSymmetric())
		{
			return Definiteness() == DefinitenessType.PositiveDefinite;
		}
		return false;
	}

	public bool IsSPD()
	{
		return IsSymmetricPositiveDefinite();
	}

	public DefinitenessType Definiteness()
	{
		if (!IsSquare())
		{
			throw new InvalidOperationException("Definiteness undefined for non-square matrices.");
		}
		if (this == Zeros(rowCount, columnCount))
		{
			return DefinitenessType.Indefinite;
		}
		if (!IsSymmetric())
		{
			throw new InvalidOperationException("This test works only for symmetric matrices.");
		}
		if (!IsReal())
		{
			throw new InvalidOperationException("This test only works for real matrices.");
		}
		int num = rowCount;
		Matrix[] array = new Matrix[num + 1];
		for (int i = 0; i <= num; i++)
		{
			array[i] = Zeros(num, 1);
		}
		array[1] = Column(1);
		for (int j = 2; j <= num; j++)
		{
			Matrix matrix = Column(j);
			Matrix matrix2 = Zeros(num, 1);
			for (int k = 1; k < j; k++)
			{
				matrix2 += array[k] * Dot(matrix, this * array[k]) / Dot(array[k], this * array[k]);
			}
			array[j] = matrix - matrix2;
		}
		bool flag = true;
		for (int l = 1; l < num; l++)
		{
			Complex complex = Dot(array[l], this * array[l]) * Dot(array[l + 1], this * array[l + 1]);
			if (complex == 0.0)
			{
				flag = false;
			}
			else if (complex.Re < 0.0)
			{
				return DefinitenessType.Indefinite;
			}
		}
		if (Dot(array[1], this * array[1]).Re >= 0.0)
		{
			if (flag)
			{
				return DefinitenessType.PositiveDefinite;
			}
			return DefinitenessType.PositiveSemidefinite;
		}
		if (flag)
		{
			return DefinitenessType.NegativeDefinite;
		}
		return DefinitenessType.NegativeSemidefinite;
	}

	public bool HasZeroRowOrColumn()
	{
		for (int i = 1; i <= rowCount; i++)
		{
			if (AbsRowSum(i) == 0.0)
			{
				return true;
			}
		}
		for (int j = 1; j <= columnCount; j++)
		{
			if (AbsColumnSum(j) == 0.0)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsZeroOneMatrix()
	{
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				if (this[i, j] != Complex.Zero && this[i, j] != Complex.One)
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool IsPermutation()
	{
		if (!IsSquare() && IsZeroOneMatrix())
		{
			return IsInvolutary();
		}
		return false;
	}

	public bool IsDiagonal()
	{
		return Clone() - Diag(DiagVector()) == Zeros(rowCount, columnCount);
	}

	public int VectorLength()
	{
		if (columnCount > 1 && rowCount > 1)
		{
			return 0;
		}
		return Math.Max(columnCount, rowCount);
	}

	public bool IsSquare()
	{
		return columnCount == rowCount;
	}

	public bool IsInvolutary()
	{
		return this * this == Identity(rowCount);
	}

	public bool IsSymmetric()
	{
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				if (this[i, j] != this[j, i])
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool IsOrthogonal()
	{
		if (IsSquare())
		{
			return this * Transpose() == Identity(rowCount);
		}
		return false;
	}

	public bool IsTrapeze()
	{
		if (!IsUpperTrapeze())
		{
			return IsLowerTrapeze();
		}
		return true;
	}

	public bool IsTriangular()
	{
		if (!IsLowerTriangular())
		{
			return IsUpperTriangular();
		}
		return true;
	}

	public bool IsUpperTriangular()
	{
		if (IsSquare())
		{
			return IsUpperTrapeze();
		}
		return false;
	}

	public bool IsLowerTriangular()
	{
		if (IsSquare())
		{
			return IsLowerTrapeze();
		}
		return false;
	}

	public bool IsUpperTrapeze()
	{
		for (int i = 1; i <= columnCount; i++)
		{
			for (int j = i + 1; j <= rowCount; j++)
			{
				if (this[j, i] != 0.0)
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool IsLowerTrapeze()
	{
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = i + 1; j <= columnCount; j++)
			{
				if (this[i, j] != 0.0)
				{
					return false;
				}
			}
		}
		return true;
	}

	public override string ToString()
	{
		string text = "";
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				Complex complex = this[i, j];
				text = ((complex.Re != double.PositiveInfinity && complex.Re != double.NegativeInfinity && complex.Im != double.PositiveInfinity && complex.Im != double.NegativeInfinity) ? ((complex.Re != double.NaN && complex.Im != double.NaN) ? (text + complex.ToString()) : (text + "?")) : (text + "oo"));
				text += ";\t";
			}
			text = text + "\\" + Environment.NewLine;
		}
		return text;
	}

	public string ToString(string format)
	{
		string text = "";
		for (int i = 1; i <= rowCount; i++)
		{
			for (int j = 1; j <= columnCount; j++)
			{
				Complex complex = this[i, j];
				text = ((complex.Re != double.PositiveInfinity && complex.Re != double.NegativeInfinity && complex.Im != double.PositiveInfinity && complex.Im != double.NegativeInfinity) ? ((complex.Re != double.NaN && complex.Im != double.NaN) ? (text + complex.ToString(format)) : (text + "?")) : (text + "oo"));
				text += ";\t";
			}
			text = text + "\\" + Environment.NewLine;
		}
		return text;
	}

	public override bool Equals(object obj)
	{
		return obj.ToString() == ToString();
	}

	public override int GetHashCode()
	{
		return -1;
	}

	public static bool operator ==(Matrix A, Matrix B)
	{
		if (A.RowCount != B.RowCount || A.ColumnCount != B.ColumnCount)
		{
			return false;
		}
		for (int i = 1; i <= A.RowCount; i++)
		{
			for (int j = 1; j <= A.ColumnCount; j++)
			{
				if (A[i, j] != B[i, j])
				{
					return false;
				}
			}
		}
		return true;
	}

	public static bool operator !=(Matrix A, Matrix B)
	{
		return !(A == B);
	}

	public static Matrix operator +(Matrix A, Matrix B)
	{
		if (A.RowCount != B.RowCount || A.ColumnCount != B.ColumnCount)
		{
			throw new ArgumentException("Matrices must be of the same dimension.");
		}
		for (int i = 1; i <= A.RowCount; i++)
		{
			for (int j = 1; j <= A.ColumnCount; j++)
			{
				A[i, j] += B[i, j];
			}
		}
		return A;
	}

	public static Matrix operator -(Matrix A, Matrix B)
	{
		if (A.RowCount != B.RowCount || A.ColumnCount != B.ColumnCount)
		{
			throw new ArgumentException("Matrices must be of the same dimension.");
		}
		for (int i = 1; i <= A.RowCount; i++)
		{
			for (int j = 1; j <= A.ColumnCount; j++)
			{
				A[i, j] -= B[i, j];
			}
		}
		return A;
	}

	public static Matrix operator -(Matrix A)
	{
		for (int i = 1; i <= A.RowCount; i++)
		{
			for (int j = 1; j <= A.ColumnCount; j++)
			{
				A[i, j] = -A[i, j];
			}
		}
		return A;
	}

	public static Matrix operator *(Matrix A, Matrix B)
	{
		if (A.ColumnCount != B.RowCount)
		{
			throw new ArgumentException("Inner matrix dimensions must agree.");
		}
		Matrix matrix = new Matrix(A.RowCount, B.ColumnCount);
		for (int i = 1; i <= A.RowCount; i++)
		{
			for (int j = 1; j <= B.ColumnCount; j++)
			{
				matrix[i, j] = Dot(A.Row(i), B.Column(j));
			}
		}
		return matrix;
	}

	public static Matrix operator *(Matrix A, Complex x)
	{
		Matrix matrix = new Matrix(A.rowCount, A.columnCount);
		for (int i = 1; i <= A.RowCount; i++)
		{
			for (int j = 1; j <= A.ColumnCount; j++)
			{
				matrix[i, j] = A[i, j] * x;
			}
		}
		return matrix;
	}

	public static Matrix operator *(Complex x, Matrix A)
	{
		Matrix matrix = new Matrix(A.RowCount, A.ColumnCount);
		for (int i = 1; i <= A.RowCount; i++)
		{
			for (int j = 1; j <= A.ColumnCount; j++)
			{
				matrix[i, j] = A[i, j] * x;
			}
		}
		return matrix;
	}

	public static Matrix operator *(Matrix A, double x)
	{
		return new Complex(x) * A;
	}

	public static Matrix operator *(double x, Matrix A)
	{
		return new Complex(x) * A;
	}

	public static Matrix operator /(Matrix A, Complex x)
	{
		return 1.0 / x * A;
	}

	public static Matrix operator /(Matrix A, double x)
	{
		return new Complex(1.0 / x) * A;
	}

	public static Matrix operator ^(Matrix A, int k)
	{
		if (k < 0)
		{
			if (A.IsSquare())
			{
				return A.InverseLeverrier() ^ -k;
			}
			throw new InvalidOperationException("Cannot take non-square matrix to the power of zero.");
		}
		switch (k)
		{
		case 0:
			if (A.IsSquare())
			{
				return Identity(A.RowCount);
			}
			throw new InvalidOperationException("Cannot take non-square matrix to the power of zero.");
		case 1:
			if (A.IsSquare())
			{
				return A;
			}
			throw new InvalidOperationException("Cannot take non-square matrix to the power of one.");
		default:
		{
			Matrix result = A;
			for (int i = 1; i < k; i++)
			{
				result *= A;
			}
			return result;
		}
		}
	}
}
