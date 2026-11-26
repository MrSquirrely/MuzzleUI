# Muzzle reference

The detailed, per-control reference for **Muzzle.Theme**. For installation and the quick start, see the [README](../README.md).

## What's inside

```
src/Muzzle.Theme
├─ Themes/
│  ├─ Muzzle.xaml               master entry point (merge this)
│  ├─ Muzzle.Light.xaml         light palette: Muzzle.Color.*, Muzzle.Brush.*, Muzzle.Shadow.*
│  ├─ Muzzle.Dark.xaml          dark "Pastel Twilight" palette with exactly the same keys
│  ├─ Tokens/Colors.xaml        raw pastel palette ramp (Muzzle.Palette.*)
│  ├─ Accents/                  Muzzle.Accent.<Coral|Lavender|Mint|Peach>.<Light|Dark>.xaml accent overlays
│  ├─ Tokens/Brushes.xaml, Shadows.xaml   back-compat shims that merge Muzzle.Light.xaml
│  ├─ Tokens/Typography.xaml    fonts, sizes, Muzzle.Text.* styles
│  ├─ Animations/JellyEasings.xaml  Muzzle.Ease.* and Muzzle.Storyboard.*
│  ├─ Fonts/                    Atkinson Hyperlegible Next (embedded, SIL OFL 1.1)
│  └─ Controls/*.xaml           one dictionary per control
├─ Controls/  MuzzleWindow, PropertyGrid (+ PropertyItem/Category/selector), ToggleSwitch, Card, SoftBorder,
│             Growl/GrowlContainer/GrowlCard/GrowlWindow, Dialog/DialogContainer, Drawer/DrawerContainer,
│             NavigationView(+Item), BreadcrumbBar(+Item, BreadcrumbPanel), Pagination, TransitioningContentControl, StepBar(+Item),
│             NumericUpDown, TagInput(+TagItem), SearchBar, ColorPicker, ColorPickerDropDown, TimePicker, Rating(+RatingItem),
│             Badge(+BadgeAdorner), Skeleton, LoadingSpinner, InfoBar, Popover, ThemeToggleButton,
│             Avatar, Carousel, ImageViewer, AccentSwatch, MuzzleIcon (+MuzzleIconKind), CommandPalette(+Item),
│             SplitButton, DropDownButton, EmptyState, Keycap, ShortcutBadge, RangeSlider, PasswordStrengthMeter,
│             CodeBlock, CopyableText
├─ Converters/
├─ MuzzleAssist.cs              attached properties
├─ DataGridAssist.cs            Muzzle editors for stock DataGrid columns
├─ MuzzleThemeManager.cs        MuzzleTheme (Light | Dark | System) and runtime theme switching
├─ MuzzleThemeResourceDictionary.cs   one-line theme + accent setup for App.xaml
├─ Behaviors/                   JellyBehavior, ClipHelper, ScrollViewerAssist, FocusTrap, EscapeKeyBehavior
├─ Windowing/                   WindowStateSaver, BackdropHelper (Mica / Acrylic), SystemTrayIcon
tools/
├─ Generate-Palettes.ps1        base palettes + accent overlays from one token table
└─ Generate-Icons.ps1           Themes/Icons/MuzzleIcons.xaml + MuzzleIconKind.cs from one icon table
tests/
└─ Muzzle.Theme.Tests/         xUnit + Xunit.StaFact suite (net10.0-windows)
├─ JellyController.cs, Jelly.cs, MuzzleEasings.cs, MuzzleShadows.cs, TabIndicatorController.cs
```

### Styled controls

| Control | Keys | Notes |
|---|---|---|
| Button | `Muzzle.Button` (implicit), `.Primary`, `.Secondary`, `.Mint`, `.Ghost`, `.Pill`, `.Icon`, `.Spin` | Hover 1.03×, press 0.94×0.92, elastic release |
| ToggleButton / RepeatButton | `Muzzle.ToggleButton` (implicit) | Blush when checked |
| TextBox / PasswordBox | `Muzzle.TextBox`, `Muzzle.TextBox.Compact`, `Muzzle.PasswordBox` | Springy focus ring, scale pulse, placeholder |
| CheckBox / RadioButton | `Muzzle.CheckBox`, `Muzzle.RadioButton` | Elastic checkmark / dot, three-state dash |
| ToggleSwitch | `Muzzle.ToggleSwitch` | Thumb stretches and springs across |
| ComboBox | `Muzzle.ComboBox`, `Muzzle.ComboBoxItem` | Spring-open dropdown, editable support |
| Slider | `Muzzle.Slider` | Thumb swells on hover, grows on drag; vertical + ticks |
| ProgressBar | `Muzzle.ProgressBar`, `.Lavender`, `.Mint` | Value glides; indeterminate shimmer |
| TabControl | `Muzzle.TabControl`, `Muzzle.TabItem` | Sliding/stretching pill, content fades up; all 4 placements |
| Expander | `Muzzle.Expander`, `Muzzle.Expander.Flat` | Elastic open, fold shut; Down/Up |
| Card / Border | `mz:Card`, `Muzzle.Surface`, `Muzzle.Surface.Flat`, `Muzzle.Chip` | Soft elevation |
| ScrollViewer / ScrollBar | `Muzzle.ScrollViewer`, `Muzzle.ScrollBar` | Slim thumb widens on hover |
| ListBox, ToolTip | `Muzzle.ListBox`, `Muzzle.ToolTip` | Rounded rows; tooltip pops in |
| Menu, ContextMenu, MenuItem, Separator | `Muzzle.Menu`, `Muzzle.ContextMenu`, `Muzzle.MenuItem`, `Muzzle.MenuSeparator` (all implicit) | Rounded cards pop in; nested submenus, gesture text, checkable rows. `Muzzle.MenuItem` picks its template from the item's `Role`, so it also works for a loose `MenuItem` in a panel |
| Growl, Dialog, Drawer | `Muzzle.GrowlCard`, `Muzzle.Dialog`, `Muzzle.Drawer` | Toasts, modal overlays, edge panels (below) |
| Badge, Skeleton, LoadingSpinner, InfoBar, Popover, ToolTip | `Muzzle.Badge`, `Muzzle.Skeleton`, `Muzzle.LoadingSpinner`, `Muzzle.InfoBar`, `Muzzle.Popover`, `Muzzle.ToolTip` | Feedback & status (below) |
| NumericUpDown, TagInput, SearchBar, ColorPicker (+DropDown), DatePicker / Calendar, TimePicker, Rating | `Muzzle.NumericUpDown`, `Muzzle.TagInput`, `Muzzle.SearchBar`, `Muzzle.ColorPicker`, `Muzzle.DatePicker`, `Muzzle.Calendar`, `Muzzle.TimePicker`, `Muzzle.Rating` | Inputs & pickers (below) |
| NavigationView, BreadcrumbBar, Pagination, TransitioningContentControl, StepBar | `Muzzle.NavigationView`, `Muzzle.BreadcrumbBar`, `Muzzle.Pagination`, `Muzzle.TransitioningContentControl`, `Muzzle.StepBar` | Navigation & layout (below) |

All templates keep keyboard navigation and a lavender keyboard `FocusVisualStyle`, and disabled states fade to ~50% opacity instead of turning grey.

