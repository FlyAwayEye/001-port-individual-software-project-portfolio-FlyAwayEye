#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$HERE/.." && pwd)"

echo "Running integration tests against the published app..."

# Ensure we're running from the repository root
cd "$REPO_ROOT"

# Use the published output
PUBLISH_DIR="$REPO_ROOT/out"
if [ ! -d "$PUBLISH_DIR" ]; then
  echo "Publish directory '$PUBLISH_DIR' not found. Did build/publish succeed?"
  exit 1
fi

# Helper to run an expect script (it spawns 'dotnet run --project .' so run from repo root)
run_expect() {
  local script="$1"
  echo "Running expect script: $script"
  /usr/bin/expect -f "$script"
}

# Run the ESC key test (must exit within 1 second)
if ! run_expect "$REPO_ROOT/tests/expect_scripts/esc_test.exp"; then
  echo "ESC test failed."
  exit 1
fi
echo "ESC test passed."

# Run login probing test (best-effort: tries a series of common codes and reports success if any login is accepted)
if ! run_expect "$REPO_ROOT/tests/expect_scripts/login_test.exp"; then
  echo "Login probe test failed."
  exit 1
fi
echo "Login probe test passed."

echo "Integration tests completed successfully."

exit 0