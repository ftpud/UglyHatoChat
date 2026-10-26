---
name: ugly-fw-ui
description: Create and reuse Telegram UI controls in UglyFwUI, splitting rendering into Views, interaction into ViewModels, and composing shared forms without application business dependencies.
---

Read root `ARCHITECTURE.md` for boundaries and `UglyFwUI/README.md` for the
current API. The test bot's HatoChat is the runnable composition:
`TelegramTestBot/ViewModels/ChatRoomViewModel.cs` + `Views/ChatRoomView.cs` use
every control (lobby, chat, settings and people pages).

## Split controls consistently

- `UglyFwUI/Views/<Name>View.cs`: stateless `[Managed]` implementations of
  `IView`. Display reads the model and returns ViewResponse; it does not register
  callbacks, mutate fields, call repositories or send Telegram requests.
- `UglyFwUI/ViewModels/<Name>ViewModel.cs`: per-session dialog interaction,
  validation results and navigation, bound to the corresponding view.
- `Models/`: typed UI data such as `PickerOption<T>(Label, Value)` and
  `ScrollWindow<T>(Items, Older, Newer, AutoScroll)`.
- `Forms/`: reusable composition elements and their owner-bound callbacks.
- `Rendering/`: pure shared escaping and layout helpers (`HtmlText`, `FormLayout`,
  `FormRenderer`).
- `Tests/`: separate offline test project. Match namespaces to folder names.

Keep domain repositories, entity/card formatters, persistence and application
pages in the application. The dependency direction is application -> UglyFwUI ->
UglyTgApplication -> UglyAppFramework; do not introduce a reference back into an
application or from framework core into UI.

## Reuse and compose

`TextDialogViewModel(prompt, onSubmit, validate)` takes an async
`Func<string, CancellationToken, Task>` and an optional validator returning an
error string or null/empty. `PickerDialogViewModel<T>(prompt, options, onSelect)`
takes typed PickerOption values and an async option callback. Push dialogs as
children, not starting states. Submission is awaited before PopAsync so parent
activation renders the new value; errors keep the dialog open. Back cancels
without submission. Keep duplicate labels valid and snapshot picker options so
rendered choices continue to match their registered handlers.

Form controls register with an owning view model. Construct them once per owner,
normally in its constructor, and store them in the view model:

- `FormTextBox` (field that keeps its value; opens a validated text dialog;
  `Clear()` resets it),
- `FormInputButton(owner, label, prompt, onSubmit, validate)` (one-shot action
  such as "send a message": opens a text dialog, passes accepted text to the
  awaited onSubmit, stores nothing; put rejection rules such as unknown commands
  in the validator so errors show inside the dialog),
- `FormPickerButton<T>` (opens a picker; `Select(value)` preselects, `HasSelection`
  distinguishes no choice from a valid default),
- `FormButton<T>` (passes its current Data to an awaited callback),
- `FormToggle` (bool; awaits onChanged before flipping, then renders the owner),
- `FormScrollBox<T>(owner, source, pageSize)` (window over a live list; auto mode
  follows new items, scrolling older switches to manual and stays put, reaching
  the bottom/⏬/auto button resumes; its callbacks render the owner).

Prefer a single-step action (FormInputButton) over a field-plus-submit-button pair
when the value is consumed immediately.

Render with `FormLayout.Create(title).Html(..).Text(..).Scroll(box, formatHtml,
emptyText, maxLength).Fields(fields).Buttons(..).Back("/back").Build()`, or the
`FormRenderer.Render(title, fields, back)` shorthand. The owner handles any
Back/Buttons callback data it passes. Never recreate controls inside Display:
doing so grows the callback registry and invalidates existing keyboards. A
toggle/scroll box reads state at render time only (`GetWindow()` is pure).
App-specific service work and explicit UpdateViewAsync belong in callbacks.

Use per-instance callback IDs (the library's CallbackId helper); keep callback
payloads within Telegram's 64-byte limit. Do not restore the old global counter
or persist business data inside callback payloads. New dialog instances must
ignore keyboard IDs from prior instances.

Escape dynamic HTML with `HtmlText.Encode` (escapes only `& < > "`; emoji stay
literal so lengths stay predictable). Prompts and labels are plain text; keep
intentional markup in views, and scroll-box `formatHtml` must escape item text.
Apply input-size validation and compose messages within Telegram's 4096 limit;
`FormLayout.Scroll` folds visible items over `maxLength` into the "earlier"
count. Localization is not provided by these controls.

## Promote and verify

When promoting another reusable control, move both its rendering and interaction
into the library, migrate callers and remove duplicated app implementations.
Keep application policies supplied through typed options, validation functions
and async callbacks. Document the public API in the library README.

Test validation failures, awaited submission/errors, Back navigation, typed
selection, stale callbacks, scroll modes and escaped/bounded rendering with a fake
Telegram transport. Run `dotnet test UglyFwUI/Tests/UglyFwUI.Tests.csproj` and
affected app/framework tests. Update root solution and architecture guidance when
adding projects or changing the agreed folder conventions.