ListBox, ComboBox and TreeView leave `ItemContainerStyle` unset, so an implicit `ListBoxItem`, `ComboBoxItem` or `TreeViewItem` style in your app (based on the `Muzzle.*Item` key) applies to every list without extra markup.

### `MuzzleAssist` attached properties

| Property | Type | Purpose |
|---|---|---|
| `CornerRadius` | `CornerRadius` | Rounding of any Muzzle control (also sets `Border.CornerRadius`). Large values (999) make true pills. |
| `ShadowDepth` | `None/Subtle/Soft/Raised/Floating` | Elevation. On Controls it's drawn inside the template (text stays crisp); on plain elements it sets `Effect`. |
| `JellyIntensity` | `double` (inherits) | Multiplies every squish. `0` = calm, `1` = default, `3` = maximum wobble. |
| `Jelly` | `JellyMode` flags | Make *any* element jelly: `Hover, Press, Focus, Toggle, All`. |
| `Placeholder` | `string` | Hint text for TextBox, PasswordBox, ComboBox. |
| `HoverBackground`, `PressedBackground` | `Brush` | Button interaction colours. |
| `AnimateProgress` / `ProgressRatio` | `bool` / `double` | Smoothly eased progress (on by default for ProgressBar). |

```xml
<Button Content="Extra squishy" Style="{StaticResource Muzzle.Button.Primary}"
        mz:MuzzleAssist.CornerRadius="18"
        mz:MuzzleAssist.ShadowDepth="Floating"
        mz:MuzzleAssist.JellyIntensity="2.5" />

<Border Style="{StaticResource Muzzle.Surface}" mz:MuzzleAssist.Jelly="Hover, Press" />
```

From code: `Jelly.Wobble(element)`, `MuzzleEasings.Get(element, MuzzleEasings.JellyKey)`.

### MuzzleWindow

```xml
<mz:MuzzleWindow x:Class="MyApp.MainWindow" Title="My app" NonClientAreaHeight="48">
    <mz:MuzzleWindow.NonClientAreaContent>
        <TextBox Width="240" HorizontalAlignment="Center" mz:MuzzleAssist.Placeholder="Search…" />
    </mz:MuzzleWindow.NonClientAreaContent>
    ...
</mz:MuzzleWindow>
```

- `WindowChrome` with `GlassFrameThickness="0"`, `CornerRadius="16"`, caption height = `NonClientAreaHeight`; Windows 11 DWM rounded corners.
- Windows 11 **snap layouts**: the maximize button answers `WM_NCHITTEST` with `HTMAXBUTTON` and its jelly hover/press is driven from the non-client messages.
- Maximized frame compensation from `GetSystemMetricsForDpi` (per-monitor DPI aware).
- Properties: `NonClientAreaContent`, `NonClientAreaContentTemplate`, `NonClientAreaHeight`, `NonClientAreaBackground`, `NonClientAreaForeground`, `IsFullScreen`, `ShowTitle`, `ShowIcon`, `ShowMinimizeButton`, `ShowMaximizeButton`. `ResizeMode` hides buttons automatically.
- `NonClientAreaForeground` colours the title text and the caption glyphs, for a custom `NonClientAreaBackground`. Left unset, the theme colours apply.
- `IsFullScreen` covers the whole monitor (taskbar included), hides the title bar and border, and restores the previous bounds and state when turned off. It binds two-way; the window adds no key of its own, so bind Esc or F11 yourself.
- Elements in `NonClientAreaContent` are hit-test visible in chrome; empty space still drags the window.
- `SizeToContent` works with the custom chrome: the window re-measures once its handle exists, so it fits the content, title bar and border exactly.

### PropertyGrid

```xml
<mz:PropertyGrid SelectedObject="{Binding Settings}" ShowSearchBox="True" ShowCategories="True" />
```

Reflects public, writable, `[Browsable]` properties via `TypeDescriptor`, honouring `[Category]`, `[DisplayName]`, `[Description]`. Editors:

| Type | Editor |
|---|---|
| `string`, `object`, anything with a string `TypeConverter` (Thickness, CornerRadius, flags enums…) | TextBox (live parse, error tint) |
| `int`/`long`/`double`/`float`/`decimal`… | TextBox + spin buttons |
| numeric with `[Range(min, max)]` (DataAnnotations) | Slider + value box |
| `bool` / `bool?` | ToggleSwitch / three-state CheckBox |
| `enum` | ComboBox |
| `Color`, `Brush` | Pastel swatch popup + hex input |

Also: `SearchText`, `SelectedProperty`, `ShowDescription`, read-only `Categories`/`Properties`. Values update live when the object raises `PropertyChanged` (or for DependencyObjects, when a DP changes).

**Order.** `SortMode="Declared"` keeps the order the type declares its properties in, or `[Display(Order = n)]` where present; each category sits where its first property does. The default, `Alphabetical`, sorts by display name with "Misc" last.

**As a settings pane.** `ShowHeader="False"` drops the overline, type name and count; `ShowTypeNames="False"` hides CLR type names in the header and the description panel. `LabelWidth` (a `GridLength`, default `2*`), `MinLabelWidth` (90) and `MaxLabelWidth` size the label column. A property without `[Description]` has an empty description. Every built-in text is a `Muzzle.String.PropertyGrid.*` resource (see [Themes/Tokens/Strings.xaml](../src/Muzzle.Theme/Themes/Tokens/Strings.xaml)); declare the same key in your app's resources to translate it.

**Reset to default.** `ShowResetButtons="True"` ends each row in a button that restores the default (`[DefaultValue]` or a `ResetXxx` method) and is disabled while the value is the default. `PropertyItem.CanReset` and `ResetCommand` do the same from your own template.

**Steps and formats.** `[PropertyStep(0.25, Format = "0.00×")]` sets the step of a numeric editor and snaps values to it. With a `Format`, a range editor shows the formatted value as a label instead of an editable box.

**Custom editors.** Three ways in, from narrowest to widest:

```csharp
public sealed class FontEditor : PropertyEditorBase {
    public override FrameworkElement CreateElement(PropertyItem item) {
        return new ComboBox { ItemsSource = Fonts.SystemFontFamilies.Select(static f => f.Source) };
    }

    public override DependencyProperty? GetDependencyProperty() {
        return Selector.SelectedItemProperty;   // bound two-way to the property value
    }
}

[Editor(typeof(FontEditor), typeof(PropertyEditorBase))]
public string Font { get; set; }
```

- **`[Editor]`** with a `PropertyEditorBase`, as above. The element's `DataContext` is the `PropertyItem`; `item.Descriptor` and `item.Component` give access to custom attributes and the edited object.
- **`EditorTemplateSelector`** on the grid: a `DataTemplateSelector` asked for every property first. Return `null` to keep the built-in editor.
- **`RowTemplate`** / **`CategoryTemplate`** replace a whole row or category card. Host the standard editor in your own row with `<ContentControl Content="{Binding}" ContentTemplateSelector="{StaticResource Muzzle.PropertyGrid.EditorSelector}" />`.

## Overlays & notifications

Status tokens back all three: `Muzzle.Brush.Info|Success|Warning|Error` (`#98C9F2`, `#9FD8CB`, `#F7CF8B`, `#F2949C`), each with a `…Soft` tint and a readable `…Text` colour, plus `Muzzle.Brush.Backdrop` (`#402E2B36`).

