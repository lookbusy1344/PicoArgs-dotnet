using System.Runtime.InteropServices;
using System.Text;

namespace TestPicoArgs;

// Borrowed from
// https://github.com/fuweichin/Commander.NET/blob/master/Commander.NET/Utils.cs

internal static class SplitArgs
{
	internal static string[] SplitArgumentsLine(string line) => CommandLineToArgvW($"echo {line}").Skip(1).ToArray();

	/**
	 * C# equivalent of CommandLineToArgvW
	 * Translated from https://source.winehq.org/git/wine.git/blob/HEAD:/dlls/shcore/main.c#l264
	 */
	private static string[] CommandLineToArgvW(string cmdline)
	{
		if (string.IsNullOrWhiteSpace(cmdline)) return [];

		var len = cmdline.Length;
		var i = 0;
		var s = cmdline[i];
		const char END = '\0';

		// The first argument, the executable path, follows special rules
		var argc = 1;
		if (s == '"')
		{
			do
			{
				s = ++i < len ? cmdline[i] : END;
				if (s == '"') break;
			} while (s != END);
		}
		else
		{
			while (s is not END and not ' ' and not '\t')
				s = ++i < len ? cmdline[i] : END;
		}
		// skip to the first argument, if any
		while (s is ' ' or '\t')
			s = ++i < len ? cmdline[i] : END;
		if (s != END) argc++;

		// Analyze the remaining arguments
		var qcount = 0; // quote count
		var bcount = 0; // backslash count
		while (i < len)
		{
			s = cmdline[i];
			if ((s == ' ' || s == '\t') && qcount == 0)
			{
				// skip to the next argument and count it if any
				do
				{
					s = ++i < len ? cmdline[i] : END;
				} while (s is ' ' or '\t');

				if (s != END) argc++;
				bcount = 0;
			}
			else if (s == '\\')
			{
				// '\', count them
				bcount++;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
				s = ++i < len ? cmdline[i] : END;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
			}
			else if (s == '"')
			{
				// '"'
				if ((bcount & 1) == 0) qcount++; // unescaped '"'
				s = ++i < len ? cmdline[i] : END;
				bcount = 0;

				// consecutive quotes, see comment in copying code below
				while (s == '"')
				{
					qcount++;
					s = ++i < len ? cmdline[i] : END;
				}
				qcount %= 3;
				if (qcount == 2) qcount = 0;
			}
			else
			{
				// a regular character
				bcount = 0;
#pragma warning disable IDE0059 // Unnecessary assignment of a value
				s = ++i < len ? cmdline[i] : END;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
			}
		}

		var argv = new string[argc];
		var sb = new StringBuilder();
		i = 0;
		var j = 0;
		s = cmdline[i];
		if (s == '"')
		{
			do
			{
				s = ++i < len ? cmdline[i] : END;
				if (s == '"')
					break;
				else
					_ = sb.Append(s);
			} while (s != END);

			argv[j++] = sb.ToString();
			_ = sb.Clear();
		}
		else
		{
			while (s is not END and not ' ' and not '\t')
			{
				_ = sb.Append(s);
				s = ++i < len ? cmdline[i] : END;
			}

			argv[j++] = sb.ToString();
			_ = sb.Clear();
		}
		while (s is ' ' or '\t')
			s = ++i < len ? cmdline[i] : END;
		if (i >= len) return argv;

		qcount = 0;
		bcount = 0;
		while (i < len)
		{
			if ((s == ' ' || s == '\t') && qcount == 0)
			{
				// close the argument
				argv[j++] = sb.ToString();
				_ = sb.Clear();
				bcount = 0;

				// skip to the next one and initialize it if any
				do
				{
					s = ++i < len ? cmdline[i] : END;
				} while (s is ' ' or '\t');
			}
			else if (s == '\\')
			{
				_ = sb.Append(s);
				s = ++i < len ? cmdline[i] : END;
				bcount++;
			}
			else if (s == '"')
			{
				if ((bcount & 1) == 0)
				{
					// Preceded by an even number of '\', this is half that number of '\', plus a quote which we erase.
					sb.Length -= bcount / 2;
					qcount++;
				}
				else
				{
					// Preceded by an odd number of '\', this is half that number of '\' followed by a '"'
					sb.Length = sb.Length - 1 - (bcount / 2) - 1;
					_ = sb.Append('"');
				}
				s = ++i < len ? cmdline[i] : END;
				bcount = 0;

				/* Now count the number of consecutive quotes. Note that qcount
				 * already takes into account the opening quote if any, as well as
				 * the quote that lead us here.
				 */
				while (s == '"')
				{
					if (++qcount == 3)
					{
						_ = sb.Append('"');
						qcount = 0;
					}
					s = ++i < len ? cmdline[i] : END;
				}
				if (qcount == 2) qcount = 0;
			}
			else
			{
				// a regular character
				_ = sb.Append(s);
				s = ++i < len ? cmdline[i] : END;
				bcount = 0;
			}
		}
		if (sb.Length > 0)
		{
			argv[j++] = sb.ToString();
			_ = sb.Clear();
		}
		return argv;
	}

	/**
	 * Windows native CommandLineToArgvW
	 * Copied from https://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp#answer-749653
	 */
	/*
	[DllImport("shell32.dll", SetLastError = true)]
	private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

	public static string[] CommandLineToArgvW(string commandLine)
	{
		int argc;
		var argv = CommandLineToArgvW(commandLine, out argc);
		if (argv == IntPtr.Zero)
			throw new System.ComponentModel.Win32Exception();
		try
		{
			var args = new string[argc];
			for (var i = 0; i < argc; i++)
			{
				var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
				args[i] = Marshal.PtrToStringUni(p);
			}
			return args;
		}
		finally
		{
			Marshal.FreeHGlobal(argv);
		}
	} */
}
