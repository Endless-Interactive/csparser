namespace CSParser.UnitTests;

public class ParameterTest
{
	private CSParameter _parameter;

	[SetUp]
	public void Setup()
	{
		_parameter = new CSParameter
		{
			Name = "test",
			Type = "string"
		};
	}

	[Test]
	public void ParameterRenders()
	{
		Assert.That(_parameter.ToString(), Is.EqualTo("string test"));
	}

	[Test]
	public void ParameterWithDefaultRenders()
	{
		_parameter.Optional = true;
		_parameter.DefaultValue = "text";

		Assert.That(_parameter.ToString(), Is.EqualTo("string test = \"text\""));
	}
}