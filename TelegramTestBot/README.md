# Telegram test bot

A separate application built on the sibling `../UglyAppFw` framework. The bot
owns its startup, view model, views, and tests, and uses the framework's async
polling, session and rendering APIs. Its host filters input to private text chats.

Requires the .NET 6 SDK and a Telegram bot token from **@BotFather** (`/newbot`).

Paste the BotFather token into `TelegramTestBot/.env`:

```dotenv
TELEGRAM_BOT_TOKEN='your-bot-token'
```

The `.env` file is ignored by Git; `.env.example` is a shareable template.
It uses shell assignment syntax and is loaded by `run.sh`.
From the workspace root:

```bash
sh ./TelegramTestBot/run.sh
```

Or select **telegram-test-bot** in HatoPad's project-local run configurations.
The launcher uses `dotnet` on PATH, falling back to `/tmp/ugly-dotnet/dotnet`
in this environment. The run configuration is saved without starting the bot.

To run directly without the launcher, set the environment variable yourself:

```bash
export TELEGRAM_BOT_TOKEN='your-bot-token'
dotnet run --project ./TelegramTestBot
```

If using the SDK installed in this environment, replace `dotnet` with
`/tmp/ugly-dotnet/dotnet`.

Open a private chat with your bot and send `/start` to reach **HatoChat** 🐦, a
reactive group chat shared by everyone talking to the bot. All state is in memory
and resets when the application restarts: no database.

| Page | What it does |
| --- | --- |
| Lobby (start page) | Register a unique nickname (2–16 letters/digits/`_`/`-`, case-insensitive, some names reserved), pick an avatar, see who's online and the latest message, enter the chat |
| Chat | Scrollable history, a `💬 Message` button (type, and it's posted), quick 🎲 Roll / 🪙 Flip / 👋 Wave buttons, Settings, People, Leave |
| Settings | Toggle timestamps and @mention highlighting, choose messages per page (5/8/12) |
| Who's here | Online list, chattiest-users leaderboard and the list of chat tricks |

In the chat, just type to post: your message is deleted and appears in the
history. Or tap **💬 Message**, type, and it's posted in one step. Too-long text
and unknown commands are rejected inside the dialog so you can fix them. When someone else posts, joins or
leaves, every open chat, lobby and people page re-renders on its own. The
history follows new messages (📌 Auto). Scrolling up (⏫ 🔼) switches to ✋ Manual
mode, which stays put and counts `▼ N newer`. Scrolling back down to the bottom,
pressing ⏬, or sending a message resumes auto-scroll. The 📌/✋ button toggles the
mode directly.

Tricks: `/me dances`, `/roll [sides]`, `/flip`, `/8ball question`,
`/shrug [text]`, `/hug nick`, `/help`. Mention someone with `@nick`, and say
"hato" to summon the resident pigeon. Messages are limited to 300 characters;
the window trims the oldest visible lines to stay within Telegram's message size.

Non-text messages and group chats are ignored. Long polling requires no public
URL; use a bot with no webhook and run one instance per token. Keep the process
running; press Ctrl+C to stop. Keep your token out of source control.

```bash
dotnet build ./TelegramTestBot/TelegramTestBot.csproj
dotnet test ./TelegramTestBot/Tests/TelegramTestBot.Tests.csproj
```

Tests use a fake Telegram connection and require no token or network calls.

## Fresh UI after a restart

The first accepted message in a new session removes the bot's tracked previous
views before displaying the new page. This includes parent pages and dialogs
left open when the process stopped. Incoming text messages are deleted after
processing, including commands, validation failures and dialog submissions. The
host enforces this policy for all pages and shared dialogs. Already-existing user
message history is not swept.

`run.sh` keeps the UI message journal under the Git-ignored
`TelegramTestBot/.data/ui-messages/`. Set `TELEGRAM_BOT_DATA_DIR` to override the
data directory. When bypassing the launcher, its default is `.data` beside the
compiled application. The journal stores only IDs/timestamps; application state
still resets on restart. Keep the data directory across restarts for cleanup.

Telegram generally only permits deleting messages less than 48 hours old. Views
sent before journaling was enabled are not discoverable and need manual cleanup.
Expired callback acknowledgements are ignored so an old query timeout no longer
prevents a button action from being dispatched.

The chat is built from shared `UglyFwUI` controls: `FormTextBox` (nickname),
`FormInputButton` (message), `FormPickerButton` (avatar, page size), `FormButton`, `FormToggle` and
`FormScrollBox`, composed by `FormLayout`. Cross-user updates go through the
framework's `ViewRefreshQueue`. `Services/ChatRoom` only enqueues the other
users' pages, and the queue renders each one under its own session lock.

See `../ARCHITECTURE.md` for the workspace layout. Application rendering belongs
in `Views/`, state and handlers in `ViewModels/`, the shared room, nickname
registry and commands in `Services/`, records in `Models/`, and polling/filtering in `Hosting/`.
