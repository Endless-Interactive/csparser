namespace CSParser.UnitTests;

public class XMLTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void XMLToString()
	{
		var xml = new XMLDoc
		{
			Summary = "Test summary"
		};

		Assert.That(xml.ToString(), Is.EqualTo("Test summary"));
	}

	[Test]
	public void ClassXML()
	{
		_generator.AddCode(@"
namespace TestNamespace;

/// <summary>
/// Test class
/// </summary>
public class Test
{
}
");

		Assert.That(_generator.Namespaces[0].Classes[0].XmlDoc.Summary, Is.EqualTo("Test class"));
	}

	[Test]
	public void RemarksXML()
	{
		_generator.AddCode(@"
namespace TestNamespace;

/// <remarks>
/// Test remarks
/// </remarks>
public class Test
{
}
");
		Assert.That(_generator.Namespaces[0].Classes[0].XmlDoc.Remarks, Is.EqualTo("Test remarks"));
	}

	[Test]
	public void MethodXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[0].XmlDoc.Summary, Is.EqualTo("Test method"));
	}

	[Test]
	public void MethodReturnXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[0].XmlDoc.Returns, Is.EqualTo("A + B"));
	}

	[Test]
	public void MethodParamXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes[0].Methods[0].XmlDoc.Parameters[0].Name, Is.EqualTo("a"));
			Assert.That(_generator.Namespaces[0].Classes[0].Methods[0].XmlDoc.Parameters[1].Name, Is.EqualTo("b"));
		});
	}

	[Test]
	public void MethodSeeCrefXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[1].XmlDoc.See[0].Cref, Is.EqualTo("TestMethod"));
	}

	[Test]
	public void MethodSeeHrefXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[3].XmlDoc.See[0].Href, Is.EqualTo("https://google.com"));
		Assert.That(_generator.Namespaces[0].Classes[0].Methods[3].XmlDoc.See[0].Description, Is.EqualTo("Google"));
	}

	[Test]
	public void MethodSeeLangWordXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[5].XmlDoc.See[0].LangWord, Is.EqualTo("test"));
	}

	[Test]
	public void MethodValueXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[6].XmlDoc.Value, Is.EqualTo("test"));
	}

	[Test]
	public void MethodExampleXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[7].XmlDoc.Examples[0],
			Is.EqualTo("An example\r\n\t     <code>TestMethod8(1, 2)</code>"));
	}

	[Test]
	public void MethodExceptionsXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[8].XmlDoc.Exceptions, Has.Count.EqualTo(2));
		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes[0].Methods[8].XmlDoc.Exceptions[0].Cref, Is.EqualTo("System.Exception"));
		});
	}

	[Test]
	public void MethodSeeAlsoCrefXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[2].XmlDoc.SeeAlso[0].Cref, Is.EqualTo("TestMethod2"));
	}

	[Test]
	public void MethodSeeAlsoHrefXML()
	{
		_generator.AddFile("../../../Test/MethodXML.cs");

		Assert.That(_generator.Namespaces[0].Classes[0].Methods[4].XmlDoc.SeeAlso[0].Href, Is.EqualTo("https://google.com"));
		Assert.That(_generator.Namespaces[0].Classes[0].Methods[4].XmlDoc.SeeAlso[0].Description, Is.EqualTo("Google"));
	}

	[Test]
	public void PropertyXML()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	/// <summary>
	/// Test property
	/// </summary>
	public int TestProperty { get; set; }
}");

		Assert.That(_generator.Namespaces[0].Classes[0].Properties[0].XmlDoc.Summary, Is.EqualTo("Test property"));
	}

	[Test]
	public void FieldXML()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	/// <summary>
	/// Test field
	/// </summary>
	public int TestField;
}");
		Assert.That(_generator.Namespaces[0].Classes[0].Fields[0].XmlDoc.Summary, Is.EqualTo("Test field"));
	}
}