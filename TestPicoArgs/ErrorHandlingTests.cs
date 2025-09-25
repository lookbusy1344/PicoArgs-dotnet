namespace TestPicoArgs;

using PicoArgs_dotnet;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class ErrorHandlingTests
{
	[Fact(DisplayName = "GetParam - parameter not found")]
	public void NoParamFound()
	{
		var pico = SplitArgs.BuildFromSingleString("-f file.txt");

		// this should throw an exception
		Helpers.AssertPicoThrows(() => {
			var files = pico.GetParam("--something");
		}, "GetParam() should throw when param missing", ErrorCode.MissingRequiredParameter);
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
}
