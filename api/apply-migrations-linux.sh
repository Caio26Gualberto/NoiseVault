#!/bin/bash

echo "=========================================="
echo "        NOISEVAULT MIGRATOR"
echo "=========================================="
echo

# Vai para a pasta onde está o script
cd "$(dirname "$0")"

dotnet ef database update \
  --project src/NoiseVault.Infra.Data \
  --startup-project src/NoiseVault.Api

echo
echo "Done."
