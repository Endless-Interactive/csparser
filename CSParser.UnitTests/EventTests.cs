namespace CSParser.UnitTests;

public class EventTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void DelegateRenders()
	{
		_generator.AddCode(@"
namespace TestNamespace;
public delegate void TestEventHandler(object sender);
");
		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Delegates, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Delegates[0].Name, Is.EqualTo("TestEventHandler"));
			Assert.That(_generator.Namespaces[0].Delegates[0].Parameters, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Delegates[0].Parameters[0].Name, Is.EqualTo("sender"));
			Assert.That(_generator.Namespaces[0].Delegates[0].Parameters[0].Type, Is.EqualTo("System.Object"));
		});
	}

	[Test]
	public void EventRenders()
	{
		_generator.AddCode(@"
namespace TestNamespace;
public delegate void TestEventHandler(object sender);
public class TestClass
{
	public event TestEventHandler TestEvent;
}
");
		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Classes[0].Events, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Events[0].Name, Is.EqualTo("TestEvent"));
			Assert.That(_generator.Namespaces[0].Classes[0].Events[0].Delegate.Name, Is.EqualTo("TestEventHandler"));
		});
	}
}