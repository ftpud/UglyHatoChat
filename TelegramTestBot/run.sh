#!/bin/sh
set -eu

bot_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
if [ -f "$bot_dir/.env" ]; then
    set -a
    . "$bot_dir/.env"
    set +a
fi

: "${TELEGRAM_BOT_DATA_DIR:=$bot_dir/.data}"
export TELEGRAM_BOT_DATA_DIR

if [ -z "${TELEGRAM_BOT_TOKEN:-}" ]; then
    echo "Add your BotFather token to TelegramTestBot/.env (TELEGRAM_BOT_TOKEN)." >&2
    exit 1
fi

if command -v dotnet >/dev/null 2>&1; then
    dotnet_cmd=dotnet
elif [ -x /tmp/ugly-dotnet/dotnet ]; then
    dotnet_cmd=/tmp/ugly-dotnet/dotnet
else
    echo "Install the .NET 6 SDK and make dotnet available on PATH." >&2
    exit 1
fi

exec "$dotnet_cmd" run --project "$bot_dir/TelegramTestBot.csproj"
