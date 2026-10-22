namespace TestPicoArgs;

using PicoArgs_dotnet;

public class NegativeNumberTests
{
	[Theory(DisplayName = "Negative number after a switch is its value, not combined switches")]
	[InlineData("-5")]
	[InlineData("-10")]
	[InlineData("-1.5")]
	[InlineData("-.5")]
	[InlineData("-1.")]
	[InlineData("-1e3")]
	[InlineData("-2.5E-3")]
	public void NegativeNumberAsValue(string number)
	{
		var pico = new PicoArgs(["-n", number, "-v"]);

		Assert.True(pico.Contains("-v"));
		Assert.Equal(number, pico.GetParam("-n"));
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Multi-digit negative number is one argument")]
	public void MultiDigitNotSplit()
	{
		var pico = new PicoArgs(["-12"]);

		Assert.Equal([new KeyValue("-12", null)], pico.UnconsumedArgs);
		Assert.False(pico.Contains("-1"));
	}

	[Theory(DisplayName = "Non-numbers after a dash are still split into switches")]
	[InlineData("-abc", new[] { "-a", "-b", "-c" })]
	[InlineData("-Inf", new[] { "-I", "-n", "-f" })]
	[InlineData("-NaN", new[] { "-N", "-a", "-N" })]
	[InlineData("-1,5", new[] { "-1", "-,", "-5" })]
	[InlineData("-1x", new[] { "-1", "-x" })]
	[InlineData("-0x10", new[] { "-0", "-x", "-1", "-0" })]
	public void NonNumbersStillSplit(string arg, string[] expectedKeys)
		=> Assert.Equal(expectedKeys, new PicoArgs([arg]).UnconsumedArgs.Select(kv => kv.Key));

	[Fact(DisplayName = "-Infinity is split, not parsed as a number")]
	public void InfinityStillSplit()
	{
		var pico = new PicoArgs(["-Infinity"]);

		Assert.Equal(["-I", "-n", "-f", "-i", "-n", "-i", "-t", "-y"], pico.UnconsumedArgs.Select(kv => kv.Key));
	}

	[Theory(DisplayName = "Negative number is accepted as a command")]
	[InlineData("-5")]
	[InlineData("-10")]
	[InlineData("-1.5")]
	public void NegativeNumberAsCommand(string number)
	{
		var pico = new PicoArgs([number, "rest"]);

		Assert.Equal(number, pico.GetCommand());
		Assert.Equal("rest", pico.GetCommand());
	}

	[Fact(DisplayName = "Switch is still rejected as a command")]
	public void SwitchRejectedAsCommand()
	{
		var pico = new PicoArgs(["-x"]);

		var ex = Assert.Throws<PicoArgsException>(() => pico.GetCommand());
		Assert.Equal(ErrorCode.InvalidCommand, ex.Code);
		Assert.Equal("Expected command not \"-x\"", ex.Message);
	}
}
