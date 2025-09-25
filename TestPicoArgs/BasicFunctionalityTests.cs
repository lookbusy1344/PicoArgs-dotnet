namespace TestPicoArgs;

using PicoArgs_dotnet;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class BasicFunctionalityTests
{
	[Fact(DisplayName = "Basic test")]
	public void BasicTest()
	{
		var pico = SplitArgs.BuildFromSingleString("--help --another something");

		var help = pico.Contains("-h", "-?", "--help");
		var absent = pico.Contains("-a", "-b", "--absent");
		var another = pico.GetParamOpt("--another") ?? "missing";

		Assert.True(help);
		Assert.False(absent);
		Assert.Equal("something", another);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "GetParamOpt test")]
	public void GetParamOptTest()
	{
		var pico = SplitArgs.BuildFromSingleString("--help --another something");

		var another = pico.GetParamOpt("--another");
		var missing = pico.GetParamOpt("--missing");

		Assert.Equal("something", another);
		Assert.Null(missing);
	}

	[Fact(DisplayName = "GetMultipleParams test")]
	public void MultipleTest()
	{
		string[] expected = ["file.txt", "another.txt", "again.txt"];
		var pico = SplitArgs.BuildFromSingleString("-f file.txt --junk xxx --file another.txt -f again.txt");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.False(pico.IsEmpty);
	}

	[Fact(DisplayName = "GetCommand test")]
	public void GetCommand()
	{
		var pico = SplitArgs.BuildFromSingleString("--file file.txt -v PRINT");

		var verbose = pico.Contains("-v", "--verbose");
		var file = pico.GetParam("-f", "--file");
		var print = pico.GetCommand();
		pico.Finished();

		Assert.True(verbose);
		Assert.Equal("file.txt", file);
		Assert.Equal("PRINT", print);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "GetCommand single chars")]
	public void GetCommandSingleChar()
	{
		var pico = SplitArgs.BuildFromSingleString("a b c=something -c=code");

		var code = pico.GetParam("-c", "--code");
		var first = pico.GetCommand();
		var second = pico.GetCommand();
		var third = pico.GetCommand();
		pico.Finished();

		Assert.Equal("code", code);
		Assert.Equal("a", first);
		Assert.Equal("b", second);
		Assert.Equal("c=something", third);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Single and double dash")]
	public void SingleDashSwitch()
	{
		var pico = SplitArgs.BuildFromSingleString("- --hello --");

		var hello = pico.Contains("-h", "--hello");
		var dash = pico.GetCommand(); // a single dash is not a switch or param, so should be the command
		var doubledash = pico.GetCommand();
		pico.Finished();

		Assert.True(hello);
		Assert.Equal("-", dash);
		Assert.Equal("--", doubledash);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "GetCommandOpt test")]
	public void GetCommandOpt()
	{
		var pico = SplitArgs.BuildFromSingleString("--file file.txt -v PRINT");

		var verbose = pico.Contains("-v", "--verbose");
		var file = pico.GetParam("-f", "--file");
		var print = pico.GetCommand();
		var notFound = pico.GetCommandOpt(); // a command that is not present, so should be null
		pico.Finished();

		Assert.True(verbose);
		Assert.Equal("file.txt", file);
		Assert.Equal("PRINT", print);
		Assert.Null(notFound);
		Assert.True(pico.IsEmpty);
	}
}
