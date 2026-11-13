#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
sdk="${BIBLE_SURVIVOR_DOTNET:-$HOME/.local/share/bible-survivor/dotnet/dotnet}"
if [[ ! -x "$sdk" ]]; then
  if command -v dotnet >/dev/null 2>&1; then sdk="$(command -v dotnet)";
  else printf '%s\n' 'BLOCKED: .NET SDK missing. Set BIBLE_SURVIVOR_DOTNET to a dotnet executable.' >&2; exit 3; fi
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_CLI_HOME="$root/.local/dotnet-home"
cd "$root"
exec "$sdk" "$@"
