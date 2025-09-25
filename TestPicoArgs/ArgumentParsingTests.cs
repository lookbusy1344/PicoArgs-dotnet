namespace TestPicoArgs;

using PicoArgs_dotnet;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class ArgumentParsingTests
{
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
		var pico = SplitArgs.BuildFromSingleString(
			"--file=item1 --file=\"item2\" --file='item3' --file=\"item 4\" --file=\"-item 5\" -f=\"--item=6\"");

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
		KeyValue[] expected = [
			new("--file", "file1.txt"), new("--print", null),
			new("something", null), new("--verbose", "yes")
		];
		var pico = SplitArgs.BuildFromSingleString("--file=file1.txt --print something --verbose=yes");

		var match = expected.SequenceEqual(pico.UnconsumedArgs);

		Assert.Equal(expected.Length, pico.UnconsumedArgs.Count);
		Assert.True(match);
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
