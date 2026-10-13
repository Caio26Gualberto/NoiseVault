#!/bin/bash

echo "=========================================="
echo "        HEADBANGER MIGRATOR"
echo "=========================================="
echo

# Vai para a pasta onde está o script
cd "$(dirname "$0")"

dotnet ef database update \
  --project src/Headbanger.Infra.Data \
  --startup-project src/Headbanger.Api

echo
echo "Done."
