#if DEBUG
using System.Text.RegularExpressions;
#endif

namespace PicoArgs_dotnet;

/* PICOARGS_DOTNET:
	using var pico = new PicoArgs(args);

	bool help = pico.Contains("-h", "-?", "--help");					// true if any of these switches are present
	string pattern = pico.GetParamOpt("-p", "--pattern") ?? "*.txt";	// optional parameter
	string requiredpath = pico.GetParam("-p", "--path");				// mandatory parameter, throws if not present
	string[] files = pico.GetMultipleParams("-f", "--file");			// multiple parameters returned in string[]


  INSPIRED BY PICO-ARGS FOR RUST:
  https://github.com/RazrFalcon/pico-args

*/

/// <summary>
/// Tiny command line argument parser
/// </summary>
public class PicoArgs
{
	private readonly List<string> args;

	/// <summary>
	/// Build a PicoArgs from the command line arguments
	/// </summary>
	public PicoArgs(string[] args) => this.args = args.ToList();

#if DEBUG
	/// <summary>
	/// Build a PicoArgs from a single string, for testing
	/// </summary>
	public PicoArgs(string args) => this.args = StringSplitter.SplitParams(args);
#endif

	/// <summary>
	/// Get a boolean value from the command line, returns TRUE if found
	/// </summary>
	public bool Contains(params string[] options)
	{
		if (options == null || options.Length == 0)
			throw new ArgumentException("Must specify at least one option", nameof(options));

		// no args left
		if (args.Count == 0) return false;

		foreach (var o in options)
		{
			if (!o.StartsWith('-')) throw new ArgumentException("Must start with -", nameof(options));

			var index = args.IndexOf(o);
			if (index >= 0)
			{
				// found switch so consume it and return
				args.RemoveAt(index);
				return true;
			}
		}

		// not found
		return false;
	}

	/// <summary>
	/// Get a string value from the command line, throws is not present
	/// eg -a "value" or --foldera "value"
	/// </summary>
	public string GetParam(params string[] options)
	{
		var s = GetParamOpt(options);
		return s ?? throw new PicoArgsException($"Expected value for \"{string.Join(", ", options)}\"");
	}

	/// <summary>
	/// Get multiple parameters from the command line, or empty array if not present
	/// eg -a value1 -a value2 will return ["value1", "value2"]
	/// </summary>
	public string[] GetMultipleParams(params string[] options)
	{
		var result = new List<string>();
		while (true)
		{
			var s = GetParamOpt(options);
			if (s == null) break;   // nothing else found, break out of loop
			result.Add(s);
		}

		return result.ToArray();
	}

	/// <summary>
	/// Get a string value from the command line, or null if not present
	/// eg -a "value" or --foldera "value"
	/// </summary>
	public string? GetParamOpt(params string[] options)
	{
		if (options == null || options.Length == 0)
			throw new ArgumentException("Must specify at least one option", nameof(options));

		if (args.Count == 0) return null;

		// check all options are switches
		foreach (var o in options)
			if (!o.StartsWith('-')) throw new ArgumentException("Must start with -", nameof(options));

		// do we have this switch on command line?
		var option = args.Find(a => options.Contains(a));
		if (option == null) return null;

		// is it the last parameter?
		var index = args.IndexOf(option);
		if (index == args.Count - 1)
			throw new PicoArgsException($"Expected value after \"{option}\"");

		// is the next parameter another switch?
		var str = args[index + 1];
		if (str.StartsWith('-'))
			throw new PicoArgsException($"Value for \"{option}\" is \"{str}\"");

		// consume the switch and the value
		args.RemoveAt(index);
		args.RemoveAt(index); // index is now the previous index+1

		// return the value
		return str;
	}

	/// <summary>
	/// Return and consume the first command line parameter. Throws if not present
	/// </summary>
	public string GetCommand()
	{
		if (args.Count == 0) throw new PicoArgsException("Expected command");

		// check for a switch
		var cmd = args[0];
		if (cmd.StartsWith('-')) throw new PicoArgsException($"Expected command not \"{cmd}\"");

		// consume the command, and return it
		args.RemoveAt(0);
		return cmd;
	}

	/// <summary>
	/// Return any unused command line parameters
	/// </summary>
	public IReadOnlyList<string> UnconsumedArgs => args;

	/// <summary>
	/// Throw an exception if there are any unused command line parameters
	/// </summary>
	public void CheckArgsConsumed()
	{
		if (args.Count > 0)
			throw new PicoArgsException($"Unrecognised parameter(s): {string.Join(", ", args)}");
	}
}

/// <summary>
/// Tiny command line argument parser. This version implements IDisposable, and will throw if there are any unused command line parameters
/// </summary>
public sealed class PicoArgsDisposable : PicoArgs, IDisposable
{
	public PicoArgsDisposable(string[] args) : base(args) { }

#if DEBUG
	/// <summary>
	/// Build a PicoArgs from a single string, for testing
	/// </summary>
	public PicoArgsDisposable(string args) : base(args) { }
#endif

	/// <summary>
	/// Throw an exception if there are any unused command line parameters
	/// </summary>
	public void Dispose() => CheckArgsConsumed();
}

/// <summary>
/// Exception thrown when there is a problem with the command line arguments
/// </summary>
public class PicoArgsException : Exception
{
	public PicoArgsException(string message) : base(message) { }

	public PicoArgsException()
	{
	}

	public PicoArgsException(string message, Exception innerException) : base(message, innerException)
	{
	}
}

#if DEBUG
/// <summary>
/// Helper class to split a string into parameters, respecting quotes
/// </summary>
internal static partial class StringSplitter
{
	/// <summary>
	/// Split a string into parameters, respecting quotes
	/// </summary>
	/// <returns></returns>
	public static List<string> SplitParams(string s) => SplitOnSpacesRespectQuotes().Split(s).Where(i => i != "\"").ToList();

	/// <summary>
	/// Regex to split a string into parameters, respecting quotes
	/// </summary>
	[GeneratedRegex("(?<=\")(.*?)(?=\")|\\s+")]
	private static partial Regex SplitOnSpacesRespectQuotes();
}
#endif
