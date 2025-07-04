namespace TestPicoArgs;

using PicoArgs_dotnet;

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
		string[] expected = ["file.txt", "another.txt", "again.txt"];
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
		}, "GetParam() should throw when param missing", ErrorCode.MissingRequiredParameter);
	}

	[Fact(DisplayName = "Using equals and complex value")]
	public void ComplexValue()
	{
		string[] expected = ["file.txt", "-something=else"];
		var pico = SplitArgs.BuildFromSingleString("--file=file.txt --file=-something=else");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Valid quoted values")]
	public void QuotedValues()
	{
		string[] expected = ["item1", "item2", "item3", "item 4", "-item 5", "--item=6"];
		var pico = SplitArgs.BuildFromSingleString("--file=item1 --file=\"item2\" --file='item3' --file=\"item 4\" --file=\"-item 5\" -f=\"--item=6\"");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Invalid quoted values")]
	public void InvalidQuotedValues()
	{
		// these quotes do not match, so are not parsed
		string[] expected = ["\"item1'", "'item2\""];
		var pico = new PicoArgs(["--file=\"item1'", "--file='item2\""]);

		var files = pico.GetMultipleParams("--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Using equals and complex value, when not enabled")]
	public void ComplexValueFail()
	{
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
		}, "Finished() should throw when parameters are leftover", ErrorCode.UnrecognisedParameters);
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
		}, "GetCommand() should throw when no command present", ErrorCode.MissingCommand);
	}

	[Fact(DisplayName = "GetCommandOpt test")]
	public void GetCommandOpt()
	{
		var pico = SplitArgs.BuildFromSingleString("--file file.txt -v PRINT");

		var verbose = pico.Contains("-v", "--verbose");
		var file = pico.GetParam("-f", "--file");
		var print = pico.GetCommand();
		var notFound = pico.GetCommandOpt();    // a command that is not present, so should be null
		pico.Finished();

		Assert.True(verbose);
		Assert.Equal("file.txt", file);
		Assert.Equal("PRINT", print);
		Assert.Null(notFound);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Argument splitting")]
	public void ArgumentSplitting()
	{
		string[] expected = ["once", "upon", "a", "time", "in Hollywood"];
		var pico = SplitArgs.BuildFromSingleString("once upon a time \"in Hollywood\"");

		var content = pico.UnconsumedArgs.Select(a => a.Key).ToArray();
		var match = Helpers.CompareNames(expected, content);
		Assert.True(match);
	}

	[Fact(DisplayName = "Key / value splitting in detail")]
	public void KVCheck()
	{
		KeyValue[] expected = [ new("--file", "file1.txt"), new("--print", null),
			new("something", null), new("--verbose", "yes") ];
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
		}, "Dispose() should throw when parameters are leftover", ErrorCode.UnrecognisedParameters);
