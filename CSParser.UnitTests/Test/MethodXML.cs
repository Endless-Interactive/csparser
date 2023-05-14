namespace TestNamespace;

public class MethodXML
{
	/// <summary>
	///     Test method
	/// </summary>
	/// <param name="a">First int</param>
	/// <param name="b">Second int</param>
	/// <returns>A + B</returns>
	public int TestMethod(int a, int b)
	{
		return 0;
	}

	/// <see cref="TestMethod" />
	public int TestMethod2(int a, int b)
	{
		return TestMethod(a, b);
	}

	/// <seealso cref="TestMethod2" />
	public int TestMethod3(int a, int b)
	{
		return TestMethod2(a, b);
	}

	/// <see href="https://google.com">Google</see>
	public int TestMethod4(int a, int b)
	{
		return TestMethod3(a, b);
	}

	/// <seealso href="https://google.com">Google</seealso>
	public int TestMethod5(int a, int b)
	{
		return TestMethod3(a, b);
	}

	/// <see langword="test" />
	public int TestMethod6(int a, int b)
	{
		return TestMethod3(a, b);
	}

	/// <value>test</value>
	public int TestMethod7(int a, int b)
	{
		return TestMethod3(a, b);
	}

	/// <example>
	///     An example
	///     <code>TestMethod8(1, 2)</code>
	/// </example>
	public int TestMethod8(int a, int b)
	{
		return TestMethod3(a, b);
	}

	/// <exception cref="System.Exception">Description</exception>
	/// <exception cref="System.Exception">Description 2</exception>
	public int TestMethod9(int a, int b)
	{
		return TestMethod3(a, b);
	}
}