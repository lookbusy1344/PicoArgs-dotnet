﻿namespace PicoArgs_dotnet;

internal static class Program
{
	private static void Main(string[] args)
	{
		Console.WriteLine("Demo of PicoArgs.cs class, eg:");
		Console.WriteLine("PicoArgs-dotnet.exe --raw -i file1.txt -i file2.txt --file file3.txt --exclude something");
		Console.WriteLine("PicoArgs-dotnet.exe --raw -i=file1.txt -i=\"file 2.txt\" --file file3.txt -i=\"-something=else\" --exclude=\"something else\"");

		try {
			//var pico = new PicoArgs("--raw -f file1.txt -f file2.txt --file file3.txt --exclude something");
			var pico = new PicoArgs(args);

			var help = pico.Contains("-h", "-?", "--help");

			// if we want help, just bail here
			if (help) {
				Console.WriteLine(HelpMessage);
				return;
			}

			// parse the rest of the command line
			var raw = pico.Contains("-r", "--raw");
			var fast = pico.Contains("-f", "--fast");
			var slow = pico.Contains("-s", "--slow");

			var files = pico.GetMultipleParams("-i", "--file");
			var exclude = pico.GetParamOpt("-e", "--exclude") ?? "example-exclude";

			// we have finished, make sure there are no unused arguments
			pico.Finished();

			// show the results
			if (files.Count == 0) {
				Console.WriteLine(HelpMessage);
				Console.WriteLine("\r\nNo files specified");
				return;
			}

			var filesString = string.Join(", ", files);
			Console.WriteLine($"raw: {raw}");
			Console.WriteLine($"fast: {fast}");
			Console.WriteLine($"slow: {slow}");
			Console.WriteLine($"files: {filesString}");
			Console.WriteLine($"exclude: {exclude}");
		}
		catch (PicoArgsException ex) {
			Console.WriteLine($"PICOARGS ERROR: {ex.Message}");
		}
		catch (Exception ex) {
			Console.WriteLine($"ERROR: {ex.Message}");
		}
	}

#pragma warning disable IDE0051 // Remove unused private members

	/// <summary>
	/// Alternative usage to the example in Main() above.
	/// This wraps the parsing of the command line, and returns the results as a tuple.
	/// </summary>
	private static (bool help, DirectoryInfo folder, string pattern, bool verbose) ParseCommandLineWrapper(string[] args)
	{
		// this will throw when it leaves scope if there are any unused arguments
		using var pico = new PicoArgsDisposable(args);

		var help = pico.Contains("-h", "-?", "--help");

		// if we want help, just bail here, don't bother parsing the rest
		if (help) {
			return (true, null!, null!, false);
		}

		// parse the rest of the command line
		var folder = new DirectoryInfo(pico.GetParamOpt("-f", "--folder") ?? ".");
		var pattern = pico.GetParamOpt("-p", "--pattern") ?? "*.txt";
		var verbose = pico.Contains("-v", "--verbose");

		return (false, folder, pattern, verbose);
	}

	private const string HelpMessage = """
		Usage: PicoArgs-dotnet.exe [options]

		Options:
		  -f, --file <filename>     File(s) to search (required)
		  -e, --exclude <pattern>   Exclude pattern (default 'example-exclude')
		  -r, --raw                 Raw output
		  -h, --help, -?            Help information
		""";
}