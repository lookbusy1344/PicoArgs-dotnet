---
name: pre-commit
description: Use when about to create a git commit in this project - runs the required verification sequence before committing
---

# Pre-Commit Checklist

Run these steps in order before every commit. All must pass cleanly.

```bash
dotnet format PicoArgs-dotnet.sln
dotnet build --configuration Debug --no-restore
gtimeout 60 dotnet test --no-restore
```

**Format first** — `dotnet format` modifies files in-place, so run it before staging to avoid a dirty tree after the fact.

**Tests must pass** — do not commit with failing tests. If a test fails unexpectedly, investigate before touching the test.
