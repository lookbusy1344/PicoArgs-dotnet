#if DEBUG
using System.Text.RegularExpressions;
#endif

namespace PicoArgs_dotnet;

/*  PICOARGS_DOTNET - a tiny command line argument parser for .NET
    https://github.com/lookbusy1344/PicoArgs-dotnet

    Version 1.0.2 - 24 Sept 2023

    Example usage:

	var pico = new PicoArgs(args);

	bool verbose = pico.Contains("-v", "--verbose");  // true if any of these switches are present
	string pattern = pico.GetParamOpt("-t", "--pattern") ?? "*.txt";  // optional parameter
	string requiredpath = pico.GetParam("-p", "--path");  // mandatory parameter, throws if not present
	string[] files = pico.GetMultipleParams("-f", "--file");  // multiple parameters returned in string[]
	string command = pico.GetCommand();  // first parameter, throws if not present

	pico.Finished();  // We are done. Throw if there are any unused parameters


    INSPIRED BY PICO-ARGS FOR RUST: https://github.com/RazrFalcon/pico-args

*/

/// <summary>
/// a key and optional identified value eg --key=value
/// </summary>
public record class KeyValue(string Key, string? Value);

/// <summary>
/// Tiny command line argument parser
/// </summary>
public class PicoArgs
{
	private readonly List<KeyValue> args;
	private bool finished;

	/// <summary>
	/// Build a PicoArgs from the command line arguments
	/// </summary>
	public PicoArgs(IEnumerable<string> args) => this.args = args.Select(a => new KeyValue(a, null)).ToList();

#if DEBUG
	/// <summary>
	/// Build a PicoArgs from a single string, for testing
	/// </summary>
	public PicoArgs(string args) : this(StringSplitter.SplitParams(args)) { }
#endif

	/// <summary>
	/// Get a boolean value from the command line, returns TRUE if found
	/// </summary>
	public bool Contains(params string[] options)
	{
		CheckFinished();
		if (options == null || options.Length == 0)
			throw new ArgumentException("Must specify at least one option", nameof(options));

		// no args left
		if (args.Count == 0) return false;

		foreach (var o in options)
		{
			if (!o.StartsWith('-')) throw new ArgumentException("Must start with -", nameof(options));

			var index = args.FindIndex(a => a.Key == o);
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
		CheckFinished();
		var s = GetParamOpt(options);
		return s ?? throw new PicoArgsException($"Expected value for \"{string.Join(", ", options)}\"");
	}

	/// <summary>
	/// Get multiple parameters from the command line, or empty array if not present
	/// eg -a value1 -a value2 will return ["value1", "value2"]
	/// </summary>
	public string[] GetMultipleParams(params string[] options)
	{
		CheckFinished();
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
		CheckFinished();
		if (options == null || options.Length == 0)
			throw new ArgumentException("Must specify at least one option", nameof(options));

		if (args.Count == 0) return null;

		// check all options are switches
		foreach (var o in options)
			if (!o.StartsWith('-')) throw new ArgumentException("Must start with -", nameof(options));

		// do we have this switch on command line?
		var option = args.Find(a => options.Contains(a.Key));
		if (option == null) return null;

		// is it the last parameter?
		var index = args.IndexOf(option);
		if (index == args.Count - 1)
			throw new PicoArgsException($"Expected value after \"{option}\"");

		// is the next parameter another switch? This might be ok, eg --text "--something"
		var str = args[index + 1];

		// consume the switch and the value
		args.RemoveRange(index, 2);

		// return the value
		return str.Key;
	}

	/// <summary>
	/// Return and consume the first command line parameter. Throws if not present
	/// </summary>
	public string GetCommand()
	{
		CheckFinished();
		if (args.Count == 0) throw new PicoArgsException("Expected command");

		// check for a switch
		var cmd = args[0].Key;
		if (cmd.StartsWith('-')) throw new PicoArgsException($"Expected command not \"{cmd}\"");

		// consume the command, and return it
		args.RemoveAt(0);
		return cmd;
	}

	/// <summary>
	/// Return any unused command line parameters
	/// </summary>
	public IReadOnlyList<KeyValue> UnconsumedArgs => args;

	/// <summary>
	/// Return true if there are no unused command line parameters
	/// </summary>
	public bool IsEmpty => args.Count == 0;

	/// <summary>
	/// Throw an exception if there are any unused command line parameters
	/// </summary>
	public void Finished()
	{
		if (args.Count > 0)
			throw new PicoArgsException($"Unrecognised parameter(s): {string.Join(", ", args)}");

		finished = true;
	}

	/// <summary>
	/// Ensure that Finished() has not been called
	/// </summary>
	private void CheckFinished()
	{
		if (finished)
			throw new PicoArgsException("Cannot use PicoArgs after calling Finished()");
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
	/// If true, supress the check for unused command line parameters
	/// </summary>
	public bool SuppressCheck { get; set; }

	/// <summary>
	/// Throw an exception if there are any unused command line parameters
	/// </summary>
	public void Dispose()
	{
		if (!SuppressCheck)
			Finished();
	}
}

internal static class Helpers
{
	/// <summary>
	/// Index of a char in a string, or null if not found
	/// </summary>
	internal static int? IndexOf(string s, char c)
	{
		var i = s.IndexOf(c);
		return i < 0 ? null : i;
	}

	/// <summary>
	/// If the string starts and ends with the same quote, remove them
	/// </summary>
	internal static string TrimQuote(string s)
	{
		if (string.IsNullOrEmpty(s)) return s;
		if (s.Length < 2) return s;

		var c = s[0];
		if (c is '\'' or '\"')
			if (s[^1] == c)
				return s[1..^1];    // if ends with same quote, remove them

		return s;   // just return original string
	}
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
