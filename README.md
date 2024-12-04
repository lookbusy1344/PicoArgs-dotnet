# PicoArgs-dotnet

[![PicoArgs compile and test](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml/badge.svg)](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml)
[![CodeQL](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/github-code-scanning/codeql)

A tiny command line argument parser for dotnet, inspired by the Rust pico-args library https://github.com/RazrFalcon/pico-args

Like the Rust library, this library is intended to be used for small command line tools, where you don't want to use a full blown argument parser like CommandLineParser https://github.com/commandlineparser/commandline

PicoArgs-dotnet's features are intentionally very minimal:

- Only one file to add, no dependencies, very compact
- Supports flags, options and positional arguments
- Supports equivalent short and long options `-p` alias for `--print`
- Supports combining short flags, e.g. `-abc` is equivalent to `-a` `-b` `-c` including with a trailing parameter `-abc=code` same as `-a` `-b` `-c code` (new in v1.5)
- Supports multiple values for options `-f file1 -f file2`
- Tiny API, a couple of hundred lines of code
- Unit tests included
- Supports .NET 9 (main branch) and .NET 8 (dotnet8 branch) with legacy .NET 7 code at [7bb0f2c61306ef53d583f](https://github.com/lookbusy1344/PicoArgs-dotnet/tree/7bb0f2c61306ef53d583f77232e3cab49fd151ec)

Some intentional limitations:

- NO support for default help generation, you need to do this manually
- NO support for conversions, all arguments are strings (all flags are bools) unless you convert them yourself

Order of argument consumption is important. Once consumed an argument is removed from the available list. Once all your expected arguments have been consumed, you can check for any unexpected arguments with ```Finished()```.

## Usage
No nuget packages, just add ```PicoArgs.cs``` to your project. Then in your code:

```csharp
var pico = new PicoArgs(args); // construct with command line arguments string[]

bool raw = pico.Contains("-r", "--raw"); // returns true if either flag is present
IReadOnlyList<string> files = pico.GetMultipleParams("-f", "--file"); // zero length if none found
string filename = pico.GetParam("-f", "--file"); // throws if not specified
string exclude = pico.GetParamOpt("-e", "--exclude") ?? "default"; // specifying a default

pico.Finished(); // we are finished, check for extra unwanted arguments & throw is any are left over

```

More examples can be found in the ```Program.cs``` file.


There is a ```PicoArgsDisposable``` class which implements ```IDisposable``` to automatically throw on extra params. This may be to your taste:

```csharp
using var pico = new PicoArgsDisposable(args);

bool raw = pico.Contains("-r", "--raw");
// Finished() called to check for extra arguments, when pico goes out of scope
```

## Suggested use pattern

I like to use a simple wrapper function to handle the argument parsing and throw an exception if there are any extra arguments:

```csharp
private static ConfigOptions ParseConfig(string[] args)
{
    var pico = new PicoArgs(args);

    if (pico.Contains("-h", "--help", "-?")) {
        // help, no further parsing needed
        Console.WriteLine(CommandLineMessage);
        Environment.Exit(0);
    }

    // parse command line parameters
    string? instance = pico.GetParamOpt("-i", "--instance") ?? Environment.GetEnvironmentVariable("INSTANCE");
    string? database = pico.GetParamOpt("-d", "--database") ?? Environment.GetEnvironmentVariable("DATABASE");
    bool singleThread = pico.Contains("-s", "--single-thread");

    pico.Finished(); // throw if any unknown arguments

    // ensure required parameters are present
    if (string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(database)) {
        // or just throw here
        Console.WriteLine(CommandLineMessage);
        Environment.Exit(1);
    }

    return new ConfigOptions(instance, database, singleThread);
}
```

## .NET 9 improvements with ReadOnlySpan

In .NET 9 the API has been updated to use `params ReadOnlySpan<string> options` instead of `params string[] options`. Since possible command line options are almost always constant, this allows great optimization by using stack-allocated inline arrays of strings, instead of heap-allocated arrays. The generated code then looks something like this:

```csharp
// .NET 9 VERSION

// As-written code:
bool raw = pico.Contains("-r", "--raw");

// ===========================================================
// Desugared code (simplified):

// elsewhere - define inline array of 2 strings
[System.Runtime.CompilerServices.InlineArray(2)]
struct InlineArray
{
	private string _element0;
}

// create the buffer on the stack
InlineArray buffer;

// populate the buffer with "-r" and "--raw"
buffer[0] = "-r";
buffer[1] = "--raw";

// call Contains() method and pass inline array as a ReadOnlySpan
bool raw = pico.Contains(buffer);

// Note, no array to garbage collect. Just cleaned up with the stack frame.
```

Before .NET 9, the code would have looked like this:

```csharp
// .NET 8 VERSION

// Same as-written code:
bool raw = pico.Contains("-r", "--raw");

// ===========================================================
// Desugared code:

// heap-allocate an array
string[] array = new string[2];

// populate the array with "-r" and "--raw"
array[0] = "-r";
array[1] = "--raw";

// call Contains() method and pass the array
bool raw = pico.Contains(array);

// array now requires garbage collection
```

This looks shorter but its less efficient because of the additional heap allocation.

See https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-9/#span-span-and-more-span for more information.

## Tests

[![PicoArgs compile and test](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml/badge.svg)](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml)

Tests are written using xUnit and can be run with `dotnet test`.
