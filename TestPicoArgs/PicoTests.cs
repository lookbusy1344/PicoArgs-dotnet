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
		var expected = new string[] { "file.txt", "another.txt", "again.txt" };
		var pico = SplitArgs.BuildFromSingleString("-f file.txt --junk xxx --file another.txt -f again.txt");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.False(pico.IsEmpty);
	}

	[Fact(DisplayName = "GetParam - parameter not found")]
	public void NoParamFound()
	{
		var pico = SplitArgs.BuildFromSingleString("-f file.txt");

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			var files = pico.GetParam("--something");
		}, "GetParam() should throw when param missing", 10);
	}

	[Fact(DisplayName = "Using equals and complex value")]
	public void ComplexValue()
	{
		var expected = new string[] { "file.txt", "-something=else" };
		var pico = SplitArgs.BuildFromSingleString("--file=file.txt --file=-something=else");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Valid quoted values")]
	public void QuotedValues()
	{
		var expected = new string[] { "item1", "item2", "item3", "item 4" };
		var pico = SplitArgs.BuildFromSingleString("--file=item1 --file=\"item2\" --file='item3' --file=\"item 4\"");

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
		var pico = new PicoArgs(new[] { "--file=\"item1'", "--file='item2\"" });

		var files = pico.GetMultipleParams("--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Using equals and complex value, when not enabled")]
	public void ComplexValueFail()
	{
		//var expected = new string[] { "file.txt", "-something=else" };
		var pico = SplitArgs.BuildFromSingleString("--file=file.txt --file=-something=else", false);

		var files = pico.GetMultipleParams("-f", "--file");

		Assert.Empty(files);
	}

	[Fact(DisplayName = "Leftover parameter")]
	public void LeftoverParam()
	{
		var pico = SplitArgs.BuildFromSingleString("-f file.txt --something");
		var something = pico.Contains("--something");

		Assert.True(something);
		Assert.False(pico.IsEmpty);

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			pico.Finished();
		}, "Finished() should throw when parameters are leftover", 60);
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

	[Fact(DisplayName = "Missing command")]
	public void MissingCommand()
	{
		var pico = SplitArgs.BuildFromSingleString("--file file.txt -v");

		var verbose = pico.Contains("-v", "--verbose");
		var file = pico.GetParam("-f", "--file");

		Assert.True(verbose);
		Assert.Equal("file.txt", file);

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			_ = pico.GetCommand();
		}, "GetCommand() should throw when no command present", 40);
	}

	[Fact(DisplayName = "GetCommandOpt test")]
	public void GetCommandOpt()
	{
		var pico = SplitArgs.BuildFromSingleString("--file file.txt -v PRINT");

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

	[Fact(DisplayName = "Argument splitting")]
	public void ArgumentSplitting()
	{
		var expected = new string[] { "once", "upon", "a", "time", "in Hollywood" };
		var pico = SplitArgs.BuildFromSingleString("once upon a time \"in Hollywood\"");

		var content = pico.UnconsumedArgs.Select(a => a.Key).ToArray();
		var match = Helpers.CompareNames(expected, content);
		Assert.True(match);
	}

	[Fact(DisplayName = "Key / value splitting in detail")]
	public void KVCheck()
	{
		var expected = new KeyValue[] { new("--file", "file1.txt"), new("--print", null),
			new("something", null), new("--verbose", "yes") };
		var pico = SplitArgs.BuildFromSingleString("--file=file1.txt --print something --verbose=yes");

		var match = expected.SequenceEqual(pico.UnconsumedArgs);

		Assert.Equal(expected.Length, pico.UnconsumedArgs.Count);
		Assert.True(match);
	}

	[Fact(DisplayName = "PicoArgsDisposable check")]
	public void DisposalCheck()
	{
#pragma warning disable CA2000 // Dispose objects before losing scope
		var pico = SplitArgs.BuildDisposableFromSingleString("-f file.txt --something");
		var something = pico.Contains("--something");

		Helpers.AssertPicoThrows(() => {
			pico.Dispose();
		}, "Dispose() should throw when parameters are leftover", 60);
#pragma warning restore CA2000 // Dispose objects before losing scope
	}

	[Fact(DisplayName = "Unwanted switch value")]
	public void UnwantedSwitchValue()
	{
		// here "--verbose" is acceptable but "--verbose=yes" is not
		var pico = SplitArgs.BuildFromSingleString("--verbose=yes --something");

		var something = pico.Contains("--something");
		var notpresent = pico.Contains("--notpresent");

		Assert.True(something);
		Assert.False(notpresent);

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			var verbose = pico.Contains("-v", "--verbose");
		}, "Contains() should throw when unwanted switch value is present", 80);
	}

	[Fact(DisplayName = "Multiple combined switches")]
	public void MultiSwitch()
	{
		var pico = SplitArgs.BuildFromSingleString("--file hello -abc");

		// confirm that -abc has been expanded to -a -b -c
		Assert.Equal(3 + 2, pico.UnconsumedArgs.Count);

		var file = pico.GetParam("-f", "--file");

		var a = pico.Contains("-a");
		var b = pico.Contains("-b");
		var c = pico.Contains("-c");
		var x = pico.Contains("-x");        // not specified

		pico.Finished();

		Assert.Equal("hello", file);
		Assert.True(a);
		Assert.True(b);
		Assert.True(c);
		Assert.False(x);
	}

	[Fact(DisplayName = "Multiple combined switches and param")]
	public void MultiSwitchAndParam()
	{
		var pico = SplitArgs.BuildFromSingleString("--file hello -abc codename");

		// confirm that "-abc codename" has been expanded to "-a -b -c codename"
		Assert.Equal(3 + 2 + 1, pico.UnconsumedArgs.Count);

		var file = pico.GetParam("-f", "--file");
		var code = pico.GetParam("-c", "--code");

		var a = pico.Contains("-a");
		var b = pico.Contains("-b");
		var x = pico.Contains("-x");        // not specified

		pico.Finished();

		Assert.Equal("hello", file);
		Assert.Equal("codename", code);
		Assert.True(a);
		Assert.True(b);
		Assert.False(x);
	}

	[Fact(DisplayName = "Multiple combined switches and param with equals")]
	public void MultiSwitchAndParamEquals()
	{
		var pico = SplitArgs.BuildFromSingleString("--file=hello -abc=codename", true);

		var file = pico.GetParam("-f", "--file");
		var code = pico.GetParam("-c", "--code");

		var a = pico.Contains("-a");
		var b = pico.Contains("-b");
		var x = pico.Contains("-x");        // not specified

		pico.Finished();

		Assert.Equal("hello", file);
		Assert.Equal("codename", code);
		Assert.True(a);
		Assert.True(b);
		Assert.False(x);
	}

	[Fact(DisplayName = "Multi switch with quotes")]
	public void MultiSwitchWithQuotes()
	{
		// we do not support multi-switches with quotes eg:
		// -abc='codename' is an error
		// =abc=codename is fine and becomes -a -b -c codename

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			var pico = SplitArgs.BuildFromSingleString("--file=hello -abc='codename'", true);
		}, "Multi-switches do not support quotes, and should throw", 90);
	}
}
