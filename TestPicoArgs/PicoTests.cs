﻿using PicoArgs_dotnet;

namespace TestPicoArgs;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0053 // Use expression body for lambda expression
#pragma warning disable IDE0200 // Remove unnecessary lambda expression
#pragma warning disable RCS1021 // Convert lambda expression body to expression body.

public class PicoTests
{
	[Fact(DisplayName = "Basic test", Timeout = 1000)]
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

	[Fact(DisplayName = "GetParamOpt test", Timeout = 1000)]
	public void GetParamOptTest()
	{
		var pico = SplitArgs.BuildFromSingleString("--help --another something");

		var another = pico.GetParamOpt("--another");
		var missing = pico.GetParamOpt("--missing");

		Assert.Equal("something", another);
		Assert.Null(missing);
	}

	[Fact(DisplayName = "GetMultipleParams test", Timeout = 1000)]
	public void MultipleTest()
	{
		var expected = new string[] { "file.txt", "another.txt", "again.txt" };
		var pico = SplitArgs.BuildFromSingleString("-f file.txt --junk xxx --file another.txt -f again.txt");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.False(pico.IsEmpty);
	}

	[Fact(DisplayName = "GetParam - parameter not found", Timeout = 1000)]
	public void NoParamFound()
	{
		var pico = SplitArgs.BuildFromSingleString("-f file.txt");

		// this should throw an exception
		Helpers.AssertPicoThrows(() =>
		{
			var files = pico.GetParam("--something");
		}, "GetParam() should throw when param missing", 10);
	}

	[Fact(DisplayName = "Using equals and complex value", Timeout = 1000)]
	public void ComplexValue()
	{
		var expected = new string[] { "file.txt", "-something=else" };
		var pico = SplitArgs.BuildFromSingleString("--file=file.txt --file=-something=else");

		var files = pico.GetMultipleParams("-f", "--file");

		var match = Helpers.CompareNames(expected, files);
		Assert.True(match);
		Assert.True(pico.IsEmpty);
	}

	[Fact(DisplayName = "Using equals and complex value, when not enabled", Timeout = 1000)]
	public void ComplexValueFail()
	{
		//var expected = new string[] { "file.txt", "-something=else" };
		var pico = SplitArgs.BuildFromSingleString("--file=file.txt --file=-something=else", false);

		var files = pico.GetMultipleParams("-f", "--file");

		Assert.Empty(files);
	}

	[Fact(DisplayName = "Leftover parameter", Timeout = 1000)]
	public void LeftoverParam()
	{
		var pico = SplitArgs.BuildFromSingleString("-f file.txt --something");
		var something = pico.Contains("--something");

		Assert.True(something);
		Assert.False(pico.IsEmpty);

		// this should throw an exception
		Helpers.AssertPicoThrows(() =>
		{
			pico.Finished();
		}, "Finished() should throw when parameters are leftover", 60);
	}

	[Fact(DisplayName = "GetCommand test", Timeout = 1000)]
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
		Helpers.AssertPicoThrows(() =>
		{
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

	[Fact(DisplayName = "Key / value splitting in detail", Timeout = 1000)]
	public void KVCheck()
	{
		var expected = new KeyValue[] { new KeyValue("--file", "file1.txt"), new KeyValue("--print", null),
			new KeyValue("something", null), new KeyValue("--verbose", "yes") };
		var pico = SplitArgs.BuildFromSingleString("--file=file1.txt --print something --verbose=yes");

		var match = expected.SequenceEqual(pico.UnconsumedArgs);

		Assert.Equal(expected.Length, pico.UnconsumedArgs.Count);
		Assert.True(match);
	}

	[Fact(DisplayName = "PicoArgsDisposable check", Timeout = 1000)]
	public void DisposalCheck()
	{
#pragma warning disable CA2000 // Dispose objects before losing scope
		var pico = SplitArgs.BuildDisposableFromSingleString("-f file.txt --something");
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
		var pico = SplitArgs.BuildFromSingleString("--verbose=yes --something");

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
