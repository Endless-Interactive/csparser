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

	[Test]
	public void MethodTypeHasFullNameSpace()
	{
		_generator.AddCode("""

		                   namespace RandomNamespace.SomethingRandom;

		                   public class NewClass
		                   {
		                   	public int SomeProperty { get; set; }
		                   }

		                   """);
		_generator.AddCode("""

		                   using RandomNamespace.SomethingRandom;
		                   namespace TestNamespace;

		                   public interface IClass
		                   {
		                   void TestMethod(NewClass data);
		                   }

		                   """);

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[1].Interfaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[1].Interfaces[0].Name, Is.EqualTo("IClass"));
			Assert.That(_generator.Namespaces[1].Interfaces[0].Methods, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[1].Interfaces[0].Methods[0].Parameters, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[1].Interfaces[0].Methods[0].Parameters[0].Type,
				Is.EqualTo("RandomNamespace.SomethingRandom.NewClass"));
		});
	}
}