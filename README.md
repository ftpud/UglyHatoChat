# UglyHatoChat 🐦

A workspace around [UglyAppFw](https://github.com/ftpud/UglyAppFw), a kind of
ugly (but async, MVVM and well-tested) framework for Telegram bot UIs, plus a
shared control library and **HatoChat**, a reactive in-memory multi-user chat bot.

| Project | What it is |
| --- | --- |
| `UglyAppFw/` (submodule) | Framework: DI, navigation, per-chat/user sessions, rendering, cross-session view refresh |
| `UglyFwUI/` | Reusable controls: text/picker dialogs, text box, input button, picker, toggle, scroll box, `FormLayout` |
| `TelegramTestBot/` | HatoChat: nickname lobby, live chat with auto/manual scrolling, settings, leaderboard, dice and a pigeon |

```bash
git clone --recurse-submodules <this repo>
dotnet test UglyWorkspace.sln          # offline, no token needed
cp TelegramTestBot/.env.example TelegramTestBot/.env   # add your BotFather token
sh TelegramTestBot/run.sh
```

Requires the .NET 6 SDK. See `ARCHITECTURE.md` for layout and conventions and
`TelegramTestBot/README.md` for the chat's features.
