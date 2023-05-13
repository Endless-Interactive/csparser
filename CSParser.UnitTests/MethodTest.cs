namespace CSParser.UnitTests;

public class MethodTest
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void MethodRenders()
	{
		var method = new CSMethod
		{
			Name = "TestMethod",
			ReturnType = "void",
			AccessModifier = CSAccessModifier.Public
		};

		Assert.That(method.ToString(), Is.EqualTo("public void TestMethod()"));
	}

	[Test]
	public void MethodRendersWithParameters()
	{
		var method = new CSMethod
		{
			Name = "TestMethod",
			ReturnType = "void",
			AccessModifier = CSAccessModifier.Public
		};

		method.Parameters.Add(new CSParameter
		{
			Name = "test",
			Type = "string"
		});

		Assert.That(method.ToString(), Is.EqualTo("public void TestMethod(string test)"));
	}

	[Test]
	public void MethodRendersWithParametersAndModifiers()
	{
		var method = new CSMethod
		{
			Name = "TestMethod",
			ReturnType = "void"
		};

		method.Parameters.Add(new CSParameter
		{
			Name = "test",
			Type = "string"
		});

		method.SetModifiers("private static");

		Assert.That(method.ToString(), Is.EqualTo("private static void TestMethod(string test)"));
	}

	[Test]
	public void MethodRendersWithOptionalParameter()
	{
		var method = new CSMethod
		{
			Name = "TestMethod",
			ReturnType = "void",
			AccessModifier = CSAccessModifier.Public
		};

		method.Parameters.Add(new CSParameter
		{
			Name = "test",
			Type = "string",
			Optional = true,
			DefaultValue = "random"
		});

		Assert.That(method.ToString(), Is.EqualTo("public void TestMethod(string test = \"random\")"));
	}

	[Test]
	public void MethodsRender()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public void TestMethod()
	{
	}

	internal void TestInternalMethod()
	{
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes[0].Methods, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[0].Classes[0].Methods[0].ToString(), Is.EqualTo("public void TestMethod()"));
			Assert.That(_generator.Namespaces[0].Classes[0].Methods[1].ToString(), Is.EqualTo("internal void TestInternalMethod()"));
		});
	}

	[Test]
	public void InternalMethodsIsExcluded()
	{
		_generator.Exclude(CSAccessModifier.Internal);

		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public void TestMethod()
	{
	}

	internal void TestInternalMethod()
	{
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes[0].Methods, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Methods[0].ToString(), Is.EqualTo("public void TestMethod()"));
		});
	}

	[Test]
	public void ParsedMethodRendersWithParameters()
	{
		_generator.Exclude(CSAccessModifier.Internal);

		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public void TestMethod(string str)
	{
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes[0].Methods, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Methods[0].ToString(), Is.EqualTo("public void TestMethod(string str)"));
		});
	}
}