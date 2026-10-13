#!/bin/bash
set -euo pipefail

BUCKET=noisevault-media

awslocal s3api head-bucket --bucket "$BUCKET" 2>/dev/null || awslocal s3api create-bucket --bucket "$BUCKET"

awslocal s3api put-bucket-policy --bucket "$BUCKET" --policy "{
  \"Version\": \"2012-10-17\",
  \"Statement\": [{
    \"Sid\": \"PublicRead\",
    \"Effect\": \"Allow\",
    \"Principal\": \"*\",
    \"Action\": \"s3:GetObject\",
    \"Resource\": \"arn:aws:s3:::$BUCKET/*\"
  }]
}"

echo "noisevault: bucket $BUCKET pronto"
