namespace CSParser.UnitTests;

public class InheritTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void InheritTest()
	{
		_generator.AddCode(@"
namespace RandomNamespace.SomethingRandom;

public class NewClass
{
	public int SomeProperty { get; set; }
}
");
		_generator.AddCode(@"
namespace TestNamespace;

public interface ITestInterface
{
	void TestMethod();
	int TestProperty { get; set; }
}
");
		_generator.AddCode(@"
using RandomNamespace.SomethingRandom;
namespace TestNamespace;

public class DerivedClass : NewClass, ITestInterface
{
    public void TestMethod()
    {
    }

    public int TestProperty { get; set; }
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[1].Classes[0].Name, Is.EqualTo("DerivedClass"));
			Assert.That(_generator.Namespaces[1].Classes[0].Inherits, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[1].Classes[0].Inherits[0], Is.EqualTo("RandomNamespace.SomethingRandom.NewClass"));
			Assert.That(_generator.Namespaces[1].Classes[0].Inherits[1], Is.EqualTo("TestNamespace.ITestInterface"));
		});
	}
}