### Growl (toasts)

```xml
<Grid mz:Growl.GrowlParent="True"> ... </Grid>        <!-- adds a GrowlContainer top-right -->
<Grid mz:Growl.GrowlParent="True" mz:Growl.Token="Sidebar" />
```

```csharp
Growl.Info("Saved", title: "Done", token: null, waitTime: TimeSpan.FromSeconds(3));
Growl.Success(...); Growl.Warning(...); Growl.Error(...); Growl.Fatal(...);   // Fatal stays open
Growl.Ask("Delete it?", confirmed => { ... });
Growl.Show(new GrowlInfo { Message = "…", IsGlobal = true, StaysOpen = true });
Growl.Clear(token); Growl.ClearGlobal(); Growl.ClearAll();
```

- Toasts slide in from the right while springing from 0.85×, show a countdown line that pauses on hover, and squish to 0.9× + fade on close while their slot collapses so neighbours glide up.
- Without a registered container (or with `IsGlobal`), toasts go to `GrowlWindow`: a transparent, click-through, topmost window in the desktop's top-right corner that hides when empty and closes with the main window.
- Safe to call from background threads. Error/Fatal toasts are announced assertively to screen readers.

### Dialog

```csharp
Dialog dialog = Dialog.Show(new MyForm());               // or Dialog.Show<MyForm>()
MyResult? result = await Dialog.ShowAsync<MyResult>(new MyForm());
Dialog.Close();                                           // top dialog of the active window (or Dialog.Close(token))
```

- Inside the content: `Dialog.GetDialog(this)?.CloseWithResult(value)`, or a button with `Command="{x:Static mz:Dialog.CloseCommand}"` (`CommandParameter` = result). Content or its DataContext can implement `IDialogResultable<T>`; `ShowAsync<T>` wires its `CloseAction`.
- Hosted over the active window's client area via its AdornerLayer (the title bar stays usable), or inside `<mz:DialogContainer mz:Dialog.Token="…">`. While a modal dialog is open the content beneath leaves hit testing and the tab order; Tab cycles inside the dialog and focus returns afterwards.
- `CloseOnBackdropClick` (default false: the dialog wobbles instead), `CloseOnEscape` (true), `IsModal` (true).
- Backdrop fades in while the surface pops from 0.82× through a 1.04× overshoot to 1.0×; closing shrinks to 0.9× and fades.

### MessageBox

```csharp
MessageBoxResult result = MessageBox.Show(this, "Save your changes?", "Unsaved changes",
    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
```

- A themed, **synchronous** stand-in for `System.Windows.MessageBox` (`Muzzle.Theme.Controls.MessageBox`). It is a real `MuzzleWindow`, so it sits above hosted HWND content (WebView2, WindowsFormsHost) and works before the app has a main window, for example from a startup exception handler.
- `Show(text, …)` uses the active window as owner, or the screen centre when there is none; `Show(owner, text, …)` centres on the owner. Both take `caption`, `MessageBoxButton`, `MessageBoxImage` and a `defaultResult`.
- Enter presses the default button and Esc the cancelling one. Closing an OK-only message gives `OK`; with a Cancel button it gives `Cancel`; a Yes / No question stays open until answered.
- Button captions and the icon names read by screen readers are the `Muzzle.String.MessageBox.*` resources (`OK`, `Cancel`, `Yes`, `No`, `Information`, `Warning`, `Error`, `Question`); declare a key in your app's resources to translate it.
- `MessageBoxWindow` is the window itself, for showing a message without blocking: `Show()` it and read `Result` when it closes.

### Drawer

```xml
<mz:DrawerContainer mz:MuzzleAssist.CornerRadius="14">
    <ToggleButton IsChecked="{Binding IsOpen, ElementName=Settings}" Content="Settings" />
    <mz:Drawer x:Name="Settings" Dock="Right" DrawerSize="300" Header="Settings"
               ShowMask="True" MaskCanClose="True" CloseButtonVisibility="Visible">...</mz:Drawer>
</mz:DrawerContainer>
```

```csharp
Drawer drawer = Drawer.Show(new SettingsPanel(), Dock.Left, header: "Settings");   // over the active window
drawer.IsOpen = false;                                                            // slides out, removes itself
```

- `IsOpen` binds two-way; `Dock` is Left/Right/Top/Bottom; inner corners on the exposed edge are rounded (`16,0,0,16` for Right, etc.).
- Opens with a springy `BackEase` that overshoots the resting edge and settles, synchronised with the mask fade. The panel carries a hidden 24px gutter past its edge so the overshoot never reveals a gap. Esc, the mask or the close button closes it.
## Feedback & status

```xml
<mz:Badge Value="{Binding Unread}" MaxValue="99" Kind="Error"><Button Content="Inbox" /></mz:Badge>
<Button mz:MuzzleAssist.Badge="7" mz:MuzzleAssist.BadgeKind="Info" />
<mz:Skeleton IsLoading="{Binding IsBusy}" SkeletonShape="TextLines" LineCount="3"><TextBlock … /></mz:Skeleton>
<mz:LoadingSpinner IsActive="{Binding IsBusy}" SpinnerStyle="BouncingDots" />
<mz:InfoBar Severity="Warning" Title="Low on treats" Message="Only 4 left." IsOpen="{Binding ShowWarning}" />
<mz:Popover Placement="Bottom" TriggerMode="Click"><Button Content="Profile" />
    <mz:Popover.PopoverContent>…</mz:Popover.PopoverContent>
</mz:Popover>
```

