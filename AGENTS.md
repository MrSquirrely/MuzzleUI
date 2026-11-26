# AGENTS.md: working on Muzzle

This file is for AI coding agents (Claude Code, Codex, Copilot and others) and for humans who want the short version. It covers how the repository is laid out, the rules that aren't obvious from the code, and the commands to check your work. The user-facing docs are [README.md](README.md) and [docs/REFERENCE.md](docs/REFERENCE.md).

## What this is

**Muzzle.Theme** is a WPF theme and control library: pastel, rounded, with "jelly" spring animations. It is published to NuGet as `Muzzle.Theme` and targets `net10.0-windows`. It runs on Windows only. The XAML namespace is `http://schemas.muzzle.dev/wpf` (prefix `mz`), and the C# namespaces are `Muzzle.Theme`, `Muzzle.Theme.Controls` and so on.

| Path | Role |
|---|---|
| `src/Muzzle.Theme/` | The packaged library |
| `src/Muzzle.Theme/Themes/Muzzle.xaml` | The master dictionary. It merges, in order: tokens, the **palette slot** (`Muzzle.Light.xaml`), the **accent slot** (`Accents/Muzzle.Accent.Coral.Light.xaml`), typography, icons, easings, then `Controls/*.xaml` |
| `src/Muzzle.Theme/Themes/Muzzle.Light.xaml`, `Muzzle.Dark.xaml` | The base palettes. **Generated**, with identical key sets |
| `src/Muzzle.Theme/Themes/Accents/` | 4 accents × Light/Dark overlays. **Generated** |
| `src/Muzzle.Theme/Themes/Icons/MuzzleIcons.xaml`, `Controls/MuzzleIconKind.cs` | **Generated** icon geometries and enum |
| `src/Muzzle.Theme/Themes/Controls/*.xaml` | One dictionary per control (styles and templates) |
| `src/Muzzle.Theme/Controls/` | Custom controls (`MuzzleWindow`, `Growl`, `Dialog`, `Drawer`, `CommandPalette`, `PropertyGrid`…) |
| `src/Muzzle.Theme/Behaviors/`, `Windowing/`, `Converters/` | Attached behaviors, Win32 helpers and value converters |
| `src/Muzzle.Theme/MuzzleThemeManager.cs` | Runtime theme and accent switching (it swaps the palette and accent slots in place) |
| `src/Muzzle.Gallery/` | The demo app (not packaged). It is also the visual-regression harness (`--snapshot`) |
| `tests/Muzzle.Theme.Tests/` | The xUnit suite. All WPF work runs on one STA thread via `MuzzleWpfTestFixture` |
| `tools/` | Generators. Edit their tables, never their output |
| `.github/workflows/` | `ci.yml` (PRs and dev) and `release.yml` (main → GitHub Release + NuGet) |

## Branches and releases (important)

- **All work happens on `dev`.** Commit and push to `dev`, or to a feature branch with a PR into `dev`.
- **`main` is production.** It is protected: changes arrive only through a pull request from `dev`, and that PR must pass CI. Open one only when the maintainer says to release.
- **Every push to `main` releases.** `release.yml` builds, tests and packs, then creates a GitHub Release tagged `v<Version>` with the `.nupkg` and `.snupkg` attached.
- **NuGet.org uses Trusted Publishing; no API key is stored anywhere.** When the `NUGET_USER` repository variable holds the nuget.org profile name, `NuGet/login@v1` exchanges the job's GitHub OIDC token for a one-hour key and pushes with `--skip-duplicate`. Without the variable, the step is skipped with a notice.
- **Bump the version for each release.** The `<Version>` in `src/Muzzle.Theme/Muzzle.Theme.csproj` must go up before merging to `main`. If the tag already exists, the job creates no new GitHub Release. The NuGet push still runs, but a version that is already on NuGet.org is skipped, so re-running a release is safe.

### Trusted Publishing setup (one-time, done by the maintainer)

