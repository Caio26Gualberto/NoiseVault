#!/bin/bash
set -euo pipefail

REPO=${REPO:-https://github.com/Caio26Gualberto/NoiseVault.git}
TARGET=${TARGET:-$HOME/noisevault}

sudo apt-get update
sudo apt-get upgrade -y
sudo apt-get install -y git unzip ca-certificates curl

if ! command -v docker >/dev/null 2>&1; then
  curl -fsSL https://get.docker.com | sudo sh
fi
sudo usermod -aG docker "$USER"

if ! command -v aws >/dev/null 2>&1; then
  curl -fsSL "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o /tmp/awscliv2.zip
  unzip -q -o /tmp/awscliv2.zip -d /tmp
  sudo /tmp/aws/install
  rm -rf /tmp/aws /tmp/awscliv2.zip
fi

if [ ! -f /swapfile ]; then
  sudo fallocate -l 2G /swapfile
  sudo chmod 600 /swapfile
  sudo mkswap /swapfile
  sudo swapon /swapfile
  echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab >/dev/null
fi

if [ ! -d "$TARGET/.git" ]; then
  git clone "$REPO" "$TARGET"
fi

cd "$TARGET/infra/lightsail"
if [ ! -f .env ]; then
  cp .env.example .env
  chmod 600 .env
fi
chmod +x backup.sh

echo
echo "Pronto. Falta, nesta ordem:"
echo "  1. nano $TARGET/infra/lightsail/.env   (preencher os segredos)"
echo "  2. docker login ghcr.io -u <usuario-github>   (token com read:packages)"
echo "  3. sair do SSH e entrar de novo, para o grupo docker valer"
echo "  4. fazer o push na main, ou rodar o workflow 'Deploy API' no GitHub"
