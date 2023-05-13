namespace CSParser.UnitTests;

public class NamespaceTests
{
	private Generator _generator;
	
	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void NamespaceExists()
	{
		_generator.AddCode(@"
namespace Test
{
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
	}
	
	[Test]
	public void NamespaceIsExcluded()
	{
		_generator.Exclude(CSExclusions.ExcludeType.Namespace, "^Test$");
		
		_generator.AddCode(@"
namespace Test	
{
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(0));
	}

	[Test]
	public void NamespaceIsNotExcluded()
	{
		_generator.Exclude(CSExclusions.ExcludeType.Namespace, "^Test$");

		_generator.AddCode(@"
namespace Test	
{
}

namespace Test2
{
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
		Assert.That(_generator.Namespaces[0].Namespace, Is.EqualTo("Test2"));
	}

	[Test(Description = "Excludes all namespaces starting with Test")]
	public void ExcludeMultipleNamespaces()
	{
		_generator.Exclude(CSExclusions.ExcludeType.Namespace, "^Test");
		
		_generator.AddCode(@"
namespace Test	
{
}

namespace Test2
{
}

namespace Test3
{
}

namespace RandomTest
{
}
");
		
		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
		Assert.That(_generator.Namespaces[0].Namespace, Is.EqualTo("RandomTest"));
	}
}