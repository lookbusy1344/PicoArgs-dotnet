// #define WIN32_CALLS

namespace TestPicoArgs;

using System.Runtime.InteropServices;
using System.Text;
using PicoArgs_dotnet;

// Borrowed / inspired from
// https://github.com/fuweichin/Commander.NET/blob/master/Commander.NET/Utils.cs

// This class provides a generic way to split command line arguments, but can also use the Windows API to do so

internal static class SplitArgs
{
	/// <summary>
	/// Build a PicoArgs from a single command line arguments
	/// </summary>
	internal static PicoArgs BuildFromSingleString(string line, bool recogniseEquals = true) => new(SplitArgumentsLine(line), recogniseEquals);

	/// <summary>
	/// Build a PicoArgsDisposable from a single command line arguments
	/// </summary>
	internal static PicoArgsDisposable BuildDisposableFromSingleString(string line) => new(SplitArgumentsLine(line));

	/// <summary>
	/// Split a command line into arguments (adds "echo" to the front to handle the case where the first argument is quoted)
	/// </summary>
	private static IEnumerable<string> SplitArgumentsLine(string line) => SplitArgsImplementation.CommandLineToArgvW($"echo {line}").Skip(1);
}

internal static partial class SplitArgsImplementation
{
#if WIN32_CALLS
	/**
	 * Windows native CommandLineToArgvW
	 * Copied from https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp#answer-749653
	 */
	[LibraryImport("shell32.dll", SetLastError = true, EntryPoint = "CommandLineToArgvW")]
	private static extern IntPtr CommandLineToArgvW_Win32([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

	internal static string[] CommandLineToArgvW(string commandLine)
	{
		var argv = CommandLineToArgvW_Win32(commandLine, out var argc);
		if (argv == IntPtr.Zero) {
			throw new System.ComponentModel.Win32Exception();
		}

		try {
			var args = new string[argc];
			for (var i = 0; i < argc; i++) {
				var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
				args[i] = Marshal.PtrToStringUni(p) ?? "";
			}
			return args;
		}
		finally {
			Marshal.FreeHGlobal(argv);
		}
	}

#else

	/**
	 * C# equivalent of CommandLineToArgvW
	 * Translated from https://source.winehq.org/git/wine.git/blob/HEAD:/dlls/shcore/main.c#l264
	 */
	internal static string[] CommandLineToArgvW(string cmdLine)
	{
		if (string.IsNullOrWhiteSpace(cmdLine)) {
			return [];
		}

		var len = cmdLine.Length;
		var i = 0;
		var s = cmdLine[0];
		const char END = '\0';

		// The first argument, the executable path, follows special rules
		var argc = 1;
		if (s == '"') {
			do {
				s = ++i < len ? cmdLine[i] : END;
				if (s == '"') {
					break;
				}
			} while (s != END);
		} else {
			while (s is not END and not ' ' and not '\t') {
				s = ++i < len ? cmdLine[i] : END;
			}
		}

		// skip to the first argument, if any
		while (s is ' ' or '\t') {
			s = ++i < len ? cmdLine[i] : END;
		}

		if (s != END) {
			argc++;
		}

		// Analyse the remaining arguments
		var quoteCount = 0; // quote count
		var backCount = 0; // backslash count
		while (i < len) {
			s = cmdLine[i];
			if ((s == ' ' || s == '\t') && quoteCount == 0) {
				// skip to the next argument and count it if any
				do {
					s = ++i < len ? cmdLine[i] : END;
				} while (s is ' ' or '\t');

				if (s != END) {
					argc++;
				}

				backCount = 0;
			} else if (s == '\\') {
				// '\', count them
				backCount++;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
				s = ++i < len ? cmdLine[i] : END;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
			} else if (s == '"') {
				// '"'
				if ((backCount & 1) == 0) {
					quoteCount++; // unescaped '"'
				}

				s = ++i < len ? cmdLine[i] : END;
				backCount = 0;

				// consecutive quotes, see comment in copying code below
				while (s == '"') {
					quoteCount++;
					s = ++i < len ? cmdLine[i] : END;
				}
				quoteCount %= 3;
				if (quoteCount == 2) {
					quoteCount = 0;
				}
			} else {
				// a regular character
				backCount = 0;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
				s = ++i < len ? cmdLine[i] : END;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
			}
		}

		var argv = new string[argc];
		var sb = new StringBuilder();
		i = 0;
		var j = 0;
		s = cmdLine[i];
		if (s == '"') {
			do {
				s = ++i < len ? cmdLine[i] : END;
				if (s == '"') {
					break;
				}

				_ = sb.Append(s);
			} while (s != END);

			argv[j++] = sb.ToString();
			_ = sb.Clear();
		} else {
			while (s is not END and not ' ' and not '\t') {
				_ = sb.Append(s);
				s = ++i < len ? cmdLine[i] : END;
			}

			argv[j++] = sb.ToString();
			_ = sb.Clear();
		}
		while (s is ' ' or '\t') {
			s = ++i < len ? cmdLine[i] : END;
		}

		if (i >= len) {
			return argv;
		}

		quoteCount = 0;
		backCount = 0;
		while (i < len) {
			if ((s == ' ' || s == '\t') && quoteCount == 0) {
				// close the argument
				argv[j++] = sb.ToString();
				_ = sb.Clear();
				backCount = 0;

				// skip to the next one and initialize it if any
				do {
					s = ++i < len ? cmdLine[i] : END;
				} while (s is ' ' or '\t');
			} else if (s == '\\') {
				_ = sb.Append(s);
				s = ++i < len ? cmdLine[i] : END;
				backCount++;
			} else if (s == '"') {
				if ((backCount & 1) == 0) {
					// Preceded by an even number of '\', this is half that number of '\', plus a quote which we erase.
					sb.Length -= backCount / 2;
					quoteCount++;
				} else {
					// Preceded by an odd number of '\', this is half that number of '\' followed by a '"'
					sb.Length = sb.Length - 1 - (backCount / 2) - 1;
					_ = sb.Append('"');
				}
				s = ++i < len ? cmdLine[i] : END;
				backCount = 0;

				/* Now count the number of consecutive quotes. Note that quoteCount
				 * already takes into account the opening quote if any, as well as
				 * the quote that lead us here.
				 */
				while (s == '"') {
					if (++quoteCount == 3) {
						_ = sb.Append('"');
						quoteCount = 0;
					}
					s = ++i < len ? cmdLine[i] : END;
				}
				if (quoteCount == 2) {
					quoteCount = 0;
				}
			} else {
				// a regular character
				_ = sb.Append(s);
				s = ++i < len ? cmdLine[i] : END;
				backCount = 0;
			}
		}
		if (sb.Length > 0) {
			argv[j++] = sb.ToString();
			_ = sb.Clear();
		}
		return argv;
	}
#endif
}
