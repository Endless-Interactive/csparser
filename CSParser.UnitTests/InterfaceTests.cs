namespace CSParser.UnitTests;

public class InterfaceTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void InterfaceRenders()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public interface ITestInterface
{
	void TestMethod();
	int TestProperty { get; set; }
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Interfaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Interfaces[0].Name, Is.EqualTo("ITestInterface"));
			Assert.That(_generator.Namespaces[0].Interfaces[0].Methods, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Interfaces[0].Properties, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Interfaces[0].Methods[0].Name, Is.EqualTo("TestMethod"));
			Assert.That(_generator.Namespaces[0].Interfaces[0].Properties[0].Name, Is.EqualTo("TestProperty"));
		});
	}
}