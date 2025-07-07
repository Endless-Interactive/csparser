namespace CSParser.UnitTests;

public class EnumTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void EnumRenders()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public enum Test
{
	TestValue,
	TestValue2
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Enums, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Enums[0].Values, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public void EnumTypeRenders()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public enum Test : byte
{
	TestValue,
	TestValue2
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Enums, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Enums[0].ToString(), Is.EqualTo("public enum Test : System.Byte"));
			Assert.That(_generator.Namespaces[0].Enums[0].Values, Has.Count.EqualTo(2));
		});
	}
}