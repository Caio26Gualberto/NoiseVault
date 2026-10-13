#!/bin/bash

echo "=========================================="
echo "     HEADBANGER MIGRATION CREATOR"
echo "=========================================="
echo

# Vai para a pasta onde está o script
cd "$(dirname "$0")"

# Pergunta o nome da migration
read -p "Enter migration name: " MIGRATION_NAME

# Validação
if [ -z "$MIGRATION_NAME" ]; then
  echo
  echo "Migration name cannot be empty."
  exit 1
fi

echo
echo "Creating migration \"$MIGRATION_NAME\"..."
echo

dotnet ef migrations add "$MIGRATION_NAME" \
  --project src/Headbanger.Infra.Data \
  --startup-project src/Headbanger.Api

echo
echo "Done."
