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
		public string TestField;
	}

	public partial class TestClass
	{
		public string TestField2;
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Classes[0].IsPartial, Is.EqualTo(true));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public void SplitPartialClassRenders()
	{
		_generator.AddCode(@"
namespace Test
{
	public partial class TestClass
	{
		public string TestField;
	}
}
");

		_generator.AddCode(@"
namespace Test
{
	public partial class TestClass
	{
		public string TestField2;
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Classes[0].IsPartial, Is.EqualTo(true));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public void SplitPartialClassRendersWithOtherClasses()
	{
		_generator.AddCode(@"
namespace Test
{
	public partial class TestClass
	{
		public string TestField;
	}

	public class Random {
		public float FloatField;
		public string TestProperty3 { get; set; }

		public void TestMethod3()
		{
		}
	}
}
");

		_generator.AddCode(@"
namespace Test
{
	public partial class TestClass
	{
		public string TestField2;
	}

	public class AlsoRandom {
		public int IntField;
		public string TestProperty2 { get; set; }

		public void TestMethod2()
		{
		}
	}
}
");

		_generator.AddCode(@"
namespace Test
{
	public partial class TestClass
	{
		public string TestProperty { get; set; }

		public void TestMethod()
		{
		}
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(3));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Classes[0].IsPartial, Is.EqualTo(true));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Methods, Has.Count.EqualTo(1));
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
		public string TestField;
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
	public void ClassInsideClassExists()
	{
		_generator.AddCode(@"
namespace Test
{
	public class TestClass
	{
		public class SubClass
		{
			public string TestField;
		}
	}
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Classes[1].Name, Is.EqualTo("SubClass"));
			Assert.That(_generator.Namespaces[0].Classes[1].ParentClass, Is.EqualTo("TestClass"));
		});
	}

	[Test]
	public void SubclassOnlyHasField()
	{
		_generator.AddCode(@"
namespace Test
{
	public class TestClass
	{
		public class SubClass
		{
			public string TestField;
		}
	}
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Classes[0].Fields, Has.Count.EqualTo(0));
			Assert.That(_generator.Namespaces[0].Classes[1].Name, Is.EqualTo("SubClass"));
			Assert.That(_generator.Namespaces[0].Classes[1].Fields, Has.Count.EqualTo(1));
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
		public string TestField;
	}

	public class AnotherClass
	{
		public string AnotherField;
	}	
}
");

		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
		Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
	}
}