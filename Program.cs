namespace PicoArgs_dotnet;

internal static class Program
{
	private static void Main(string[] args)
	{
		Console.WriteLine("Demo of PicoArgs.cs class, eg:");
		Console.WriteLine("PicoArgs-dotnet.exe --raw -f file1.txt -f file2.txt --file file3.txt --exclude something");

		//var pico = new PicoArgs("--raw -f file1.txt -f file2.txt --file file3.txt --exclude something");
		var pico = new PicoArgs(args);

		var help = pico.Contains("-h", "-?", "--help");

		// if we want help, just bail here
		if (help)
		{
			Console.WriteLine(HelpMessage);
			return;
		}

		// parse the rest of the command line
		var raw = pico.Contains("-r", "--raw");
		var files = pico.GetMultipleParams("-f", "--file");
		var exclude = pico.GetParamOpt("-e", "--exclude") ?? "example-exclude";

		// check for extra args
		pico.CheckArgsConsumed();

		// show the results
		if (files.Length == 0)
		{
			Console.WriteLine(HelpMessage);
			Console.WriteLine("\r\nNo files specified");
			return;
		}

		var filesstr = string.Join(", ", files);
		Console.WriteLine($"raw: {raw}");
		Console.WriteLine($"files: {filesstr}");
		Console.WriteLine($"exclude: {exclude}");
	}

	private const string HelpMessage =
@"Usage: PicoArgs-dotnet.exe [options]

Options:
  -f, --file <filename>     File(s) to search (required)
  -e, --exclude <pattern>   Exclude pattern (default 'example-exclude')
  -r, --raw                 Raw output
  -h, --help, -?            Help information";
}