1. **Create the policy on nuget.org.** Click your username, choose **Trusted Publishing**, and add a policy with Repository Owner `MrSquirrely`, Repository `MuzzleUI` and Workflow File `release.yml` (just the file name, without `.github/workflows/`). Leave Environment empty.
2. **Set the repository variable.** On GitHub, open Settings → Secrets and variables → Actions → **Variables**, and add `NUGET_USER` with your nuget.org profile name (not your email address).
3. **Activate the policy within 7 days.** A new policy may start out "temporarily active" for 7 days. The first successful publish makes it permanent, and you can restart the window from nuget.org if it lapses.

If the workflow file is renamed, the policy must be updated to match.
- **`dotnet pack` checks for API breaks.** `PackageValidationBaselineVersion` in the `.csproj` names the last published version, and pack fails if the public API breaks against it. After each release, set it to the version just published. For an intentional break, bump the major version and regenerate the suppression file with `/p:ApiCompatGenerateSuppressionFile=true`.
- **Never force-push** `main` or `dev`, and never skip hooks.

## Commands

```powershell
dotnet build Muzzle.slnx                                   # Debug, all projects
dotnet build src/Muzzle.Theme -c Release                   # must report 0 warnings
dotnet test tests/Muzzle.Theme.Tests                       # 262 tests, about 25 s
dotnet pack src/Muzzle.Theme -c Release -o artifacts       # .nupkg + .snupkg, must report 0 warnings
dotnet run --project src/Muzzle.Gallery                    # interactive demo
src/Muzzle.Gallery/bin/Debug/net10.0-windows/Muzzle.Gallery.exe --snapshot <dir>   # renders every page to PNG
```

**Snapshot mode:**

- It writes PNGs for every page in Light, Dark and each accent, and logs binding errors to the console.
- After a visual change, look at the affected PNGs.
- It briefly brings the gallery window to the foreground to send real keystrokes.

## Definition of done

