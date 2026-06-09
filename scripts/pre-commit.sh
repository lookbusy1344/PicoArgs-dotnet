#!/usr/bin/env bash

# Install with:
#   ln -sf ../../scripts/pre-commit.sh .git/hooks/pre-commit
#   chmod +x .git/hooks/pre-commit
#
# Or append to an existing .git/hooks/pre-commit:
# if [ ! -x "scripts/pre-commit.sh" ]; then
#     echo "Missing executable scripts/pre-commit.sh" >&2
#     exit 1
# fi
#
# exec ./scripts/pre-commit.sh


set -euo pipefail

readonly SOLUTION_PATH="PicoArgs-dotnet.sln"
readonly TEST_TIMEOUT_SECONDS="60"

is_documentation_file() {
  local path="$1"

  [[ "$path" == *.md ]] \
    || [[ "$path" == docs/* ]] \
    || [[ "$(basename "$path")" == README* ]]
}

requires_dotnet_checks() {
  local path="$1"

  [[ "$path" == *.cs ]] \
    || [[ "$path" == *.csproj ]] \
    || [[ "$path" == *.sln ]] \
    || [[ "$path" == *.editorconfig ]] \
    || [[ "$path" == *.props ]] \
    || [[ "$path" == *.targets ]]
}

collect_staged_files() {
  local staged_file

  while IFS= read -r staged_file; do
    [[ -n "$staged_file" ]] && printf '%s\n' "$staged_file"
  done < <(git diff HEAD --name-only --diff-filter=ACMR)
}

run_dotnet_checks() {
  echo "Running pre-commit checks for staged .NET files..."
  dotnet build --configuration Debug --no-restore
  dotnet format "$SOLUTION_PATH" --verify-no-changes
  gtimeout "$TEST_TIMEOUT_SECONDS" dotnet test --no-restore
}

main() {
  local staged_files=()
  local staged_file

  while IFS= read -r staged_file; do
    staged_files+=("$staged_file")
  done < <(collect_staged_files)

  if [[ "${#staged_files[@]}" -eq 0 ]]; then
    echo "No modified files found."
    exit 0
  fi

  local has_non_documentation_files="false"
  local has_dotnet_files="false"

  for staged_file in "${staged_files[@]}"; do
    if ! is_documentation_file "$staged_file"; then
      has_non_documentation_files="true"
    fi

    if requires_dotnet_checks "$staged_file"; then
      has_dotnet_files="true"
    fi
  done

  if [[ "$has_non_documentation_files" == "false" ]]; then
    echo "Documentation-only commit detected. Skipping .NET checks."
    exit 0
  fi

  if [[ "$has_dotnet_files" == "false" ]]; then
    echo "No modified .NET source or build files detected. Skipping .NET checks."
    exit 0
  fi

  run_dotnet_checks
}

main "$@"
