# Workspace structure

The workspace has three layers. Dependencies point toward the framework:

```text
TelegramTestBot / UglyDemo -> UglyFwUI -> UglyTgApplication -> UglyAppFramework
```

Applications can also reference the framework directly. Framework libraries do
not reference UI controls or applications. `UglyDemo` happens to live inside the
`UglyAppFw` checkout; it is still an application, not part of the framework core.
Its project now references the sibling `UglyFwUI` library.

| Location | Responsibility |
| --- | --- |
| `UglyAppFw/UglyAppFramework/` | DI, generic state contracts and navigation |
| `UglyAppFw/UglyTgApplication/` | Telegram polling, per-chat/user sessions, dispatch, rendering transport |
| `UglyFwUI/` | Reusable dialogs, form elements, safe text rendering |
| `TelegramTestBot/` | Runnable test bot: HatoChat, a reactive in-memory multi-user chat |
| `UglyAppFw/UglyDemo/` | ToDo domain example using shared dialogs |
| `.agents/skills/` | Versioned workspace skills managed through HatoPad configuration |

## Folder and naming conventions

Use these folder names for application presentation code and reusable controls.
Create a folder when it has code to hold; no empty placeholders are required.

```text
<project>/
  <project>.csproj
  Program.cs                 # Executable composition/lifetime, if applicable
  Hosting/                   # Polling customization and input filtering, if needed
  Views/                     # <Feature>View.cs: IView and pure formatting
  ViewModels/                # <Feature>ViewModel.cs: state and interaction handlers
  Models/                    # Domain data / typed option records
  Services/                  # Domain operations and repository contracts/implementations
  Forms/                     # Reusable field/button composition (UglyFwUI)
  Rendering/                 # Shared pure formatting/layout helpers (UglyFwUI)
  Tests/                     # Separate test csproj and behavior tests
```

Namespaces mirror project and folder names, for example `UglyFwUI.Views` and
`TelegramTestBot.ViewModels`. Shared controls belong in `UglyFwUI`; application
pages and domain rules belong in the application. Framework infrastructure keeps
its existing specialized folders (`StateManage`, `DependencyManager`, `States`,
`Entity`, `View`, `Attributes`); it does not adopt presentation folder names.

Test sources nested under a library/executable are excluded from the parent
project with `<Compile Remove="Tests/**/*.cs" />`. Add new projects to the root
`UglyWorkspace.sln`. Do not add a project reference back from framework core to UI.

## View / ViewModel / model boundary

A view is a stateless `[Managed]` `IView`. `Display(IState)` reads its bound view
model and returns `ViewResponse`; it makes no network calls, registers no
callbacks, changes no state, and contains no repository operations. Escape
untrusted text with `HtmlText.Encode` before inserting it into HTML responses.

A view model extends `UglyTgApplication.States.ViewModel` and has
`[BindView(typeof(MyView))]`. It owns UI state, validation decisions, callbacks and
navigation. Create it per session with `new`; managed DI services/views are
singletons, so do not store per-user state there. Starting view models need a
public parameterless constructor. Parameterized dialogs are pushed from a parent.

Callbacks and message hooks return `Task` and propagate cancellation. Await
`PushAsync`, `PopAsync`, `UpdateViewAsync` and application service work. Rendering
is explicit; `SetProperty` only changes a field. A dialog awaits a successful
submission callback before popping, so the parent's activation renders the new
data. A failed callback keeps the dialog open and propagates the failure.

Models describe data, and services own application/domain operations. Reusable
controls accept typed values, validation functions and async callbacks; they do
not import game entities, repositories, or other application types.

Form elements are allocated once per owning view model and register handlers at
construction. Their views only render existing elements. Rebuilding controls in
`Display` would accumulate callbacks and invalidate the keyboard. See
`UglyFwUI/README.md` for the public control API and `TelegramTestBot/ViewModels/ChatRoomViewModel.cs`
for a working composition.

## Cross-session updates

A shared service (such as the test bot's `ChatRoom`) must not render or await
another session's view from inside a handler, because each session's dispatch
holds its own lock, and two sessions waiting on each other would deadlock. Call
`ViewRefreshQueue.Request(viewModel)` instead. `UglyTgApp.Load` starts the queue,
which coalesces requests and re-renders each page under that page's session lock,
only while the page is the session's current state. Covered pages render on
reactivation. Tests drain it with `ProcessPendingAsync`. Pages subscribe in
`InitializeAsync` and unsubscribe in `UnloadAsync`.

## Verification and runtime

With .NET 6 on PATH, from the workspace root:

```bash
dotnet build UglyWorkspace.sln
dotnet test UglyWorkspace.sln
```

In this environment the SDK is also available at `/tmp/ugly-dotnet/dotnet`.
If the executable is currently running and its apphost cannot be overwritten,
build/test with `-p:UseAppHost=false`; do not terminate a user's bot just to build.
Tests use simulated Telegram transport and require no token.

`sh TelegramTestBot/run.sh` loads the Git-ignored `TelegramTestBot/.env`. Keep
credentials out of committed files. The HatoPad run configuration is
`telegram-test-bot`; restart a running instance to load new code.

The test bot journals UI message IDs under its ignored `.data/ui-messages/`
directory (or `TELEGRAM_BOT_DATA_DIR`). The framework's `Services/` folder holds
the `IUiMessageStore` contract and in-memory/JSON implementations. This is restart
cleanup metadata, not persisted application state. The first accepted update of
a fresh session deletes tracked previous bot views before rendering. User history
is not swept. The test bot deletes newly processed user text using its host-level
message-removal policy, including input handled by shared dialogs. Telegram
deletion limits still apply.
