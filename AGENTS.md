# Workspace conventions

Read `ARCHITECTURE.md` when changing framework, controls, application pages or
project layout. Keep its dependency directions and folder conventions intact.

- Reusable Telegram UI belongs in `UglyFwUI/`; application-specific pages and
  domain logic stay in their application. Reuse shared controls before adding a
  second implementation.
- Use `Views/`, `ViewModels/`, `Models/`, `Services/`, `Hosting/` and `Tests/` for
  their documented roles. Match namespaces to folders. Preserve the framework's
  existing infrastructure folders.
- Views format state; view models handle interaction/navigation; services handle
  domain work. Views do not mutate state or register callbacks.
- Await the framework's async API and propagate cancellation. Do not reintroduce
  blocking Telegram calls or `async void` callbacks.
- Maintain `UglyWorkspace.sln` and the relevant README when adding/moving projects
  or changing public APIs. Verify affected behavior using offline tests.
- `.agents/skills/ugly-fw/` and `.agents/skills/ugly-fw-ui/` document the framework
  and shared controls. Keep their guidance aligned with API/structure changes;
  use HatoPad configuration tools when editing those logical skill documents.
