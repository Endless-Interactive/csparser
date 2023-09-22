namespace CSParser.UnitTests;

public class ClassTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void PartialClassRenders()
	{
		_generator.AddCode(@"
namespace Test
{
	public partial class TestClass
	{
	}

	public partial class TestClass
	{
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Classes[0].IsPartial, Is.EqualTo(true));
		});
	}

	[Test]
	public void ClassExists()
	{
		_generator.AddCode(@"
namespace Test
{
	public class TestClass
	{
	}
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
		});
	}

	[Test]
	public void ClassIsExcluded()
	{
		_generator.Exclude(CSExclusions.ExcludeType.Class, "^TestClass$");

		_generator.AddCode(@"
namespace Test
{
	public class TestClass
	{
	}
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
		Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(0));
	}
}