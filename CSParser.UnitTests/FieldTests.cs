namespace CSParser.UnitTests;

public class FieldTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void FieldRenders()
	{
		var field = new CSField
		{
			Name = "TestField",
			Type = "string",
			AccessModifier = CSAccessModifier.Public
		};

		Assert.That(field.ToString(), Is.EqualTo("public string TestField"));
	}

	[Test]
	public void FieldRendersWithModifiers()
	{
		var field = new CSField
		{
			Name = "TestField",
			Type = "string"
		};

		field.SetModifiers("private static");

		Assert.That(field.ToString(), Is.EqualTo("private static string TestField"));
	}

	[Test]
	public void FieldRendersWithModifiersAndValue()
	{
		var field = new CSField
		{
			Name = "TestField",
			Type = "string",
			DefaultValue = "test"
		};

		field.SetModifiers("private static");

		Assert.That(field.ToString(), Is.EqualTo("private static string TestField = \"test\""));
	}

	[Test]
	public void FieldRendersWithCode()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestField;
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields[0].ToString(), Is.EqualTo("public System.String TestField"));
		});
	}

	[Test]
	public void FieldRendersWithDefaultValueWithCode()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestField = ""test"";
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields[0].ToString(),
				Is.EqualTo("public System.String TestField = test"));
		});
	}

	[Test]
	public void FieldDoesNotRenderExcluded()
	{
		_generator.Exclude(CSAccessModifier.Internal);

		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string Test;
	internal string TestField;
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields, Has.Count.EqualTo(1));
		});
	}
}