#pragma warning restore CA2000 // Dispose objects before losing scope
	}

	[Fact(DisplayName = "Unwanted switch value")]
	public void UnwantedSwitchValue()
	{
		// here "--verbose" is acceptable but "--verbose=yes" is not
		var pico = SplitArgs.BuildFromSingleString("--verbose=yes --something");

		var something = pico.Contains("--something");
		var notPresent = pico.Contains("--not-present");

		Assert.True(something);
		Assert.False(notPresent);

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			var verbose = pico.Contains("-v", "--verbose");
		}, "Contains() should throw when unwanted switch value is present", ErrorCode.UnexpectedValue);
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
		var pico = SplitArgs.BuildFromSingleString("--file=hello -abc='codename' --another", true);

		var file = pico.GetParam("-f", "--file");
		var code = pico.GetParam("-c", "--code");

		var a = pico.Contains("-a");
		var b = pico.Contains("-b");
		var another = pico.Contains("--another");
		var x = pico.Contains("-x");        // not specified

		pico.Finished();

		Assert.Equal("hello", file);
		Assert.Equal("codename", code);
		Assert.True(a);
		Assert.True(b);
		Assert.True(another);
		Assert.False(x);
	}

	[Fact(DisplayName = "Multi switch no equals recognition")]
	public void MultiSwitchNotRecogniseEquals()
	{
		var pico = SplitArgs.BuildFromSingleString("-abc=codename", false);

		_ = pico.Contains("-a");
		_ = pico.Contains("-b");

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			var code = pico.GetParam("-c", "--code");
		}, "Should not be able to parse c=codename when equals recognition turned off", ErrorCode.MissingRequiredParameter);

		Helpers.AssertPicoThrows(() => {
			pico.Finished();
		}, "Since -c was not processed, Finished() should throw", ErrorCode.UnrecognisedParameters);
	}

	[Fact(DisplayName = "Invalid params like -something")]
	public void InvalidParams()
	{
		var pico = SplitArgs.BuildFromSingleString("--hello --world");

		_ = pico.Contains("-h", "--hello");
		_ = pico.Contains("-w", "--world");

		Helpers.AssertThrows<ArgumentException>(() => {
			var code = pico.GetParam("-s", "-something");
		}, "-something is not a valid param");

		Helpers.AssertThrows<ArgumentException>(() => {
			var code = pico.Contains("something");
		}, "something is not a valid param, no dash");

		Helpers.AssertThrows<ArgumentException>(() => {
			var code = pico.GetParam("---something");
		}, "---something is not a valid param");

		Helpers.AssertThrows<ArgumentException>(() => {
			var code = pico.Contains("-something");
		}, "-something is not a valid option");

		Helpers.AssertThrows<ArgumentException>(() => {
			var code = pico.Contains("-x", "--x");
		}, "--x is not a valid option");

		pico.Finished();
	}

	[Fact(DisplayName = "Invalid input like --x or ---action")]
	public void InvalidInput()
	{
		// first check good input
		var pico = SplitArgs.BuildFromSingleString("-x --ok -ab");

		var x = pico.Contains("-x");
		var ok = pico.Contains("--ok");
		var a = pico.Contains("-a");
		var b = pico.Contains("-b");
		var missing = pico.Contains("--missing");

		pico.Finished();

		Assert.True(x);
		Assert.True(ok);
		Assert.True(a);
		Assert.True(b);
		Assert.False(missing);

		// now check some invalid inputs
		Helpers.AssertPicoThrows(() => {
			var fail = SplitArgs.BuildFromSingleString("-x --o");
		}, "Should not parse --o", ErrorCode.InvalidParameter);

		Helpers.AssertPicoThrows(() => {
			var fail = SplitArgs.BuildFromSingleString("-x ---o");
		}, "Should not parse ---o", ErrorCode.InvalidParameter);

		Helpers.AssertPicoThrows(() => {
			var fail = SplitArgs.BuildFromSingleString("-x ---another");
		}, "Should not parse ---another", ErrorCode.InvalidParameter);
	}

	[Fact(DisplayName = "Equals with no dash")]
	public void EqualsNoDash()
	{
		// "hello=world" without a dash should not be split on the equals, eg key="hello=world" value=null

		var pico = SplitArgs.BuildFromSingleString("hello=world");

		var kv = Assert.Single(pico.UnconsumedArgs);
		Assert.Equal("hello=world", kv.Key);
		Assert.Null(kv.Value);

		var cmd = pico.GetCommand();
		Assert.Equal("hello=world", cmd);

		pico.Finished();
	}

	[Fact(DisplayName = "Escaped quotes in value")]
	public void EscapedQuotesInValue()
	{
		// Escaped quotes in the value should be preserved
		// --key="foo \"bar\" baz"

		// BuildFromSingleString() seems to have a bug with this, so we use PicoArgs directly
		var pico = new PicoArgs(["--key=\"foo \\\"bar\\\" baz\""]);

		var kv = Assert.Single(pico.UnconsumedArgs);
		Assert.Equal("--key", kv.Key);
		Assert.Equal("foo \"bar\" baz", kv.Value);
	}
}
