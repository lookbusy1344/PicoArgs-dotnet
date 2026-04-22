---
name: pre-commit
description: Use when about to create a git commit in this project - runs the required verification sequence before committing
---

# Pre-Commit Checklist

Run these steps in order before every commit. All must pass cleanly.

```bash
dotnet build --configuration Debug --no-restore
dotnet format PicoArgs-dotnet.sln
gtimeout 60 dotnet test --no-restore
```

**Tests must pass** — do not commit with failing tests. If a test fails unexpectedly, investigate before touching the test.
