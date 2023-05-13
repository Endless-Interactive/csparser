namespace CSParser.UnitTests;

public class ExclusionsTest
{
	private CSExclusions _exclusions;

	[SetUp]
	public void Setup()
	{
		_exclusions = new CSExclusions();

		_exclusions.Modifiers.Add(CSAccessModifier.Internal);
		_exclusions.Classes.Add("^TestClass$");
		_exclusions.Namespaces.Add("^TestNamespace$");
		_exclusions.Methods.Add("^TestMethod$");
	}

	[Test]
	public void NamespaceExcluded()
	{
		Assert.That(_exclusions.IsNamespaceExcluded("TestNamespace"));
	}

	[Test]
	public void NamespaceIsNotExcluded()
	{
		Assert.That(_exclusions.IsNamespaceExcluded("Testnamespace"), Is.EqualTo(false));
	}

	[Test]
	public void ClassExcludedByName()
	{
		var @class = new CSClass
		{
			Name = "TestClass",
			AccessModifier = CSAccessModifier.Public
		};

		Assert.That(_exclusions.IsClassExcluded(@class));
	}

	[Test]
	public void ClassExcludedByModifier()
	{
		var @class = new CSClass
		{
			Name = "TestClass",
			AccessModifier = CSAccessModifier.Internal
		};

		Assert.That(_exclusions.IsClassExcluded(@class));
	}

	[Test]
	public void AccessModifierIsNotExcluded()
	{
		Assert.That(_exclusions.IsAccessModifierExcluded(CSAccessModifier.Public), Is.EqualTo(false));
	}

	[Test]
	public void PropertyIsExcluded()
	{
		var property = new CSProperty
		{
			Name = "TestProperty",
			AccessModifier = CSAccessModifier.Internal
		};

		Assert.That(_exclusions.IsPropertyExcluded(property));
	}

	[Test]
	public void PropertyIsNotExcluded()
	{
		var property = new CSProperty
		{
			Name = "TestProperty",
			AccessModifier = CSAccessModifier.Public
		};

		Assert.That(_exclusions.IsPropertyExcluded(property), Is.EqualTo(false));
	}

	[Test]
	public void FieldIsExcluded()
	{
		var field = new CSField
		{
			Name = "TestField",
			AccessModifier = CSAccessModifier.Internal
		};

		Assert.That(_exclusions.IsFieldExcluded(field));
	}

	[Test]
	public void FieldIsNotExcluded()
	{
		var field = new CSField
		{
			Name = "TestField",
			AccessModifier = CSAccessModifier.Public
		};

		Assert.That(_exclusions.IsFieldExcluded(field), Is.EqualTo(false));
	}

	[Test]
	public void MethodIsExcludedByName()
	{
		var method = new CSMethod
		{
			Name = "TestMethod",
			AccessModifier = CSAccessModifier.Public
		};

		Assert.That(_exclusions.IsMethodExcluded(method));
	}

	[Test]
	public void MethodIsExcludedByModifier()
	{
		var method = new CSMethod
		{
			Name = "TestMethod",
			AccessModifier = CSAccessModifier.Internal
		};

		Assert.That(_exclusions.IsMethodExcluded(method));
	}
}