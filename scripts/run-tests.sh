#!/usr/bin/env bash
# Runs the full test suite in a container. Testcontainers starts Postgres through the
# host Docker socket, so --network host lets the test process reach the mapped port.
set -euo pipefail
cd "$(dirname "$0")/.."
docker run --rm --network host \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD":/work -w /work \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc 'dotnet test tests/CoachingPlatform.Tests/CoachingPlatform.Tests.csproj -v minimal'
