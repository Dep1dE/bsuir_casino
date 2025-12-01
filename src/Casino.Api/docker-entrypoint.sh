#!/bin/bash
set -e

if [ -z "$DOTNET_RUNNING_IN_CONTAINER" ]; then
    export DOTNET_RUNNING_IN_CONTAINER=true
fi

if command -v dotnet &> /dev/null && dotnet --version &> /dev/null; then
    dotnet dev-certs https --trust 2>/dev/null || true
fi

exec dotnet Casino.Api.dll "$@"