- **Badge**: decorator with `Value` / `MaxValue` ("99+"), `Text`, `IsDot`, `ShowZero`, `Kind` (`Primary`, `Secondary`, `Info`, `Success`, `Warning`, `Error`), `BadgeMargin` and `BadgeForeground` (the pill's text colour; the wrapped content keeps its own). It pops 0.5× → 1.15× → 1× with an ElasticEase when it appears or changes. `MuzzleAssist.Badge` (int, numeric string, text, or `true` for a dot) + `MuzzleAssist.BadgeKind` put one on any element via an adorner.
- **Skeleton**: `IsLoading`, `SkeletonShape` (`Rectangle`, `Circle`, `TextLines`), `CornerRadius`, `LineCount`, `LineHeight`. Soft track-coloured blocks with a diagonal shimmer masked to their shapes; the content is laid out invisibly meanwhile (so the placeholder matches its size) and cross-fades in when loading ends.
- **LoadingSpinner**: `IsActive`, `SpinnerStyle` (`ArcRing`, `BouncingDots`), `AccentBrush`, `Thickness`. The arc whips round with an eased, springy rhythm; the dots hop in turn with BackEase squash-and-stretch. Inactive spinners collapse and stop animating.
- **InfoBar**: `Severity`, `Title`, `Message`, `IsOpen` (two-way), `IsClosable`, `ActionContent`, `IsIconVisible`, plus optional `Content`. Pastel tint + icon per severity; dismissing squishes it and folds its height away; `Closed` fires afterwards.
- **ToolTip**: rounded (10), soft shadow, BackEase pop from 0.88×.
- **Popover**: `PopoverContent`, `Placement` (`Top`, `Bottom`, `Left`, `Right`), `TriggerMode` (`Click`, `Hover`, `Manual`), `ShowArrow`, `StaysOpen`, `HoverDelay`, `IsOpen`. Centred on its anchor with an arrow tail, springing out from the anchor side; hover mode stays open while the pointer is over the card.
## Inputs & pickers

```xml
<mz:NumericUpDown Minimum="0" Maximum="15" Increment="0.25" DecimalPlaces="2" ValueFormat="0.00 'kg'" Value="{Binding Weight}" />
<mz:TagInput Tags="{Binding Traits}" Delimiter=",;" AllowDuplicates="False" MaxTags="10" Placeholder="Describe your cat…" />
<mz:SearchBar SuggestionsSource="{Binding Names}" SearchCommand="{Binding Search}" DebounceDelay="0:0:0.3" />
<mz:ColorPicker SelectedColor="{Binding Collar}" ShowAlpha="True" />
<DatePicker SelectedDate="{Binding VetDate}" />
<mz:TimePicker SelectedTime="{Binding VetTime}" MinuteIncrement="15" Is24HourClock="False" />
<mz:Rating Value="{Binding Score}" Maximum="5" AllowHalf="True" IconShape="PawPrint" />
```

- **NumericUpDown**: `Value`, `Minimum`, `Maximum`, `Increment`, `DecimalPlaces`, `ValueFormat` (any .NET format; the raw number shows while editing), `IsReadOnly`, `ShowSpinButtons`. ↑/↓ and PageUp/PageDown step; the wheel steps only while focused. The − / + pills squish in and spring back with a BackEase and disable at the limits. Invalid typing wobbles and reverts.
- **TagInput**: `Tags` (two-way `IList<string>`; an `ObservableCollection` is kept in sync both ways), `Delimiter` (each character completes a tag; Enter always does), `AllowDuplicates`, `MaxTags`, `Placeholder`. Chips pop in from 0.6× with an elastic overshoot and squish out; Backspace on an empty entry arms the last chip, a second press removes it. Duplicates make the existing chip wobble.
- **SearchBar** (a `TextBox`): magnifier, a clear button that pops in when there is text, `SearchRequested` (with `Query` and `IsSubmitted`) and `SearchCommand` after `DebounceDelay` or immediately on Enter, plus a suggestion dropdown filtered from `SuggestionsSource` (↑/↓, Enter, click; Esc closes, then clears).
- **ColorPickerDropDown**: a compact field (swatch + hex + chevron) that opens a full ColorPicker in a springy dropdown; SelectedColor / SelectedBrush are two-way and update live, plus ShowAlpha, Presets, IsDropDownOpen (Alt+↓ opens, Esc closes).
- **ColorPicker**: HSV canvas (keyboard: arrows), hue and alpha sliders, `Presets` swatches, and Hex / R / G / B inputs, all in sync with `SelectedColor` and `SelectedBrush`. Hex accepts `#RGB`, `#ARGB`, `#RRGGBB`, `#AARRGGBB` with or without `#`.
- **DatePicker / Calendar**: the standard controls, retemplated. The dropdown card springs open, the selected day gets a blush circle that bounces in, today wears a lavender ring, and the year/decade views use pill cells.
- **TimePicker**: `SelectedTime` (`TimeSpan?`), `Is24HourClock`, `MinuteIncrement`, `IsDropDownOpen`, `Placeholder`. The dropdown has scrollable hour / minute / AM-PM columns that open centred on the current time; the chosen slot gets a bouncing pill. Alt+↓ opens, Enter / Esc closes.
- **Rating**: `Value`, `Maximum`, `AllowHalf`, `IconShape` (`Star`, `Heart`, `PawPrint`), `IsReadOnly`, `ItemSize`, `ItemSpacing`, `FillBrush`, `EmptyBrush`. Icons grow 1.18× and wiggle on hover, and picked values ripple a jelly bounce. Clicking the current value clears it; arrows, Home and End work.
## Navigation & layout

```xml
<mz:NavigationView IsPaneOpen="{Binding IsMenuOpen}" OpenPaneLength="232" CompactPaneLength="64"
                   ItemsSource="{Binding Sections}" DisplayMemberPath="Title" SelectedItem="{Binding Current}">
    <mz:NavigationView.PaneHeader>…</mz:NavigationView.PaneHeader>
    <mz:NavigationView.PaneFooter>…</mz:NavigationView.PaneFooter>
</mz:NavigationView>
<mz:NavigationViewItem Content="Lounge" IconGeometry="M 2,8 L 8,2.5 L 14,8 M 4,6.5 V 14 H 12 V 6.5" />

<mz:TransitioningContentControl Content="{Binding CurrentPage}" Transition="SlideUp" />
<mz:BreadcrumbBar ItemsSource="{Binding Path}" ItemCommand="{Binding NavigateCommand}" ItemClicked="OnCrumbClicked" />
<mz:Pagination PageCount="24" CurrentPage="{Binding Page}" MaxDisplayedPages="7" ShowJumpBox="True" />
<mz:StepBar StepIndex="{Binding Step}" Orientation="Vertical">
    <mz:StepBarItem Content="Pick a kitten" /> …
</mz:StepBar>
```

- **NavigationView** is a single-selection `ListBox` (so `ItemsSource`, `SelectedItem`, keyboard and UI Automation all work). A blush pill slides behind the selected item (Y offset + height, `BackEase` overshoot); the pane springs between `CompactPaneLength` and `OpenPaneLength`, labels fade out and become tooltips when compact. Items take `IconGeometry` (line art drawn in the item's foreground) or any `Icon` content.
- **BreadcrumbBar** segments grow 1.04× on hover and their chevrons pop in and nudge forward. `ItemClicked` (item + index) and `ItemCommand` report clicks. When space runs out, middle segments fold behind "…" and the last segment trims with an ellipsis.
- **Pagination** keeps first/last pages visible and windows the middle with ellipses; a pill bounces to the current page. Commands: `FirstPageCommand`, `PreviousPageCommand`, `NextPageCommand`, `LastPageCommand`, `GoToPageCommand`, `JumpCommand`; `PageChanged` event. Out-of-range jump input wobbles.
- **TransitioningContentControl** animates every content swap: `None`, `CrossFade`, `SlideLeft`, `SlideRight`, `SlideUp`, `SlideDown`, `ScalePop` (with `SlideDistance`, `TransitionDuration`, `TransitionCompleted`).
- **StepBar**: `StepIndex` is 0-based (equal to the item count when all steps are done). Completed circles fill and a checkmark springs in, the current step gets a halo, and the blush track glides between circles, horizontally or vertically.

## Data & media

```xml
<DataGrid ItemsSource="{Binding Cats}" AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" />
        <DataGridComboBoxColumn Header="Breed" SelectedItemBinding="{Binding Breed}" ItemsSource="{Binding Breeds}" />
        <DataGridCheckBoxColumn Header="Indoor" Binding="{Binding IsIndoor}" />
    </DataGrid.Columns>
</DataGrid>
<mz:Avatar Source="{Binding Photo}" DisplayName="Ada Lovelace" Size="40" Shape="Squircle" Status="Online" />
<mz:Carousel ItemsSource="{Binding Slides}" ItemTemplate="{StaticResource SlideTemplate}" AutoPlay="True" Interval="0:0:4" />
<mz:ImageViewer Source="/Assets/garden.jpg" MaxZoom="8" />
<TreeView ItemsSource="{Binding Rooms}">…</TreeView>
```

- **DataGrid**: every part of a stock `DataGrid` is restyled, with no extra markup needed.
  - **Rows**: rounded cards (radius 8) with 2 px of air between them. A lavender hover layer and a blush selection layer fade in.
  - **Cells**: a soft focus ring when focused.
  - **Headers**: a sort chevron that springs in with an elastic ease and flips 180° with a back-ease when the direction changes, plus a hover-revealed resize gripper.
  - **Optional row header**: shows a blush selected-row marker.
  - **Editors**: `DataGridAssist.AutoStyleColumns` (on by default) gives text, check box and combo box columns Muzzle editors (`Muzzle.DataGrid.TextBox.Editing`, `.CheckBox`, `.ComboBox.Editing`). It leaves alone any column that sets its own `ElementStyle` / `EditingElementStyle`; set the property to `False` on a grid to opt out.
  - **Template columns**: can use those styles directly.
  - **Check boxes**: `DataGridAssist.SingleClickCheckBoxes` (also on by default) toggles a check box on the first click. A stock grid needs one click to select the cell and another to toggle.
- **Avatar**: `Source`, `DisplayName`, `Size`, `Shape` (`Circle`, `Squircle`), `Status` (`None`, `Online`, `Away`, `Busy`, `Offline`), plus `BorderBrush`/`BorderThickness` for a ring.
  - **No picture**: shows two-letter initials ("Ada Lovelace" → AL, "mochi" → MO) on one of six theme pastels. The colour is chosen by a stable hash of the name, so a person keeps their colour across runs.
  - **Failed downloads**: fall back to initials too. With no name either, a person glyph is shown.
  - **Status dot**: sits on the outline and pops in with a jelly bounce whenever the status changes.
  - **Helpers**: `Avatar.GetInitials` / `GetColorIndex` are public.
- **Carousel** (`ItemsControl`): `ItemsSource` + `ItemTemplate` or inline children, plus `SelectedIndex` (two-way), `SelectedItem`, `AutoPlay`, `Interval`, `IsLooping`, `ShowIndicators`, `ShowNavigationButtons`, `TransitionDuration` and a `SelectedIndexChanged` event.
  - **Motion**: slides arrive with a BackEase overshoot while the old one glides out.
  - **Controls**: pill arrows; dot indicators where the current dot stretches into a bouncing pill (click a dot to jump); ← / → / Home / End when focused.
  - **Auto-play**: pauses while the pointer or keyboard focus is inside.
- **ImageViewer**: `Source`, `Zoom` (two-way), `MinZoom`, `MaxZoom`, `ZoomStep`, `Stretch`, `ShowToolbar`; `ZoomTo(zoom, anchor)`, `ZoomIn()`, `ZoomOut()`, `ResetView()`.
  - **Mouse**: the wheel zooms smoothly around the pointer; dragging pans, and the picture always covers the frame (or stays centred when smaller).
  - **Double-click**: springs back to 100 %, or zooms 2× into the clicked spot when already at 100 %.
  - **Toolbar & keys**: a floating pill toolbar shows − / % / + / reset; + / − / 0 and the arrows work when focused.
- **TreeView**: rounded pastel selection pills (lavender when the tree isn't focused) and soft hover tints. Chevrons rotate 0° → 90° with a back-ease, and children spring open and fold shut. Works with `HierarchicalDataTemplate` and inline `TreeViewItem`s.

## Utilities & behaviors

```xml
<Border mz:JellyBehavior.EnableHoverBounce="True" mz:JellyBehavior.EnablePressSquish="True" mz:JellyBehavior.EntranceAnimation="PopIn" />
<Border CornerRadius="20" BorderThickness="2" mz:ClipHelper.CornerRadius="20"><Image … /></Border>
<ScrollViewer mz:ScrollViewerAssist.EnableSmoothScroll="True" mz:ScrollViewerAssist.EnableShiftHorizontalScroll="True" />
<Border mz:FocusTrap.IsEnabled="True" mz:FocusTrap.FocusFirstOnShow="True" mz:EscapeKeyBehavior.IsOpen="{Binding IsPanelOpen}" />
<mz:MuzzleIcon Kind="Search" IconSize="18" />
```

- **`JellyBehavior`** adds jelly physics to any element, with no retemplating.
  - **Properties**: `EnableHoverBounce` / `HoverScale` (1.03), `EnablePressSquish` / `PressSquishScale` (0.94), and `EntranceAnimation` (`None`, `PopIn`, `SlideUp`, `FadeIn`) on `Loaded`, with an optional `EntranceDelay` for staggering.
  - **Replaying**: `JellyBehavior.PlayEntrance(element)` plays the entrance again.
  - **Motion settings**: honours `JellyIntensity` and the Windows animation setting.
  - **Transforms**: it shares the element's transform with `MuzzleAssist.Jelly` (`Jelly.EnsureScale` now also finds a `ScaleTransform` inside a `TransformGroup`).
- **`ClipHelper.CornerRadius`** keeps a rounded clip in step with the element's size; per-corner radii work too, and 0 turns it off.
  - **On a `Border`**: it clips the child instead, inset by the border thickness and padding with matching inner radii, so images and scroll viewers never bleed past rounded corners.
  - **Helper**: `ClipHelper.CreateRoundedRectangle` is public.
- **`ScrollViewerAssist`**: set it on a ScrollViewer or anything containing one.
  - `EnableSmoothScroll` glides to the wheel target each frame (`WheelStep`, default 96 DIP).
  - `EnableShiftHorizontalScroll` makes Shift + wheel scroll sideways.
  - At either end, or over an inner scroller that can still move, the wheel passes through.
  - Item-based scrolling (`CanContentScroll`) steps by items.
- **`FocusTrap.IsEnabled`**: Tab, Shift+Tab and Ctrl+Tab cycle inside the container, with a guard against escaping through nested scopes. `FocusFirstOnShow` focuses the first child whenever it becomes visible. Dialog, Drawer, drop-down menus and the command palette use it.
- **`EscapeKeyBehavior`**: Esc (pressed with focus inside) runs `Command` (with `CommandParameter`) and/or sets a two-way `IsOpen` back to false.
- **Converters** (in the `mz` namespace, each with a static `Instance`):
  - `BoolToVisibilityConverter` and `NullToVisibilityConverter`: `Invert` / `UseHidden` properties, or a ConverterParameter of "Invert"; both also have a static `Inverted`.
  - `StringToInitialsConverter`: first + last word ("Sorrel Hartwick" → SH, "Lee Loo Dallas" → LD); a single word gives one letter ("Muzzle" → M), or two with `SingleWordLetters="2"` to match `Avatar`. Blank, null or `UnsetValue` give "".
  - `NullToVisibilityConverter` treats null, "" and `DependencyProperty.UnsetValue` as absent.
  - `ColorToSoftTintConverter`: a `Color`, `SolidColorBrush` or hex string to a translucent tint brush, with the strength in the parameter (default 0.18).
- **`MuzzleThemeResourceDictionary`**: `Theme` and `Accent` in one element.
  - It merges Muzzle.xaml plus the chosen palette and accent, so the first frame is right.
  - With `System` it hands over to `MuzzleThemeManager`, which keeps following Windows.
  - Your own resources can sit inside it; the gallery's App.xaml uses it.
- **`MuzzleIcon`**: 156 rounded, stroke-drawn icons on a 24 × 24 grid.
  - **Groups**: navigation, actions, media, files, status, interface, people and cozy extras such as Paw, Cat, Fish and Coffee.
  - **Properties**: `Kind`, `IconSize`, `StrokeThickness`; it draws in the inherited `Foreground`.
  - **Geometries**: they live in `Themes/Icons/MuzzleIcons.xaml` as `Muzzle.Icon.<Kind>`, so you can use them in any `Path` or override one by redefining its key. `MuzzleIcon.GetGeometry(kind)` returns one in code.
  - The gallery sidebar uses them.

## Desktop controls

```xml
<mz:CommandPalette ItemsSource="{Binding Commands}" ShortcutGesture="Ctrl+K" ItemInvoked="OnInvoked" />
<mz:SplitButton Content="Save" Click="OnSave" SegmentStyle="{StaticResource Muzzle.Button.Primary}">
    <mz:SplitButton.MenuItems><MenuItem Header="Save as…" InputGestureText="Ctrl+Shift+S" /></mz:SplitButton.MenuItems>
</mz:SplitButton>
<GridSplitter Grid.Column="1" Width="10" HorizontalAlignment="Stretch" />
<mz:EmptyState IconKind="Inbox" Title="Nothing here yet" Description="…" />
<mz:ShortcutBadge Shortcut="Ctrl+Shift+P" />
<mz:RangeSlider Minimum="0" Maximum="100" LowerValue="{Binding Min}" UpperValue="{Binding Max}" StepFrequency="5" />
<mz:PasswordStrengthMeter PasswordBox="{Binding ElementName=Secret}" />
<mz:CodeBlock Code="{Binding Snippet}" Language="xaml" ShowLineNumbers="True" />
```

- **CommandPalette**: lay it over a window's content.
  - **Opening**: its `ShortcutGesture` (default Ctrl+K) toggles it from anywhere in that window.
  - **Look**: the search card pops in with a BackEase over a dimmed backdrop.
  - **Search**: `SearchText` fuzzy-matches `ItemsSource` (subsequence match favouring prefixes, word starts and runs; `FuzzyScore` is public). Results are ranked in `Results`.
  - **Items**: `CommandPaletteItem` has a title, description, category badge, `MuzzleIconKind` icon, keycap `Shortcut`, hidden `Keywords`, `Command` and `Tag`.
  - **Keys and clicks**: ↑ / ↓ / PageUp / PageDown move, Enter or a click runs (closing first, then executing the command and raising `ItemInvoked`), Esc or a backdrop click closes. Focus is trapped while open and returned afterwards.
  - **No results**: shows an `EmptyState`.
- **SplitButton / DropDownButton**:
  - `SplitButton` has a primary segment (`Click`, `Command`, `CommandParameter`) plus a chevron pill; `DropDownButton` opens from its whole face.
  - **Menu contents**: `DropDownContent`, or `MenuItems` (`MenuItem`s and `Separator`s get the Muzzle menu style: icon, header, gesture text, a checkmark for checked items).
  - **Behaviour**: the menu springs open from the button, and a menu-item click, Esc or an outside click closes it; Alt+↓ opens it.
  - **Styling**: each segment squishes on its own; `SegmentStyle` picks the button style.
- **GridSplitter**: every `GridSplitter` gets a centred pill grab handle. It lies flat for row splitters, grows on hover, and turns accent while dragging or focused.
- **EmptyState**:
  - `IconKind` (or custom `Icon`), `Title`, `Description`, `ActionContent`.
  - The icon disc pops in and bounces on hover.
- **Keycap / ShortcutBadge**: mechanical keycaps with a bottom lip that dips when clicked or when `Press()` is called.
  - `ShortcutBadge` turns "Ctrl+Shift+P" or a chord ("Ctrl+K, Ctrl+S") into a row of keycaps, with friendly names (↑, Esc, Win…).
- **RangeSlider**: two thumbs that grow on hover and squish while dragging.
  - **Properties**: `Minimum`, `Maximum`, `LowerValue` / `UpperValue` (two-way, coerced so they never cross), `StepFrequency` snapping, `MinimumRange`.
  - **Input**: clicking the track moves the nearer thumb; arrow, Page and Home/End keys work on a focused thumb.
  - **Event**: `RangeChanged`.
- **PasswordStrengthMeter**: four segments with a label ("Too weak" → "Strong").
  - **Input**: set `Password`, point `PasswordBox` at one, or set `StrengthScore` (0–4) directly.
  - **Motion**: segments spring to their fill and glide from coral-red through peach and butter to mint-sage.
  - **`Estimate()`**: an offline heuristic (length and variety entropy, minus common passwords, runs, repeats and word-plus-digits).
- **CodeBlock / CopyableText**:
  - **CodeBlock**: a monospace rounded surface with optional line numbers, light XAML / XML and C# colouring that follows the theme, a language badge and a floating Copy button.
  - **CopyableText**: inline text with a small copy button.
  - **Copying**: the button morphs into a bouncing mint "Copied!" for two seconds (`IsCopied`, `Copied` event, `Copy()`). `ClipboardHelper.TrySetText` retries while another app holds the clipboard.

## Windows & system helpers

- **`WindowStateSaver.Key="MainWindow"`**: saves Left / Top / Width / Height (restored bounds) and the maximised state to `%LOCALAPPDATA%\<app>\window-state.json` on close.
  - **Restoring**: it restores them on `SourceInitialized`, so XAML sizes and `CenterScreen` don't win.
  - **Monitor check**: the title strip must land on a connected monitor's work area, otherwise the size is kept and the window is centred.
  - **Static API**: `StorageFolder`, `IsEnabled`, `Save` / `Restore` / `Forget`.
- **`BackdropHelper.BackdropType`** (`None`, `Mica`, `Acrylic`, `Tabbed`): Windows 11 22H2+ system backdrops via `DwmSetWindowAttribute`. `BackdropHelper.TintBackground="False"` keeps the window's own `Background` instead of switching to `Muzzle.Brush.BackdropCanvas`. The system menu (Alt+Space, right-click on the title bar) stays available while a backdrop is on.
  - **While active**: it extends the frame, makes the composition background transparent and swaps the window background for the translucent, theme-aware `Muzzle.Brush.BackdropCanvas`.
  - **Caption buttons**: it hides the native caption buttons that DWM would otherwise draw behind Muzzle's (it removes `WS_SYSMENU` while active, so Alt+Space has no system menu then).
  - **Older Windows**: it does nothing (`IsSupported`, `IsBackdropActive`).
- **`SystemTrayIcon`**: a notification-area icon declared in XAML (it takes no layout space).
  - **Properties**: `IconSource` (any `ImageSource`, rendered to an HICON), `ToolTipText`, `MenuContent`, `LeftClickCommand`, and `LeftClick` / `DoubleClick` / `RightClick` events.
  - **Menu**: right-click opens `MenuContent` in a Muzzle card that pops in at the cursor, instead of a Win32 menu. Menu items get the Muzzle style and close the card; the content is a logical child, so `DataContext` flows in.
  - **Notifications**: `ShowNotification(title, message)` raises a Windows toast.
  - **Lifecycle**: it re-adds itself if Explorer restarts, and is removed on unload or app exit, or when `IsIconVisible` turns false.

## Dark mode & theming

Muzzle ships two palettes with identical keys: **Muzzle.Light.xaml** (warm cream) and **Muzzle.Dark.xaml**, "Pastel Twilight". The dark palette has a deep plum-charcoal canvas (`#181622`), slate-indigo surfaces (`#221F2E`, raised `#2A2638`) and luminous pastel accents (blush `#F6A2B3`, lavender `#C4B5FD`, mint `#A7E3D5`), with off-white text (`#F2EFF7`). `Muzzle.xaml` merges the light palette by default; every control template reads colours, brushes and shadows through `DynamicResource`, so switching palettes restyles everything that is open.

```csharp
MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);                 // instant
MuzzleThemeManager.ApplyTheme(MuzzleTheme.System, animate: true); // follow Windows, with a crossfade
MuzzleThemeManager.ThemeChanged += (_, e) => Debug.WriteLine($"{e.Theme} -> {e.EffectiveTheme}");
```

```xml
<!-- A sun / moon / monitor button that cycles Light → Dark → System. -->
<mz:ThemeToggleButton />
```

- **`MuzzleTheme`**: `Light`, `Dark`, `System`. `System` reads `HKCU\…\Themes\Personalize\AppsUseLightTheme` and keeps following it through `SystemEvents.UserPreferenceChanged`, crossfading when Windows flips.
- **`MuzzleThemeManager`**:
  - `ApplyTheme(theme, animate)` finds the palette slot wherever it sits in `Application.Current.Resources` and swaps it in place.
  - `CurrentTheme` is the theme you asked for; `EffectiveTheme` / `IsDark` say which palette is actually applied.
  - Also: `IsSystemDark()`, `DetectAppliedTheme()`, `TransitionDuration`, and the `ThemeChanged` event.
  - With `animate`, each open window shows a snapshot of its old look that fades away over the new one (skipped when Windows animations are off).
  - Calls from other threads are marshalled to the UI thread.
- **What updates on a switch**:
  - Every window, popup, adorner overlay (dialogs, drawers), Growl card and the desktop Growl window.
  - `MuzzleAssist.ShadowDepth` shadows, which are live `DropShadowEffect`s re-synced by `MuzzleShadows.Refresh()`.
  - `MuzzleWindow`'s DWM frame: immersive dark mode plus a border colour taken from `Muzzle.Color.SurfaceBorder`.
- **Dark-specific touches**:
  - Ambient shadows are pure black at 35–50 % opacity (light uses plum at 8–15 %).
  - Elevated surfaces (cards, menus, dialogs, drawers, toasts, popovers) use `Muzzle.Brush.SurfaceRim`, a border with a 1 px `#3D3750` rim highlight along the top edge.
- **Tokens added for theming**:
  - `Muzzle.Brush.OnStatus`: text and icons on pastel fills.
  - `SwatchOutline`, `Shimmer`, `Checker` (transparency checkerboard) and `Thumb`.
  - `PrimaryWash` / `SecondaryWash` / `MintWash`: very soft hero tints.
  - `Muzzle.ThemeName`: the string "Light" or "Dark".
- **Your own resources**: use `{DynamicResource Muzzle.Brush.*}` / `{DynamicResource Muzzle.Color.*}` (including on `GradientStop.Color`) and they follow the theme. Values read once in code (`TryFindResource`) won't; use `SetResourceReference`, or refresh on `ThemeChanged`.
- **Starting in dark**: call `MuzzleThemeManager.ApplyTheme(MuzzleTheme.System)` in `App.OnStartup`, or merge `Muzzle.Dark.xaml` after `Muzzle.xaml` in App.xaml. That top-level slot is the one the manager swaps.

## Accent colours

Accents sit on top of either base theme and switch independently of it: changing Light ↔ Dark keeps the accent, and changing the accent keeps the theme. **Coral** (toe-bean blush) is the default and reproduces the original palette exactly, so existing apps look the same.

| Accent | Light base | Dark base | Secondary role | Tertiary role |
|---|---|---|---|---|
| `Coral` (default) | `#F497A9` | `#F6A2B3` | lavender | mint |
| `Lavender` | `#A896E6` | `#B8A9F2` | blush | mint |
| `Mint` | `#7CCBB9` | `#9FD8CB` | lavender | peach |
| `Peach` | `#F5A67E` | `#F7B492` | lavender | mint |

```csharp
MuzzleThemeManager.ApplyAccent(MuzzleAccent.Lavender);                 // or animate: true for a crossfade
MuzzleThemeManager.ApplyCustomAccent(Color.FromRgb(0x5F, 0xA8, 0xD3)); // any colour; a full ramp is derived
MuzzleThemeManager.AccentChanged += (_, e) => Debug.WriteLine(e.Accent);
```

```xml
<!-- Squishy swatches: the active one shows a ring and a checkmark. -->
<mz:AccentSwatch Accent="Coral" />
<mz:AccentSwatch Accent="Lavender" />
<mz:AccentSwatch Accent="Mint" />
<mz:AccentSwatch Accent="Peach" />
```

- **Files**: `Themes/Accents/Muzzle.Accent.<Name>.Light.xaml` and `.Dark.xaml`, one pair per accent, because the same accent needs different values for contrast in each theme.
- **What an accent overrides**: `Primary`, `PrimaryHover`, `PrimaryPressed`, `PrimarySoft`, `PrimarySoftHover`, `PrimaryText`, `OnPrimary`, `FocusRing` and `PrimaryWash`, as both `Muzzle.Color.*` and `Muzzle.Brush.*`. These cover every control that uses the accent: buttons, selection pills, toggles, sliders, tabs, navigation, progress, focus rings, badges and pickers.
- **Soft fills**: the brief's translucent SoftFill (15 % in light, 20 % in dark) is flattened onto the theme surface, so stacked layers (hover under selection) never double-tint.
- **Supporting roles**: each accent also redefines the `Secondary*` family and a new `Tertiary*` family (`Tertiary`, `TertiarySoft`, `TertiaryText`). They rotate so neither ever shares the primary's hue; for example, with Lavender as the accent, secondary becomes blush.
  - Secondary buttons, tag chips, badges, calendar rings, the lavender progress style and the loading-spinner dots therefore never clash with the primary.
  - The named-hue styles keep their colour on purpose: `Muzzle.Button.Mint`, `Muzzle.ProgressBar.Mint`, and the `Mint*` / status tokens.
- **Where the accent slot lives**: `Muzzle.xaml` merges `Muzzle.Accent.Coral.Light.xaml` right after the palette slot.
  - `MuzzleThemeManager` keeps an accent slot directly after whichever palette is in effect, inserting one if your App.xaml layers its own palette later.
  - It swaps the slot's Light / Dark variant whenever the theme changes.
- **`MuzzleThemeManager`**:
  - `CurrentAccent` (detected from the merged slot until you apply one), `CustomAccentColor`, `ApplyAccent`, `ApplyCustomAccent` and the `AccentChanged` event.
  - `CreateAccentDictionary(accent, dark, custom)` returns the dictionary it would merge.
  - Open windows (including the desktop Growl window), popups and overlays update live, with the same optional crossfade as theme switches.
- **Custom accents**: the ramp is derived from the base colour.
  - Hover is mixed toward white and pressed toward black. Soft fills, wash and focus ring are made the same way as the presets'.
  - `PrimaryText` is a deeper (light theme) or lighter (dark theme) shade for readable text.
  - `OnPrimary` is chosen by luminance: dark text on light accents, white on deep ones.
  - Secondary and tertiary come from the preset whose supporting hues sit furthest from the colour.
- **Custom base palette**: `MuzzleThemeManager.ApplyCustomPalette(canvas, text, accent)` fits the theme to your own page colours, for example a sepia or dimmed reading profile.
  - The surface, border, track, hover, scrollbar, tooltip and text tokens are re-derived: each keeps its place between the stock canvas and text colours, moved onto the line between yours. Accent, status and decorative colours stay as they are.
  - The Light or Dark base (shadows, accent variant) follows from which of the two colours is lighter. The optional third argument is passed to `ApplyCustomAccent`.
  - It stays in effect across accent changes. `ClearCustomPalette()` or any `ApplyTheme` call ends it. `CustomPaletteCanvas` / `CustomPaletteText` report it, and `ThemeChanged` is raised.
  - It is one top-level dictionary swap per call, so it can follow a slider. `CreateCustomPaletteDictionary(canvas, text)` returns the dictionary on its own.
- **Regenerating**: `tools/Generate-Palettes.ps1` writes both base palettes and all eight accent files from one token table. Edit the table there, not the generated XAML.

## Tests

```powershell
dotnet test tests/Muzzle.Theme.Tests
```

`Muzzle.Theme.Tests` is an xUnit suite (with `Xunit.StaFact`) that runs on net10.0-windows: 234 tests, about 25 s.

- **`MuzzleWpfTestFixture`** (a collection fixture shared by every WPF test):
  - It starts one `Application` on a dedicated STA thread, or adopts an existing one, and merges `Themes/Muzzle.xaml` exactly as an app does.
  - Test bodies run on that thread through `Run` / `RunAsync`, as in a real app: the theme manager and overlay hosts expect the application's dispatcher.
  - It records binding errors and unhandled dispatcher exceptions, so tests can `AssertClean()`. Off-screen windows never take focus and are closed in `finally` blocks.
- **Converters**: every converter, including `null`, `UnsetValue`, wrong types, invalid strings, clamping, inversion and round trips. These are plain `[Fact]`s, with no WPF thread needed.
- **Theming**:
  - Key parity: Light ⇄ Dark, all eight accent files, and Coral ≡ the base palette.
  - `ApplyTheme` / `ApplyAccent` / `ApplyCustomAccent`: state, events, resolved colours, elements already on screen repainting, and the accent surviving theme switches (and vice versa).
  - `MuzzleThemeResourceDictionary`, both standalone and as `Application.Resources`.
- **Behaviors**:
  - `JellyBehavior`: transform installation that keeps existing transforms, real hover and press animations, intensity 0, and every entrance.
  - `ClipHelper`: Grid and Border clips, resizing, per-corner radii, and zero to remove.
  - `EscapeKeyBehavior`, `FocusTrap` and `ScrollViewerAssist`.
- **Controls**:
  - Every Muzzle control and every restyled standard control (55 cases, plus a MuzzleWindow test of its title-bar content and caption parts) is templated in an off-screen window. Declared template parts are checked, and the run must produce no binding errors or exceptions. All of them are also checked in Dark with each accent.
  - Behaviour checks for PropertyGrid reflection, NumericUpDown, TagInput, RangeSlider, the password estimator, CommandPalette fuzzy search, SplitButton, CodeBlock, Avatar, Carousel, ImageViewer, Badge, Pagination, StepBar, Rating, ColorPicker, ShortcutBadge and every `MuzzleIconKind`.
  - Growl (every type, Clear, auto-dismiss), Dialog (Show / Close / ShowAsync) and Drawer (all four edges, plus a declared drawer).
## Gallery snapshot mode

`Muzzle.Gallery.exe --snapshot <dir>` renders every page to PNG and exits. Along the way it:

- exercises interactive states: tab switch, expander, dropdown, toggles, property-grid edits, growls, dialogs, drawers, pagination, breadcrumbs, steps, pane collapse, every picker, badges, banners, skeletons and popover placement;
- renders popup contents (calendar, time columns, suggestions) separately;
- exercises the Data & media controls: DataGrid sorting and editing, avatar initials and broken-image fallback, carousel looping, image-viewer zoom limits and reset, and tree expansion;
- exercises the utilities and desktop controls: converters, ClipHelper, a smooth wheel glide, the focus trap with Tab and Esc, real Ctrl+K / Esc on the command palette (fuzzy search, empty state, Enter navigation), real clicks on split-button segments and menu items, RangeSlider coercion, password scores, tray icon add / menu / remove, a Mica screen capture, and WindowStateSaver round-trips (including an off-screen placement) against a temporary folder;
- runs an accent pass: every Light/Dark × Coral/Lavender/Mint/Peach combination (`accent-*.png`), checking the resolved tokens, that a live button repaints, that the secondary and tertiary roles never match the primary, that theme and accent switch independently, the custom accent, and the title-bar swatch state;
- runs a dark pass: switches through the title-bar theme toggle, captures every page plus Growl, Dialog, Drawer and the popups as `dark-*.png`, and checks that existing windows, live shadows and the Light → Dark → System → Light cycle all update;
- logs binding errors and probes non-client hit testing.

It's useful for visual regression checks.

## Typography

The default font is **[Atkinson Hyperlegible Next](https://github.com/googlefonts/atkinson-hyperlegible-next)** by the Braille Institute, designed for low-vision readers. All seven static weights (ExtraLight, Light, Regular, Medium, SemiBold, Bold, ExtraBold) and their italics are embedded as WPF resources in `Muzzle.Theme` (license: `Themes/Fonts/AtkinsonHyperlegibleNext-OFL.txt`), so nothing needs to be installed. It's exposed as `Muzzle.FontFamily` / `Muzzle.FontFamily.Display` (falling back to Segoe UI) and applied by `MuzzleWindow`, tooltips and the `Muzzle.Text.*` styles. In a plain `Window`, opt in with `FontFamily="{DynamicResource Muzzle.FontFamily}"`.

Static files are used instead of the variable font because WPF can't select weights from a variable font's axis, so every `FontWeight` maps to a real face.

## Notes

- **Calm mode / reduced motion**: code-driven jelly (button squish, hover growth, tab pill, progress glide, `Jelly.Wobble`) honours `JellyIntensity = 0` and the Windows *Show animations* setting. Template storyboards (checkmark spring, switch thumb, expander, dropdown) are XAML `Storyboard`s and always play.
- `DataGridAssist.AutoStyleColumns` styles columns as they are added. Columns that set `ElementStyle` / `EditingElementStyle` themselves keep their own styles.
- **Gallery shortcuts**: Ctrl+K opens the command palette and Ctrl+F focuses the page search. The snapshot run sends real keystrokes and clicks to the gallery window, and only when it is in the foreground (it briefly brings it forward, then puts the cursor back). It never writes to the clipboard and never touches your saved window placement.
- **Copy feedback** isn't covered by the snapshot run, because testing it would overwrite your clipboard.