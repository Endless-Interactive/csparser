﻿namespace CSParser.UnitTests;

public class PropertyTests
{
	private Generator _generator;

	[SetUp]
	public void Setup()
	{
		_generator = new Generator();
	}

	[Test]
	public void PropertyRenders()
	{
		var property = new CSProperty
		{
			Name = "TestProperty",
			Type = "string",
			AccessModifier = CSAccessModifier.Public
		};

		Assert.That(property.ToString(), Is.EqualTo("public string TestProperty"));
	}

	[Test]
	public void PropertyRendersWithModifiers()
	{
		var property = new CSProperty
		{
			Name = "TestProperty",
			Type = "string"
		};

		property.SetModifiers("private static");

		Assert.That(property.ToString(), Is.EqualTo("private static string TestProperty"));
	}

	[Test]
	public void PropertyRendersWithModifiersAndValue()
	{
		var property = new CSProperty
		{
			Name = "TestProperty",
			Type = "string",
			DefaultValue = "test"
		};

		property.SetModifiers("private static");

		Assert.That(property.ToString(), Is.EqualTo("private static string TestProperty"));
	}

	[Test]
	public void PropertyRendersWithCode()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestProperty {get; set;}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties[0].ToString(), Is.EqualTo("public System.String TestProperty"));
		});
	}

	[Test]
	public void PropertyRendersWithDefaultValueWithCode()
	{
		var value = "\"test\"";

		_generator.AddCode($@"
namespace TestNamespace;

public class Test
{{
	public string TestProperty {{get; set;}} = {value};
}}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties[0].ToString(), Is.EqualTo("public System.String TestProperty"));
		});
	}

	[Test]
	public void PropertyDoesNotRenderExcluded()
	{
		_generator.Exclude(CSAccessModifier.Internal);

		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestField;
	internal string TestProperty {get; set;}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(0));
		});
	}

	[Test]
	public void PropertyHasDefaultGetterSetter()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestProperty {get; set;}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(1));
			var property = _generator.Namespaces[0].Classes[0].Properties[0];
			Assert.That(property.Getter, Is.Not.Null);
			Assert.That(property.Setter, Is.Not.Null);

			Assert.That(property.Getter?.ToString(), Is.EqualTo(""));
			Assert.That(property.Setter?.ToString(), Is.EqualTo(""));
		});
	}

	[Test]
	public void PropertyHasPrivateSetter()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestProperty {get; private set;}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(1));
			var property = _generator.Namespaces[0].Classes[0].Properties[0];
			Assert.That(property.Getter, Is.Not.Null);
			Assert.That(property.Setter, Is.Not.Null);

			Assert.That(property.Getter?.ToString(), Is.EqualTo(""));
			Assert.That(property.Setter?.ToString(), Is.EqualTo("private"));
		});
	}

	[Test]
	public void PropertyHasProtectedGetterPrivateSetter()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestProperty {protected get; private set;}
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(1));
			var property = _generator.Namespaces[0].Classes[0].Properties[0];
			Assert.That(property.Getter, Is.Not.Null);
			Assert.That(property.Setter, Is.Not.Null);

			Assert.That(property.Getter?.ToString(), Is.EqualTo("protected"));
			Assert.That(property.Setter?.ToString(), Is.EqualTo("private"));
		});
	}

	[Test]
	public void PropertyOnlyHasGetter()
	{
		_generator.AddCode(@"
namespace TestNamespace;

public class Test
{
	public string TestProperty => ""test"";
}
");

		Assert.Multiple(() =>
		{
			Assert.That(_generator.Namespaces, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes, Has.Count.EqualTo(1));
			Assert.That(_generator.Namespaces[0].Classes[0].Properties, Has.Count.EqualTo(1));
			var property = _generator.Namespaces[0].Classes[0].Properties[0];
			Assert.That(property.Getter, Is.Not.Null);
			Assert.That(property.Setter, Is.Null);
		});
	}
}