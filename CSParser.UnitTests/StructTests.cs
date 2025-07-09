namespace CSParser.UnitTests;

public class StructTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void StructRenders()
	{
		_generator.AddCode(@"
namespace TestNamespace;
public struct TestStruct
{
	public int TestField;
	public string TestProperty { get; set; }
}
");
		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Structs, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Structs[0].AccessModifier, Is.EqualTo(CSAccessModifier.Public));
			Assert.That(_generator.Namespaces[0].Structs[0].FullModifier, Is.EqualTo("public"));
			Assert.That(_generator.Namespaces[0].Structs[0].Name, Is.EqualTo("TestStruct"));
			Assert.That(_generator.Namespaces[0].Structs[0].Fields, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Structs[0].Fields[0].Name, Is.EqualTo("TestField"));
			Assert.That(_generator.Namespaces[0].Structs[0].Fields[0].Type, Is.EqualTo("System.Int32"));
			Assert.That(_generator.Namespaces[0].Structs[0].Properties, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Structs[0].Properties[0].Name, Is.EqualTo("TestProperty"));
			Assert.That(_generator.Namespaces[0].Structs[0].Properties[0].Type, Is.EqualTo("System.String"));
		});
	}

	[Test]
	public void NestedStructRenders()
	{
		_generator.AddCode(@"
namespace TestNamespace;
public class TestClass {
	public struct TestStruct
	{
		public int TestField;
	}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Structs, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Structs[0].Name, Is.EqualTo("TestStruct"));
			Assert.That(_generator.Namespaces[0].Structs[0].AccessModifier, Is.EqualTo(CSAccessModifier.Public));
			Assert.That(_generator.Namespaces[0].Structs[0].FullModifier, Is.EqualTo("public"));
			Assert.That(_generator.Namespaces[0].Structs[0].ParentClass, Is.EqualTo("TestClass"));
			Assert.That(_generator.Namespaces[0].Structs[0].Fields, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Structs[0].Fields[0].Name, Is.EqualTo("TestField"));
			Assert.That(_generator.Namespaces[0].Structs[0].Fields[0].Type, Is.EqualTo("System.Int32"));
		});
	}
}