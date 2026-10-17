#!/bin/bash
set -euo pipefail

cd "$(dirname "$0")"

BUCKET=${BACKUP_BUCKET:-noisevault-media}
PREFIX=${BACKUP_PREFIX:-backups/postgres}
STAMP=$(date -u +%Y%m%d-%H%M%S)
FILE=/tmp/noisevault-$STAMP.dump

docker compose exec -T postgres pg_dump -Fc -U noisevault noisevault > "$FILE"
aws s3 cp "$FILE" "s3://$BUCKET/$PREFIX/noisevault-$STAMP.dump" --only-show-errors
rm -f "$FILE"

echo "backup ok: $PREFIX/noisevault-$STAMP.dump"
