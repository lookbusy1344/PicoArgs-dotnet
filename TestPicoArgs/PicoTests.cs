using PicoArgs_dotnet;

namespace TestPicoArgs;

#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class PicoTests
{
	[Fact(DisplayName = "Basic test", Timeout = 1000)]
	public void BasicTest()
	{
		var pico = new PicoArgs("--help --another something");

		var help = pico.Contains("-h", "-?", "--help");
		var absent = pico.Contains("-a", "-b", "--absent");
		var another = pico.GetParamOpt("--another") ?? "missing";

		Assert.True(help);
		Assert.False(absent);
		Assert.Equal("something", another);
	}

	[Fact(DisplayName = "Multiple matches", Timeout = 1000)]
	public void MultipleTest()
	{
		var expected = new string[] { "file.txt", "another.txt", "again.txt" };
		var pico = new PicoArgs("-f file.txt --junk xxx --file another.txt -f again.txt");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
	}

	[Fact(DisplayName = "Parameter not found", Timeout = 1000)]
	public void NoParamFound()
	{
		var pico = new PicoArgs("-f file.txt");

		// this should throw an exception
		Helpers.AssertThrows<PicoArgsException>(() =>
		{
			var files = pico.GetParam("--something");
		}, "GetParam should throw when param missing");
	}

	[Fact(DisplayName = "Using equals and complex value", Timeout = 1000)]
	public void ComplexValue()
	{
		var expected = new string[] { "file.txt", "-something=else" };
		var pico = new PicoArgs("--file=file.txt --file=-something=else");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
	}

	[Fact(DisplayName = "Using equals and complex value, when not enabled", Timeout = 1000)]
	public void ComplexValueFail()
	{
		//var expected = new string[] { "file.txt", "-something=else" };
		var pico = new PicoArgs("--file=file.txt --file=-something=else", false);

		var files = pico.GetMultipleParams("-f", "--file");

		Assert.Empty(files);
	}

	[Fact(DisplayName = "Leftover parameter", Timeout = 1000)]
	public void LeftoverParam()
	{
		var pico = new PicoArgs("-f file.txt --something");
		var something = pico.Contains("--something");

		Assert.True(something);

		// this should throw an exception
		Helpers.AssertThrows<PicoArgsException>(() =>
		{
			pico.Finished();
		}, "Finished should throw when parameters are leftover");
	}

	// test GetCommand()
}
