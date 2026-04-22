# CLAUDE.md

## Project Structure

PicoArgs-dotnet is a single-file command line argument parser library for .NET, inspired by the Rust pico-args library. The project consists of:

- `PicoArgs.cs` - The main library (single file, no dependencies)
- `Program.cs` - Demo/example usage
- `TestPicoArgs/` - xUnit test project

Key architectural concepts:
- Single-file library design with no external dependencies
- Order-dependent argument consumption (consumed arguments are removed)
- Uses `ReadOnlySpan<string>` parameters in .NET 9 for performance optimization
- Supports both regular `PicoArgs` class and disposable `PicoArgsDisposable` wrapper

## Common Commands

### Build and Test
```bash
dotnet restore
dotnet build --configuration Debug --no-restore
dotnet test --no-restore
dotnet format PicoArgs-dotnet.sln
```

### Run the demo application
```bash
dotnet run -- --help
dotnet run -- --raw -i file1.txt -i file2.txt --exclude something
```

### Run specific tests
```bash
dotnet test --filter "TestMethodName"
dotnet test --filter "ClassName"
```

## Committing

Run these steps in order before every commit. All must pass cleanly.

```bash
dotnet build --configuration Debug --no-restore
dotnet format PicoArgs-dotnet.sln
gtimeout 60 dotnet test --no-restore
```

**Tests must pass** — do not commit with failing tests. If a test fails unexpectedly, investigate before touching the test.

## Development Guidelines

### Code Style
- Modern C# 14 idioms using .NET 10 features
- Functional style preferred
- Unneeded return values should always have explicit discards: `_ = func()`

### Library Design Principles
- Intentionally minimal feature set
- Single file with no dependencies
- Order-dependent argument consumption
- Manual help generation (no automatic help)
- All arguments are strings (except flags which are bools)