# Project Guidelines

## Version control (read first)

Before the first VCS command, run `jj --ignore-working-copy root`. This may be a jj repo on one machine and plain Git on another.

**IMPORTANT:** if it succeeds, use `jj` for all VCS commands, including `log`, `show`, `status` and `diff`. Do not run `git` on jj repos. The `gitStatus` snapshot in the session context is not a reason to use git.

jj has no commit hooks. Run the pre-commit checks before `jj commit`, `jj describe` (when finalising a change) and `jj squash`.
If a change touches only non-code files (`*.md`), skip the dotnet steps.

Before `jj git push` or moving a shared bookmark, run `scripts/pre-push.sh`. By default it formats the tip (newest non-empty mutable revision) with `jj fix` (`scripts/format-stdin.sh`, whitespace rules), then runs restore, build, `dotnet format --verify-no-changes` and tests on it. `--full` formats every mutable revision in `::@` and checks each in its own checkout via `jj run`. `scripts/agent-pre-push-hook.sh` runs the tip check on every push command and blocks the push on failure. Claude Code (`.claude/settings.json`) and Codex (`.codex/hooks.json`) call it as a `PreToolUse` hook. The hook checks the tip of `@`, not the bookmark being pushed, so push only the bookmark you are working on: the one on `@-`, with an empty `@` above it. In a plain Git checkout it formats the working tree with `dotnet format` and runs the same checks, and `git push` triggers the same hook. CI builds and tests pushes to `main`, `dotnet8` and `dotnet9`.

Push only on explicit request. "Push this" means: if `@` is non-empty, `jj commit` it (after the pre-commit checks). Move the bookmark to `@-` with `jj bookmark set <name> -r @-`, then `jj git push --bookmark <name>`. Use the bookmark already on the stack; otherwise `main`. Never push any other bookmark. Do not use `jj git push -c`.

## Personal information

Exclude PII from every commit, commit message and bookmark name: real names, email addresses, usernames, machine paths such as `/Users/<name>/`, hostnames, tokens and credentials. Check the diff before `jj commit`, `jj describe` (finalising) and `git commit`.

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

- **IMPORTANT** Every `dotnet` Bash call must set `dangerouslyDisableSandbox: true` (build, test, format, run, restore, publish, and any `gtimeout`
  -wrapped variants). The Claude Code sandbox blocks `dotnet` even when listed in `excludedCommands`: MSBuild's Unix-domain sockets for diagnostic IPC
  and worker-node communication fail under `network-inbound` deny, and the EPERM surfaces as a silent generic build failure.
