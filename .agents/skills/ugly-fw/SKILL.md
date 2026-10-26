---
name: ugly-fw
description: Implement and refactor C# Telegram bots using this workspace's UglyAppFw async framework, including callbacks, navigation, DI, per-chat/user sessions and cross-session view refresh.
---

Read `ARCHITECTURE.md` at the workspace root for project boundaries and folder
conventions; read `UglyAppFw/README.md` when changing framework APIs or lifecycle.

## Place the change

- `UglyAppFw/UglyAppFramework` owns generic state/navigation and DI;
  `UglyAppFw/UglyTgApplication` owns polling, Telegram sessions, rendering
  transport and the cross-session `ViewRefreshQueue`.
- Shared presentation controls go in the sibling `UglyFwUI` project. App pages,
  domain validation, models and repository work stay in the application.
- Application presentation folders are `Views/`, `ViewModels/`, `Models/`,
  `Services/`, `Hosting/` and `Tests/` when needed. Namespaces mirror folders.
  Preserve the framework's existing infrastructure folders.
- Add projects to root `UglyWorkspace.sln`. Exclude nested test sources from the
  parent csproj with `<Compile Remove="Tests/**/*.cs" />`.
- Avoid application type names that collide with `Telegram.Bot.Types`
  (e.g. `ChatMember`, `Message`, `User`) in files that import that namespace.

## Work with the async state model

A page extends `UglyTgApplication.States.ViewModel`, has
`[BindView(typeof(PageView))]`, and is constructed per session. Starting states
need a public parameterless constructor; parameterized dialogs are pushed from
a parent. Views implement `IView`, use `[Managed]`, and stay stateless: DI-managed
dependencies are singletons. Keep per-user fields out of shared services/views.
`[Inject]` properties on a view model are set when it is pushed, so use them in
callbacks/lifecycle methods, not in the constructor body.

Use Task-returning attributed callbacks accepting `Update` and optionally
`CancellationToken`; async void is rejected. For dynamic handlers use
`RegisterCallback(id, async (update, token) => { ... })`. Override
`OnMessageAsync(Update, CancellationToken = default)` for message processing;
`UpdateAsync` is the framework dispatcher, not an application override point.
`OnMessageAsync` also runs after button callbacks that did not navigate, so read
`update.Message?.Text` to handle typed text only.

Await `PushAsync`, `PopAsync`, `UpdateViewAsync` and all service work. Lifecycle
overrides are `InitializeAsync`, `ActivateAsync` and `UnloadAsync`; propagate
the token and call the base implementation when retaining rendering/cleanup.
Property setters cannot await: `SetProperty` changes the field only, and the
handler explicitly awaits rendering. Do not add .Result/.Wait or detached requests.

A child dialog updates parent data in an awaited submission callback, then pops;
the parent's activation renders that data. Popping a root is a no-op. A failed
initialization restores the parent and attempts to clean its partial view.

Session identity is `(chatId, userId)`. `Context.CurrentChatId` is the destination;
`Context.UserId` identifies the sender. Use `GetCurrentContext(chatId, userId)`,
not the obsolete private-chat-only accessor. Dispatch serializes each session;
do not call `ResetSessionAsync` inside its callback, because reset takes the same
lock. Contexts are in memory without automatic expiry or application-state persistence.

## Update other users' views

Shared state (chat rooms, boards) lives in a `[Managed]` service with its own lock.
To re-render other sessions, inject `ViewRefreshQueue` into the service and call
`Request(viewModel)` for each watching page except the origin (which renders
itself in its awaited handler). Never await another session's render from a
handler: two sessions waiting on each other's locks deadlock. The queue is
started by `UglyTgApp.Load`, coalesces duplicates, takes each target's session
lock and renders only if the page is still current (covered pages render on
reactivation). Pages subscribe in `InitializeAsync` and unsubscribe in
`UnloadAsync`. Offline tests drain it with `await queue.ProcessPendingAsync()`.
See `TelegramTestBot/Services/ChatRoom.cs`.

## UI message journal and input removal

UI message IDs are separate cleanup metadata. UglyTgApp accepts an optional
`IUiMessageStore`: the default is in memory; `JsonUiMessageStore(directory)`
survives restarts. Before initializing a fresh session, the dispatcher cleans
tracked previous views. The test bot enables JSON journaling under its ignored
.data directory, configurable with TELEGRAM_BOT_DATA_DIR. Track only known bot
view IDs; do not guess arbitrary chat history IDs. Telegram deletion limits
still apply, and pre-journal messages cannot be recovered automatically.
Use the host's nullable `RemoveUserMessagesPolicyOverride` to enforce one input
removal policy across all pages and shared dialogs; null honors each state's
`RemoveUserMessagesPolicyEnabled`. The test bot sets the override to true and
deletes newly processed text after dispatch, including validation failures and
chat messages (which then appear in the shared history).
Expired/invalid callback acknowledgement errors are ignored narrowly so the
action can still dispatch; unrelated request errors must remain visible.

## Verify and maintain

Use `dotnet build UglyWorkspace.sln` and affected offline tests (or
`dotnet test UglyWorkspace.sln` for changes spanning layers). The local SDK is
`/tmp/ugly-dotnet/dotnet` if dotnet is absent from PATH. A running apphost can
lock its output; use `-p:UseAppHost=false` for verification rather than stopping
a user's process. Fake Telegram transport tests need no real bot token; for
multi-user tests record each request's `chat_id` and simulate several users.

The runnable example is `TelegramTestBot` (HatoChat: lobby with in-memory nickname
registration, reactive chat, settings, people). `Hosting/TestBotHost.cs`
customizes input filtering and `Program.cs` owns lifetime. Its `run.sh` loads the
ignored .env. Keep credentials out of tracked files and logs. Update architecture,
migration documentation and relevant workspace skill guidance when APIs or
folder conventions change.