1. `dotnet build src/Muzzle.Theme -c Release` reports **0 warnings**. The build generates XML docs, so every public or protected member needs a doc comment (CS1591 would fail this).
2. `dotnet test tests/Muzzle.Theme.Tests` passes, and new behaviour has a test.
3. For any template or visual change, the gallery runs and its snapshot shows no binding errors.
4. For any new control, every step in [Adding a control](#adding-a-control) is done.
5. Files are normalised (next section).

## Coding style

The maintainer's rules. There is no `.editorconfig` in the repo, so follow these exactly.

- **No `var`, ever.** Use explicit types, and target-typed `new()` when the type is visible: `List<string> names = new();`. Use collection expressions (`[]`) where they fit.
- **Brace placement:**
  - Opening braces go on the **same line** for everything: types, methods, `if`, lambdas and initialisers.
  - `else`, `catch` and `finally` start a **new line** after the closing brace.
  - Always use braces, even around a one-line body.
- **Blocks and declarations:**
  - Use file-scoped namespaces, with usings outside them.
  - Don't add file headers or `this.`.
  - Use keywords, not BCL names (`string`, not `String`).
- **Members:**
  - Always write accessibility modifiers.
  - Prefer primary constructors and auto-properties.
  - Expression bodies are for properties, accessors, lambdas and local functions, **not** for methods or constructors.
- **Patterns and operators:**
  - Write `is null` / `is not null`, use switch expressions, `?.` and `??`.
  - Use `static` lambdas where possible, `default` (not `default(T)`) and `System.Threading.Lock`.
- **Parentheses:** add them for clarity in mixed binary expressions, for example `(a + b) * c` and `(x > 0) && (y < 5)`.
- **Line endings: CRLF, with no final newline at the end of the file.** After editing, run this normalisation from the repo root:

  ```powershell
  $files = Get-ChildItem $PWD -Recurse -File -Include *.cs,*.xaml,*.csproj,*.md,*.ps1,*.json,*.yml | Where-Object { $_.FullName -notmatch '\\(bin|obj|artifacts)\\' }
  foreach ($file in $files) { $o = [IO.File]::ReadAllText($file.FullName); $c = $o.Replace("`r`n","`n").Replace("`n","`r`n").TrimEnd("`r","`n"); if ($c -ne $o) { [IO.File]::WriteAllText($file.FullName, $c, [Text.UTF8Encoding]::new($false)) } }
  ```

- **XML doc conventions:**
  - A DP identifier says `Identifies the <see cref="X"/> dependency property.` For a read-only one, write "read-only dependency property".
  - A wrapper says `Gets or sets … The default is …. This is a dependency property.`
  - Use `<inheritdoc />` on overrides and interface implementations.
  - Attached properties need docs on both `GetX` and `SetX`.

## Theming rules

- **Template colours, brushes and shadows must use `{DynamicResource Muzzle.*}`**, never hard-coded colours or `StaticResource`. If they don't, Light/Dark and accent switching stop working, and the tests check this indirectly.
- **Read colours in code with `SetResourceReference`**, not a one-off `TryFindResource`, for the same reason.
- **New palette keys go in `tools/Generate-Palettes.ps1`**, so they reach both palettes. Then run:

  ```powershell
  pwsh -NoProfile -File tools/Generate-Palettes.ps1
  ```

  The tests enforce that Light and Dark have the same keys, and that all eight accent files have the same keys.
- **Icons:** edit the table in `tools/Generate-Icons.ps1`, then run:

  ```powershell
  pwsh -NoProfile -File tools/Generate-Icons.ps1
  ```

  It regenerates `MuzzleIcons.xaml` and `MuzzleIconKind.cs`, with doc comments. The icons sit on a 24 × 24 grid as round-capped strokes.
- **Package icon:** generate it with:

  ```powershell
  pwsh -sta -NoProfile -File tools/Generate-PackageIcon.ps1
  ```

  It writes `assets/muzzle-icon.png` (128 × 128).
- **Motion:**
  - Use `MuzzleEasings.Get(element, MuzzleEasings.JellyKey)` and the related helpers.
  - Gate code-driven animation on `Jelly.IsMotionEnabled(element)`, which honours `JellyIntensity = 0` and the Windows animation setting.
  - `Jelly.EnsureScale` preserves existing transforms by wrapping them in a `TransformGroup`, so don't replace `RenderTransform` directly.

## Adding a control

1. **Code:** add `Controls/MyControl.cs`. In its static constructor, call `DefaultStyleKeyProperty.OverrideMetadata`, and declare its parts with `[TemplatePart(Name = …)]` (as `public const string …PartName`).
2. **Style:** add `Themes/Controls/MyControl.xaml`, with a keyed style `Muzzle.MyControl` plus an implicit style based on it, and merge it in `Themes/Muzzle.xaml`.
3. **Docs:** write XML docs for everything public.
4. **Tests:** add the control to the template smoke test, `ControlTemplateTests.cs` in `tests/Muzzle.Theme.Tests`. That test templates every control off-screen in Light and in Dark × every accent, checks the declared parts, and requires no binding errors. Add behaviour tests too.
5. **Gallery:** add a demo to the gallery page for its category, and include it in the snapshot run if it has interactive states.
6. **Docs pages:** add a row to the catalogue in `README.md` and a section to `docs/REFERENCE.md`.

## Testing notes

- WPF test bodies **must** run through `fixture.Run(() => …)` / `fixture.RunAsync(…)` on the shared STA app thread. Never create your own `Application`.
- Call `fixture.AssertClean()` to fail on binding errors and dispatcher exceptions.
- Off-screen test windows use `ShowActivated = false` and are closed in `finally`.
- Converter tests are plain `[Fact]`s and need no UI thread.

## Gotchas learned the hard way

- **Bindings inside template Freezables:** an `ElementName` binding inside a template `Freezable` (such as a VisualBrush in an OpacityMask) logs "cannot find governing element". Wire it in `OnApplyTemplate` instead (see `Skeleton`).
- **Menu item styles:** `MenuItem` styles need `OverridesDefaultStyle=True`, or the system theme's bindings leak in.
- **Overload names:** avoid overloads that differ only in parameters for methods used from XAML, because XAML reports "Ambiguous match".
- **Popups and tooltips:** with `StaysOpen=False`, a popup re-opened on mouse-up closes immediately. Toggle on mouse-down, as `Popover` does.
- **DataGrid:** single-click check boxes come from `DataGridAssist.SingleClickCheckBoxes`. Keep it on by default.
- **`--snapshot` side effects:** it never touches the clipboard or the user's saved window placement. Keep it that way.