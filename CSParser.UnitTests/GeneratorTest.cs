﻿namespace CSParser.UnitTests;

public class GeneratorTest
{
	private Generator _generator;
	private TextWriter _originalOutput;
	private StringWriter _output;

	[SetUp]
	public void Setup()
	{
		Generator.Debug = false;

		_output = new StringWriter();
		_originalOutput = Console.Out;

		_generator = new Generator();

		Console.SetOut(_output);
	}

	[TearDown]
	public void TearDown()
	{
		Console.SetOut(_originalOutput);

		_output.Dispose();
	}

	[Test]
	public void GenerateWithDirectory()
	{
		_generator = new Generator("../../../Test");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[0].Namespace, Is.EqualTo("TestNamespace"));
			Assert.That(_generator.Namespaces[0].Interfaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[1].Namespace, Is.EqualTo("TestNamespace.SubTest"));
		});
	}

	[Test]
	public void GenerateWithCode()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
public string TestField;
}
");
		Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
	}

	[Test]
	public void GeneratorWithFile()
	{
		_generator.AddFile("../../../Test/Test.cs");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Namespace, Is.EqualTo("TestNamespace"));
		});
	}

	[Test]
	public void GeneratorWithDirectory()
	{
		_generator.AddDirectory("../../../Test");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(2));
			Assert.That(_generator.Namespaces[0].Namespace, Is.EqualTo("TestNamespace"));
			Assert.That(_generator.Namespaces[1].Namespace, Is.EqualTo("TestNamespace.SubTest"));
		});
	}

	[Test]
	public void GeneratorWithDirectoryExcludingNamespace()
	{
		_generator.Exclude(CSAccessModifier.Internal);

		_generator.AddFile("../../../Test/Test.cs");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Name, Is.EqualTo("Test"));
		});
	}

	[Test]
	public void GeneratorDoesNotLog()
	{
		_generator.Exclude(CSExclusions.ExcludeType.Namespace, "^Test$");

		_generator.AddCode(@"
namespace Test	
{
}

namespace Test2
{
	public class TestClass
	{
		public string TestField;
	}
}
");

		Assert.That(_output.ToString(), Is.Empty);
	}

	[Test]
	public void GeneratorDoesDebugLog()
	{
		Generator.Debug = true;
		_generator.Exclude(CSExclusions.ExcludeType.Namespace, "^Test$");

		_generator.AddCode(@"
namespace Test	
{
}

namespace Test2
{
	public class TestClass
	{
		public string TestField;
	}
}
");

		Assert.That(_output.ToString(), Is.EqualTo("Excluding namespace Test\r\n"));
	}
}