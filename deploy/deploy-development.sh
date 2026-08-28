#!/usr/bin/env bash
set -euo pipefail

# Deploys the content-management-api image to the Development environment.
# Usage: ./deploy-development.sh <image-tag>

az config set extension.use_dynamic_install=yes_without_prompt

az containerapp update \
  --name content-management-api \
  --container-name content-management-api \
  --resource-group ContentManagement_Development \
  --image "${ACR_REGISTRY}/content-management-api:$1"
