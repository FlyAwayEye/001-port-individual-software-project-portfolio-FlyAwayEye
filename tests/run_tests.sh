#!/usr/bin/env bash
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$HERE/.." && pwd)"
cd "$REPO_ROOT"

echo "Running integration tests against the published app..."

PUBLISH_DIR="$REPO_ROOT/out"
if [ ! -d "$PUBLISH_DIR" ]; then
  echo "Publish directory '$PUBLISH_DIR' not found. Did build/publish succeed?"
  ls -la || true
  exit 1
fi

# Prefer a DLL (framework-dependent), otherwise look for an executable file
PUBLISHED_TARGET=""
# Find the first .dll in out
PUBLISHED_DLL="$(find "$PUBLISH_DIR" -maxdepth 1 -type f -name "*.dll" -print -quit || true)"
if [ -n "$PUBLISHED_DLL" ]; then
  PUBLISHED_TARGET="$PUBLISHED_DLL"
else
  # find any executable file (non-directory) in out
  PUBLISHED_EXE="$(find "$PUBLISH_DIR" -maxdepth 1 -type f -perm /111 -print -quit || true)"
  if [ -n "$PUBLISHED_EXE" ]; then
    PUBLISHED_TARGET="$PUBLISHED_EXE"
  fi
fi

if [ -z "$PUBLISHED_TARGET" ]; then
  echo "No published DLL or executable found in $PUBLISH_DIR"
  echo "Contents:"
  ls -la "$PUBLISH_DIR"
  exit 1
fi

echo "Using published target: $PUBLISHED_TARGET"

# Helper to run an expect script passing the target path as first argument
run_expect() {
  local script="$1"
  echo "Running expect script: $script"
  /usr/bin/expect -f "$script" -- "$PUBLISHED_TARGET"
}

# Run the ESC key test (must exit within ~1 second)
if ! run_expect "$REPO_ROOT/tests/expect_scripts/esc_test.exp"; then
  echo "ESC test failed."
  exit 1
fi
echo "ESC test passed."

# Run login probing test (best-effort: tries candidate codes)
if ! run_expect "$REPO_ROOT/tests/expect_scripts/login_test.exp"; then
  echo "Login probe test failed."
  exit 1
fi
echo "Login probe test passed."

echo "Integration tests completed successfully."
exit 0