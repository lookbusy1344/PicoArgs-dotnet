# PicoArgs-dotnet

[![PicoArgs compile and test](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml/badge.svg)](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml)
[![CodeQL](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/github-code-scanning/codeql)

A tiny command line argument parser for dotnet, inspired by the Rust pico-args library https://github.com/RazrFalcon/pico-args

Like the Rust library, this library is intended to be used for small command line tools, where you don't want to use a full blown argument parser like CommandLineParser https://github.com/commandlineparser/commandline

PicoArgs-dotnet's features are intentionally very minimal:

- Only one file to add, no dependencies
- Supports flags, options and positional arguments
- Supports short and long options
- Supports multiple values for options
- NO support for default help generation, you need to do this manually
- NO support for conversions, all arguments are strings (all flags are bools) unless you convert them yourself
- Tiny API, about 120 lines of code
- Written for .NET 8/7 but should work with earlier versions

Order of argument consumption is important. Once consumed an argument is removed from the available list. Once all your expected arguments have been consumed, you can check for any unexpected arguments with ```Finished()```.

## Usage
No nuget packages, just add ```PicoArgs.cs``` to your project. Then in your code:

```csharp
var pico = new PicoArgs(args); // construct with command line arguments string[]

bool raw = pico.Contains("-r", "--raw"); // returns true if either flag is present
string[] files = pico.GetMultipleParams("-f", "--file"); // returns string[] with zero length if none found
string exclude = pico.GetParamOpt("-e", "--exclude") ?? "example-exclude"; // specifying a default

pico.Finished(); // we are finished, check for extra unwanted arguments & throw is any are left over

```

More examples can be found in the ```Program.cs``` file.


There is a ```PicoArgsDisposable``` class which implements ```IDisposable``` to automatically throw on extra params. This may or may not be to your taste:

```csharp
using var pico = new PicoArgsDisposable(args);

bool raw = pico.Contains("-r", "--raw");
// Finished() called to check for extra arguments, when pico goes out of scope
```

## Tests

[![PicoArgs compile and test](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml/badge.svg)](https://github.com/lookbusy1344/PicoArgs-dotnet/actions/workflows/test.yml)

Tests are written using xUnit and can be run with `dotnet test`. There is also a Github Actions workflow which runs the tests on push and pull request.
