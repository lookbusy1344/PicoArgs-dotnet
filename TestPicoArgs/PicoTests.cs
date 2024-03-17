using PicoArgs_dotnet;

namespace TestPicoArgs;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class PicoTests
{
	[Fact(DisplayName = "Basic test")]
	public void BasicTest()
	{
		var pico = new PicoArgs("--help --another something");

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
		var pico = new PicoArgs("--help --another something");

		var another = pico.GetParamOpt("--another");
		var missing = pico.GetParamOpt("--missing");

		Assert.Equal("something", another);
		Assert.Null(missing);
	}

	[Fact(DisplayName = "GetMultipleParams test")]
	public void MultipleTest()
	{
		var expected = new string[] { "file.txt", "another.txt", "again.txt" };
		var pico = new PicoArgs("-f file.txt --junk xxx --file another.txt -f again.txt");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.False(pico.IsEmpty);
	}

	[Fact(DisplayName = "GetParam - parameter not found")]
	public void NoParamFound()
	{
		var pico = new PicoArgs("-f file.txt");

		// this should throw an exception
		Helpers.AssertPicoThrows(() =>
		{
			var files = pico.GetParam("--something");
		}, "GetParam() should throw when param missing", 10);
	}

	[Fact(DisplayName = "Using equals and complex value")]
	public void ComplexValue()
	{
		var expected = new string[] { "file.txt", "-something=else" };
		var pico = new PicoArgs("--file=file.txt --file=-something=else");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Valid quoted values")]
	public void QuotedValues()
	{
		var expected = new string[] { "item1", "item2", "item3" };
		//var pico = new PicoArgs("--file=item1 --file=\"item2\" --file='item3'"); ** this does not parse as a single string!
		var pico = new PicoArgs(["--file=item1", "--file=\"item2\"", "--file='item3'"]);

		var files = pico.GetMultipleParams("--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Invalid quoted values")]
	public void InvalidQuotedValues()
	{
		// these quotes do not match, so are not parsed
		var expected = new string[] { "\"item1'", "'item2\"" };
		var pico = new PicoArgs(["--file=\"item1'", "--file='item2\""]);

		var files = pico.GetMultipleParams("--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Using equals and complex value, when not enabled")]
	public void ComplexValueFail()
	{
		//var expected = new string[] { "file.txt", "-something=else" };
		var pico = new PicoArgs("--file=file.txt --file=-something=else", false);

		var files = pico.GetMultipleParams("-f", "--file");

		Assert.Empty(files);
	}

	[Fact(DisplayName = "Leftover parameter")]
	public void LeftoverParam()
	{
		var pico = new PicoArgs("-f file.txt --something");
		var something = pico.Contains("--something");

		Assert.True(something);
		Assert.False(pico.IsEmpty);

		// this should throw an exception
		Helpers.AssertPicoThrows(() =>
		{
			pico.Finished();
		}, "Finished() should throw when parameters are leftover", 60);
	}

	[Fact(DisplayName = "GetCommand test")]
	public void GetCommand()
	{
		var pico = new PicoArgs("--file file.txt -v PRINT");

		var verbose = pico.Contains("-v", "--verbose");
		var file = pico.GetParam("-f", "--file");
		var print = pico.GetCommand();
		pico.Finished();

		Assert.True(verbose);
		Assert.Equal("file.txt", file);
		Assert.Equal("PRINT", print);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Missing command")]
	public void MissingCommand()
	{
		var pico = new PicoArgs("--file file.txt -v");

		var verbose = pico.Contains("-v", "--verbose");
		var file = pico.GetParam("-f", "--file");

		Assert.True(verbose);
		Assert.Equal("file.txt", file);

		// this should throw an exception
		Helpers.AssertPicoThrows(() =>
		{
			_ = pico.GetCommand();
		}, "GetCommand() should throw when no command present", 40);
	}

	[Fact(DisplayName = "GetCommandOpt test")]
	public void GetCommandOpt()
	{
		var pico = new PicoArgs("--file file.txt -v PRINT");

		var verbose = pico.Contains("-v", "--verbose");
		var file = pico.GetParam("-f", "--file");
		var print = pico.GetCommand();
		var notfound = pico.GetCommandOpt();    // a command that is not present, so should be null
		pico.Finished();

		Assert.True(verbose);
		Assert.Equal("file.txt", file);
		Assert.Equal("PRINT", print);
		Assert.Null(notfound);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Regex splitting")]
	public void RegexSplitting()
	{
		var expected = new string[] { "once", "upon", "a", "time", "in Hollywood" };
		var pico = new PicoArgs("once upon a time \"in Hollywood\"");

		var content = pico.UnconsumedArgs.Select(a => a.Key).ToArray();
		var match = Helpers.CompareNames(expected, content);
		Assert.True(match);
	}

	[Fact(DisplayName = "Key / value splitting in detail")]
	public void KVCheck()
	{
		var expected = new KeyValue[] { new("--file", "file1.txt"), new("--print", null),
			new("something", null), new("--verbose", "yes") };
		var pico = new PicoArgs("--file=file1.txt --print something --verbose=yes");

		var match = expected.SequenceEqual(pico.UnconsumedArgs);

		Assert.Equal(expected.Length, pico.UnconsumedArgs.Count);
		Assert.True(match);
	}

	[Fact(DisplayName = "PicoArgsDisposable check")]
	public void DisposalCheck()
	{
#pragma warning disable CA2000 // Dispose objects before losing scope
		var pico = new PicoArgsDisposable("-f file.txt --something");
		var something = pico.Contains("--something");

		Helpers.AssertPicoThrows(() =>
		{
			pico.Dispose();
		}, "Dispose() should throw when parameters are leftover", 60);
#pragma warning restore CA2000 // Dispose objects before losing scope
	}

	[Fact(DisplayName = "Unwanted switch value")]
	public void UnwantedSwitchValue()
	{
		// here "--verbose" is acceptable but "--verbose=yes" is not
		var pico = new PicoArgs("--verbose=yes --something");

		var something = pico.Contains("--something");
		var notpresent = pico.Contains("--notpresent");

		Assert.True(something);
		Assert.False(notpresent);

		// this should throw an exception
		Helpers.AssertPicoThrows(() =>
		{
			var verbose = pico.Contains("-v", "--verbose");
		}, "Contains() should throw when unwanted switch value is present", 80);
	}
}
