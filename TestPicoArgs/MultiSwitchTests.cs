namespace TestPicoArgs;

using PicoArgs_dotnet;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class MultiSwitchTests
{
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
		var x = pico.Contains("-x"); // not specified

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
		var x = pico.Contains("-x"); // not specified

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
		var x = pico.Contains("-x"); // not specified

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
		var x = pico.Contains("-x"); // not specified

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
		}, "Should not be able to parse c=codename when equals recognition turned off",
			ErrorCode.MissingRequiredParameter);

		Helpers.AssertPicoThrows(() => {
			pico.Finished();
		}, "Since -c was not processed, Finished() should throw", ErrorCode.UnrecognisedParameters);
	}
}
