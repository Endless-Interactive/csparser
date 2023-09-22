namespace CSParser.UnitTests;

public class ObjectTest
{
	private CSObject _csObject;

	[SetUp]
	public void Setup()
	{
		_csObject = new CSObject
		{
			Name = "test",
			AccessModifier = CSAccessModifier.Public
		};
	}

	[Test]
	public void ObjectRenders()
	{
		Assert.That(_csObject.ToString(), Is.EqualTo("public test"));
	}

	[Test]
	public void ObjectRendersWithModifier()
	{
		_csObject.SetModifiers("public static");

		Assert.That(_csObject.ToString(), Is.EqualTo("public static test"));
	}

	[Test]
	public void ObjectRendersWhenSettingProtectedModifier()
	{
		_csObject.SetModifiers("protected");

		Assert.That(_csObject.ToString(), Is.EqualTo("protected test"));
	}

	[Test]
	public void ObjectRendersWhenSettingInternalModifier()
	{
		_csObject.SetModifiers("internal");

		Assert.That(_csObject.ToString(), Is.EqualTo("internal test"));
	}

	[Test]
	public void ObjectRendersWhenSettingModifiers()
	{
		_csObject.SetModifiers("private static");

		Assert.That(_csObject.ToString(), Is.EqualTo("private static test"));
	}

	[Test]
	public void ObjectRendersWhenSettingModifiersProtectedInternalStatic()
	{
		_csObject.SetModifiers("protected internal static");

		Assert.That(_csObject.ToString(), Is.EqualTo("protected internal static test"));
	}

	[Test]
	public void ObjectRendersWhenSettingModifiersPrivateProtectedStatic()
	{
		_csObject.SetModifiers("private protected static");

		Assert.That(_csObject.ToString(), Is.EqualTo("private protected static test"));
	}

	[Test]
	public void ObjectRendersWhenSettingModifiersAsync()
	{
		_csObject.SetModifiers("private async static");

		Assert.That(_csObject.ToString(), Is.EqualTo("private async static test"));
	}

	[Test]
	public void ObjectRendersWhenSettingInvalidAccessModifier()
	{
		Assert.Throws<Exception>(() => _csObject.SetModifiers("provite"), "Unknown access modifier: provite");
	}

	[Test]
	public void ObjectRendersFullModifier()
	{
		Assert.That(_csObject.FullModifier, Is.EqualTo("public"));
	}

	[Test]
	public void ObjectRendersWithEmptyModifier()
	{
		_csObject = new CSObject
		{
			Name = "test",
			AccessModifier = CSAccessModifier.Private
		};

		_csObject.SetModifiers("");

		Assert.That(_csObject.ToString(), Is.EqualTo("private test"));
	}

	[Test]
	public void GetModifier()
	{
		Assert.That(CSObject.GetModifier(CSAccessModifier.Public), Is.EqualTo("public"));
	}

	[Test]
	public void GetModifierProtectedInternal()
	{
		Assert.That(CSObject.GetModifier(CSAccessModifier.ProtectedInternal), Is.EqualTo("protected internal"));
	}

	[Test]
	public void GetModifierPrivateProtected()
	{
		Assert.That(CSObject.GetModifier(CSAccessModifier.PrivateProtected), Is.EqualTo("private protected"));
	}

	[Test]
	public void SingleModifier()
	{
		_csObject = new CSObject
		{
			Name = "test"
		};

		_csObject.SetModifiers("public static");
		Assert.That(_csObject.Modifiers, Is.EqualTo(CSModifier.Static));
	}

	[Test]
	public void MultipleModifiers()
	{
		_csObject = new CSObject
		{
			Name = "test"
		};

		_csObject.SetModifiers("public static async");
		Assert.That(_csObject.Modifiers, Is.EqualTo(CSModifier.Static | CSModifier.Async));
	}
}