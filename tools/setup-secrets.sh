#!/bin/bash

# Setup Snowflake User Secrets from .env file and private key
# Usage: ./tools/setup-secrets.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="$PROJECT_ROOT/.env"
SAMPLE_PROJECT="$PROJECT_ROOT/SnowflakeProxy.Sample.Server"

echo "🔧 Snowflake User Secrets Setup"
echo "================================"
echo ""

# Check if .env exists
if [ ! -f "$ENV_FILE" ]; then
    echo "❌ Error: .env file not found at $ENV_FILE"
    exit 1
fi

# Source the .env file
source "$ENV_FILE"

# Validate required variables
if [ -z "$SNOWFLAKE_ACCOUNT" ]; then
    echo "❌ Error: SNOWFLAKE_ACCOUNT not set in .env"
    exit 1
fi

if [ -z "$SNOWFLAKE_USER" ]; then
    echo "❌ Error: SNOWFLAKE_USER not set in .env"
    exit 1
fi

if [ -z "$SNOWFLAKE_PRIVATE_KEY_PATH" ]; then
    echo "❌ Error: SNOWFLAKE_PRIVATE_KEY_PATH not set in .env"
    exit 1
fi

if [ -z "$SNOWFLAKE_PRIVATE_KEY_PASSPHRASE" ]; then
    echo "❌ Error: SNOWFLAKE_PRIVATE_KEY_PASSPHRASE not set in .env"
    exit 1
fi

# Check if private key file exists
if [ ! -f "$SNOWFLAKE_PRIVATE_KEY_PATH" ]; then
    echo "❌ Error: Private key file not found at $SNOWFLAKE_PRIVATE_KEY_PATH"
    exit 1
fi

echo "✅ Found .env file"
echo "✅ Found private key file"
echo ""

# Store the private key FILE PATH (not content)
# The DirectSnowflakeService will use private_key_file parameter for local dev
echo "📖 Using private key file path: $SNOWFLAKE_PRIVATE_KEY_PATH"
echo "✅ Private key file exists and is accessible"
echo ""

# Change to sample project directory
cd "$SAMPLE_PROJECT"

# Set user secrets
echo "🔐 Setting user secrets..."
echo ""

dotnet user-secrets set "Snowflake:Account" "$SNOWFLAKE_ACCOUNT"
dotnet user-secrets set "Snowflake:User" "$SNOWFLAKE_USER"
dotnet user-secrets set "Snowflake:PrivateKey" "$SNOWFLAKE_PRIVATE_KEY_PATH"
dotnet user-secrets set "Snowflake:PrivateKeyPassword" "$SNOWFLAKE_PRIVATE_KEY_PASSPHRASE"

# Set defaults for other values (can be overridden in .env if needed)
dotnet user-secrets set "Snowflake:Warehouse" "${SNOWFLAKE_WAREHOUSE:-COMPUTE_WH}"
dotnet user-secrets set "Snowflake:Database" "${SNOWFLAKE_DATABASE:-SNOWFLAKE_SAMPLE_DATA}"
dotnet user-secrets set "Snowflake:Schema" "${SNOWFLAKE_SCHEMA:-PUBLIC}"
dotnet user-secrets set "Snowflake:Role" "${SNOWFLAKE_ROLE:-ACCOUNTADMIN}"

echo ""
echo "✅ User secrets configured successfully!"
echo ""
echo "📋 Current secrets:"
dotnet user-secrets list | grep "Snowflake:" | grep -v "PrivateKey" | grep -v "PrivateKeyPassword"
echo "   Snowflake:PrivateKey = [REDACTED]"
echo "   Snowflake:PrivateKeyPassword = [REDACTED]"
echo ""
echo "🚀 You can now run: cd SnowflakeProxy.Sample.Server && dotnet run"