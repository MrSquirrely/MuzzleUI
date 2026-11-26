using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Muzzle.Gallery.Diagnostics;

/// <summary>
/// <c>Muzzle.Gallery.exe --snapshot &lt;dir&gt;</c>: renders every gallery page to PNG, logs binding errors and
/// probes the window's non-client hit testing, then exits. Handy for CI and visual regression checks.
/// </summary>
internal static class SnapshotRunner {
    private const int WmNcHitTest = 0x0084;

    public static void Run(Application app, string directory) {
        Directory.CreateDirectory(directory);
        StreamWriter log = new(Path.Combine(directory, "log.txt")) { AutoFlush = true };
        PresentationTraceSources.Refresh();
        PresentationTraceSources.DataBindingSource.Listeners.Add(new TextWriterTraceListener(log));
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        app.DispatcherUnhandledException += (_, e) => {
            log.WriteLine("UNHANDLED: " + e.Exception);
            e.Handled = true;
            app.Shutdown(2);
        };
        try {
            // Snapshot runs use a fixed size and must not touch the user's saved window placement.
            Theme.WindowStateSaver.IsEnabled = false;
            MainWindow window = new() { Width = 1280, Height = 860, WindowStartupLocation = WindowStartupLocation.Manual, Left = 40, Top = 40 };
            window.Show();
            _ = CaptureAsync(app, window, directory, log);
        }
        catch (Exception exception) {
            log.WriteLine("STARTUP: " + exception);
            app.Shutdown(1);
        }
    }

    private static async Task CaptureAsync(Application app, MainWindow window, string directory, StreamWriter log) {
        try {
            await Task.Delay(900);
            for (int i = 0; i < window.PageCount; i++) {
                window.NavigateTo(i);
                await Task.Delay(1300);
                Save(window, Path.Combine(directory, $"page-{i}.png"));
                log.WriteLine($"captured page {i}");
            }
            ProbeHitTest(window, log);
            await ExerciseAsync(window, directory, log);
            await ExerciseDataMediaAsync(window, directory, log);
            await ExerciseAccentsAsync(window, directory, log);
            await ExerciseUtilitiesAsync(window, directory, log);
            await ExerciseDesktopAsync(window, directory, log);
            await ExerciseDarkAsync(window, directory, log);
            log.WriteLine("done");
            app.Shutdown(0);
        }
        catch (Exception exception) {
            log.WriteLine("CAPTURE: " + exception);
            app.Shutdown(3);
        }
    }

    /// <summary>Drives interactive states so storyboards, jelly controllers and editors actually run.</summary>
    private static async Task ExerciseAsync(MainWindow window, string directory, StreamWriter log) {
        // Containers: switch tabs (sliding pill), collapse an expander.
        window.NavigateTo(5);
        await Task.Delay(700);
        TabControl? tabs = FindFirst<TabControl>(window);
        if (tabs is not null) {
            tabs.SelectedIndex = 2;
            await Task.Delay(900);
            Save(window, Path.Combine(directory, "exercise-tabs.png"));
            log.WriteLine("tabs: switched to index 2");
        }
        Expander? expander = FindFirst<Expander>(window, static e => e.IsExpanded && e.Header is string);
        if (expander is not null) {
            expander.IsExpanded = false;
            await Task.Delay(400);
            expander.IsExpanded = true;
            log.WriteLine("expander: collapsed + expanded");
        }

        // Buttons: synthetic hover / press / release through the jelly controller.
        window.NavigateTo(1);
        await Task.Delay(600);
        Button? button = FindFirst<Button>((DependencyObject)window.FindName("PageHost"));
        if (button is not null) {
            button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
            button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
            await Task.Delay(150);
            button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent });
            await Task.Delay(300);
            log.WriteLine("button: jelly hover/press/release ran");
        }

        // Inputs: focus a TextBox (focus ring + pulse) and open a ComboBox dropdown.
        window.NavigateTo(2);
        await Task.Delay(600);
        TextBox? text = FindFirst<TextBox>((DependencyObject)window.FindName("PageHost"));
        text?.Focus();
        ComboBox? combo = FindFirst<ComboBox>((DependencyObject)window.FindName("PageHost"));
        if (combo is not null) {
            combo.IsDropDownOpen = true;
            await Task.Delay(700);
            combo.IsDropDownOpen = false;
            log.WriteLine("combo: opened + closed");
        }
        await Task.Delay(400);
        Save(window, Path.Combine(directory, "exercise-focus.png"));

        // Selection: flip every toggle.
        window.NavigateTo(3);
        await Task.Delay(600);
        foreach (ToggleButton toggle in FindAll<ToggleButton>((DependencyObject)window.FindName("PageHost"))) {
            if (toggle.IsEnabled && toggle is CheckBox or RadioButton or Theme.Controls.ToggleSwitch) {
                toggle.IsChecked = toggle.IsChecked != true;
            }
        }
        await Task.Delay(900);
        Save(window, Path.Combine(directory, "exercise-toggles.png"));
        log.WriteLine("selection: toggled");

        // Property grid: edit through every editor kind, then inspect a live Button.
        window.NavigateTo(6);
        await Task.Delay(700);
        Theme.Controls.PropertyGrid? grid = FindFirst<Theme.Controls.PropertyGrid>(window);
        if (grid is not null) {
            foreach (Theme.Controls.PropertyItem item in grid.Properties) {
                switch (item.EditorKind) {
                    case Theme.Controls.PropertyEditorKind.Text when item.Name == "Caption":
                        item.EditText = "Edited!";
                        break;
                    case Theme.Controls.PropertyEditorKind.Range:
                        item.NumericValue = item.Maximum;
                        break;
                    case Theme.Controls.PropertyEditorKind.Numeric:
                        item.IncrementCommand.Execute(null);
                        break;
                    case Theme.Controls.PropertyEditorKind.Color:
                        item.EditText = "#9FD8CB";
                        break;
                    case Theme.Controls.PropertyEditorKind.Brush:
                        item.EditText = "B8A9E6";
                        break;
                    case Theme.Controls.PropertyEditorKind.Enum:
                        item.Value = item.EnumValues[^1];
                        break;
                    case Theme.Controls.PropertyEditorKind.Boolean:
                        item.Value = true;
                        break;
                }
                log.WriteLine($"  {item.Category}/{item.DisplayName} [{item.EditorKind}] = '{item.EditText}' error={item.HasError}");
            }
            grid.SelectedProperty = grid.Properties[0];
            grid.SearchText = "corner";
            await Task.Delay(900);
            Save(window, Path.Combine(directory, "exercise-grid.png"));
            grid.SearchText = string.Empty;
            if (window.FindName("PageHost") is ContentControl { Content: FrameworkElement page } && page.FindName("InspectButton") is RadioButton inspectButton) {
                inspectButton.IsChecked = true;
                await Task.Delay(900);
                log.WriteLine($"grid: inspecting Button with {grid.Properties.Count} properties in {grid.Categories.Count} categories");
                Save(window, Path.Combine(directory, "exercise-grid-button.png"));
            }
        }
        Muzzle.Theme.Jelly.Wobble((FrameworkElement)window.FindName("PageHost"));
        await Task.Delay(300);
        await ExerciseOverlaysAsync(window, directory, log);
    }

    private static async Task ExerciseOverlaysAsync(MainWindow window, string directory, StreamWriter log) {
        window.NavigateTo(8);
        await Task.Delay(600);

        // Growl: every type, plus an Ask answered through its callback.
        Theme.Controls.Growl.Info("Biscuit is napping in the sunbeam.", "Nap in progress");
        Theme.Controls.Growl.Success("Dinner is served.", "Yum");
        Theme.Controls.Growl.Warning("The treat jar is getting low.", "Heads up");
        Theme.Controls.Growl.Error("Someone knocked a glass off the table.", "Oops");
        bool? answer = null;
        Theme.Controls.GrowlCard? ask = Theme.Controls.Growl.Ask("Open a new bag of treats?", confirmed => answer = confirmed, "Treat time");
        await Task.Delay(1100);
        Save(window, Path.Combine(directory, "overlay-growl.png"));
        ask?.Close();
        await Task.Delay(100);
        log.WriteLine($"growl: 5 shown, ask callback = {answer?.ToString() ?? "not called"} (expect False)");
        Theme.Controls.Growl.ClearAll();
        await Task.Delay(700);
        log.WriteLine($"growl: cleared, container children = {FindFirst<Theme.Controls.GrowlContainer>(window)?.Children.Count}");

        // Dialog: typed result through CloseWithResult, IDialogResultable, and Dialog.Close().
        Task<Dialogs.AdoptionRequest?> formTask = Theme.Controls.Dialog.ShowAsync<Dialogs.AdoptionRequest>(new Dialogs.AdoptionFormDialog());
        await Task.Delay(900);
        Save(window, Path.Combine(directory, "overlay-dialog.png"));
        Theme.Controls.Dialog? form = FindFirst<Theme.Controls.Dialog>(window);
        log.WriteLine($"dialog: open = {form is not null}, focus inside = {form?.IsKeyboardFocusWithin}");
        form?.CloseWithResult(new Dialogs.AdoptionRequest("Ada", "Mochi", true));
        Dialogs.AdoptionRequest? request = await formTask;
        log.WriteLine($"dialog: form result = {request}");
        Dialogs.ConfirmDialog confirm = new("Trim claws?", "Test");
        Task<bool> confirmTask = Theme.Controls.Dialog.ShowAsync<bool>(confirm);
        await Task.Delay(500);
        confirm.Result = true;
        confirm.CloseAction?.Invoke();
        log.WriteLine($"dialog: confirm result = {await confirmTask} (expect True)");
        Task<object?> closeTask = Theme.Controls.Dialog.Show(new TextBlock { Text = "bye" }).Completion;
        await Task.Delay(400);
        Theme.Controls.Dialog.Close();
        log.WriteLine($"dialog: static Close result = {await closeTask ?? "null"}");

        // Drawers from each edge.
        foreach (Dock dock in new[] { Dock.Left, Dock.Right, Dock.Top, Dock.Bottom }) {
            Theme.Controls.Drawer drawer = Theme.Controls.Drawer.Show(new TextBlock { Text = $"{dock} drawer content", TextWrapping = TextWrapping.Wrap }, dock, $"{dock} drawer");
            if (dock is Dock.Top or Dock.Bottom) {
                drawer.DrawerSize = 220;
            }
            await Task.Delay(900);
            Save(window, Path.Combine(directory, $"overlay-drawer-{dock}.png"));
            drawer.IsOpen = false;
            await Task.Delay(500);
            log.WriteLine($"drawer {dock}: closed, still attached = {drawer.Parent is not null} (expect False)");
        }

        // Declarative drawer bound two-way.
        if (FindFirst<Theme.Controls.Drawer>((DependencyObject)window.FindName("PageHost")) is Theme.Controls.Drawer sandbox) {
            sandbox.BringIntoView();
            sandbox.IsOpen = true;
            await Task.Delay(900);
            Save(window, Path.Combine(directory, "overlay-drawer-sandbox.png"));
            sandbox.IsOpen = false;
            await Task.Delay(500);
            log.WriteLine($"drawer sandbox: visibility after close = {sandbox.Visibility}");
        }

        // Desktop-corner fallback window.
        Theme.Controls.Growl.Show(new Theme.Controls.GrowlInfo { Message = "Desktop toast", IsGlobal = true });
        await Task.Delay(600);
        Theme.Controls.Growl.ClearGlobal();
        await Task.Delay(700);
        log.WriteLine("growl: desktop window shown and cleared");

        // MessageBox: shown without blocking so it can be captured; a click on Cancel closes it.
        Theme.Controls.MessageBoxWindow box = new(
            "Biscuit knocked a glass off the table. Save your changes before cleaning up?", "Unsaved changes",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question) { Owner = window, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        box.Show();
        await Task.Delay(700);
        Save(box, Path.Combine(directory, "messagebox.png"));
        Button? cancelButton = FindFirst<Button>(box, static b => b.IsCancel);
        cancelButton?.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        await Task.Delay(300);
        log.WriteLine($"messagebox: {box.ActualWidth:0}x{box.ActualHeight:0}, cancel -> result {box.Result} (expect Cancel), visible {box.IsVisible} (expect False)");
        await ExerciseNavigationAsync(window, directory, log);
    }

    private static async Task ExerciseNavigationAsync(MainWindow window, string directory, StreamWriter log) {
        window.NavigateTo(9);
        await Task.Delay(900);
        if ((window.FindName("PageHost") as ContentControl)?.Content is not Pages.NavigationPage page) {
            log.WriteLine("navigation: page not found");
            return;
        }

        // Pagination: step forward, jump far, and check the pill target.
        Theme.Controls.Pagination pager = (Theme.Controls.Pagination)page.FindName("Pager");
        for (int i = 0; i < 3; i++) {
            Theme.Controls.Pagination.NextPageCommand.Execute(null, pager);
            await Task.Delay(150);
        }
        log.WriteLine($"pagination: after 3x next = {pager.CurrentPage} (expect 4), slots = {string.Join(" ", pager.Slots.Select(static s => s.IsCurrent ? $"[{s.Label}]" : s.Label))}");
        Theme.Controls.Pagination.GoToPageCommand.Execute(20, pager);
        await Task.Delay(150);
        log.WriteLine($"pagination: go to 20 -> slots = {string.Join(" ", pager.Slots.Select(static s => s.IsCurrent ? $"[{s.Label}]" : s.Label))}");
        Theme.Controls.Pagination.LastPageCommand.Execute(null, pager);
        await Task.Delay(150);
        Theme.Controls.Pagination.PreviousPageCommand.Execute(null, pager);
        log.WriteLine($"pagination: last then prev = {pager.CurrentPage} (expect 23)");

        // StepBar forward two steps (horizontal + bound vertical).
        Theme.Controls.StepBar steps = (Theme.Controls.StepBar)page.FindName("Steps");
        steps.Next();
        steps.Next();
        log.WriteLine($"stepbar: index = {steps.StepIndex} (expect 3)");

        // Breadcrumbs: go deeper and narrow so it overflows.
        Button? deeper = FindAll<Button>(page).FirstOrDefault(static b => (b.Content as string) == "Go deeper");
        for (int i = 0; i < 3; i++) {
            deeper?.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        ((Slider)page.FindName("CrumbWidth")).Value = 360;

        // Transition + demo nav selection.
        Button? nextCard = FindAll<Button>(page).FirstOrDefault(static b => (b.Content as string) == "Next card");
        nextCard?.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        ((Theme.Controls.NavigationView)page.FindName("DemoNav")).SelectedIndex = 2;
        await Task.Delay(1000);
        Save(window, Path.Combine(directory, "nav-page.png"));

        ScrollViewer? scroller = FindFirst<ScrollViewer>(page);
        scroller?.ScrollToVerticalOffset(560);
        await Task.Delay(600);
        Save(window, Path.Combine(directory, "nav-page-lower.png"));
        scroller?.ScrollToVerticalOffset(1100);
        await Task.Delay(600);
        Save(window, Path.Combine(directory, "nav-page-steps.png"));

        // Breadcrumb click trims the path.
        Theme.Controls.BreadcrumbBar crumbs = (Theme.Controls.BreadcrumbBar)page.FindName("Crumbs");
        int before = crumbs.Items.Count;
        if (crumbs.ItemContainerGenerator.ContainerFromIndex(1) is Theme.Controls.BreadcrumbItem second) {
            second.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, second));
        }
        await Task.Delay(200);
        log.WriteLine($"breadcrumb: {before} segments, clicked index 1 -> {crumbs.Items.Count} (expect 2)");

        // Collapse the gallery's own NavigationView.
        Theme.Controls.NavigationView nav = (Theme.Controls.NavigationView)window.FindName("NavList");
        nav.IsPaneOpen = false;
        await Task.Delay(800);
        Save(window, Path.Combine(directory, "nav-compact.png"));
        log.WriteLine($"navigationview: compact width = {nav.ActualWidth:0}");
        nav.IsPaneOpen = true;
        await Task.Delay(800);
        log.WriteLine($"navigationview: open width = {nav.ActualWidth:0}");
        await ExercisePickersAsync(window, directory, log);
    }

    private static async Task ExercisePickersAsync(MainWindow window, string directory, StreamWriter log) {
        window.NavigateTo(10);
        await Task.Delay(900);
        if ((window.FindName("PageHost") as ContentControl)?.Content is not Pages.PickersPage page) {
            log.WriteLine("pickers: page not found");
            return;
        }
        Save(window, Path.Combine(directory, "pickers-top.png"));

        // NumericUpDown
        Theme.Controls.NumericUpDown treats = (Theme.Controls.NumericUpDown)page.FindName("TreatCount");
        Theme.Controls.NumericUpDown.IncreaseCommand.Execute(null, treats);
        Theme.Controls.NumericUpDown.IncreaseCommand.Execute(null, treats);
        log.WriteLine($"numeric: after 2x increase = {treats.Value} (expect 5)");
        treats.Value = 99;
        log.WriteLine($"numeric: set 99 -> {treats.Value} (expect 20)");

        // TagInput
        Theme.Controls.TagInput tags = (Theme.Controls.TagInput)page.FindName("Tags");
        bool added = tags.AddTag("chonky");
        bool duplicate = tags.AddTag("FLUFFY");
        await Task.Delay(300);
        tags.RemoveTagAt(0);
        await Task.Delay(500);
        log.WriteLine($"tags: add={added} dup={duplicate} (expect True False), now [{string.Join(", ", tags.Tags)}] (expect orange, sleepy, chonky)");

        // Rating
        Theme.Controls.Rating stars = (Theme.Controls.Rating)page.FindName("StarRating");
        stars.Value = 2.3;
        log.WriteLine($"rating: 2.3 with halves -> {stars.Value} (expect 2.5)");
        stars.Value = 9;
        log.WriteLine($"rating: 9 -> {stars.Value} (expect 5)");

        // ColorPicker
        Theme.Controls.ColorPicker picker = (Theme.Controls.ColorPicker)page.FindName("Picker");
        picker.HexText = "#9FD8CB";
        log.WriteLine($"color: hex -> rgb {picker.Red},{picker.Green},{picker.Blue} (expect 159,216,203), hue {picker.Hue:0}");
        picker.Hue = 330;
        log.WriteLine($"color: hue 330 -> {picker.HexText}");
        picker.Red = 244;
        log.WriteLine($"color: red 244 -> {picker.HexText}");

        // SearchBar suggestions
        Theme.Controls.SearchBar search = (Theme.Controls.SearchBar)page.FindName("CatSearch");
        search.Focus();
        search.Text = "b";
        await Task.Delay(500);
        log.WriteLine($"search: suggestions [{string.Join(", ", search.Suggestions)}], open = {search.IsSuggestionListOpen}");
        if (search.Template.FindName("PART_Popup", search) is System.Windows.Controls.Primitives.Popup searchPopup && (searchPopup.Child is FrameworkElement searchChild)) {
            await Task.Delay(400);
            SaveVisual(searchChild, Path.Combine(directory, "pickers-search-popup.png"));
        }
        search.Text = string.Empty;
        await Task.Delay(300);

        // DatePicker + TimePicker dropdowns
        DatePicker date = (DatePicker)page.FindName("VetDate");
        date.BringIntoView();
        await Task.Delay(400);
        date.IsDropDownOpen = true;
        await Task.Delay(800);
        if (date.Template.FindName("PART_Popup", date) is System.Windows.Controls.Primitives.Popup datePopup && (datePopup.Child is FrameworkElement calendar)) {
            SaveVisual(calendar, Path.Combine(directory, "pickers-calendar-popup.png"));
        }
        date.IsDropDownOpen = false;
        Theme.Controls.TimePicker time = (Theme.Controls.TimePicker)page.FindName("VetTime");
        time.IsDropDownOpen = true;
        await Task.Delay(800);
        if (time.Template.FindName("PART_Popup", time) is System.Windows.Controls.Primitives.Popup timePopup && (timePopup.Child is FrameworkElement slots)) {
            SaveVisual(slots, Path.Combine(directory, "pickers-time-popup.png"));
            if (time.Template.FindName("PART_HourList", time) is ListBox hours) {
                hours.SelectedItem = "3";
            }
        }
        time.IsDropDownOpen = false;
        log.WriteLine($"time: picked hour 3 -> {time.SelectedTime} / '{time.DisplayText}' (expect 03:15:00 / 3:15 AM)");
        ScrollViewer? scroller = FindFirst<ScrollViewer>(page);
        scroller?.ScrollToVerticalOffset(900);
        await Task.Delay(700);
        Save(window, Path.Combine(directory, "pickers-lower.png"));

        // ColorPickerDropDown: two-way with the inline picker, and its popup.
        Theme.Controls.ColorPickerDropDown dropDown = (Theme.Controls.ColorPickerDropDown)page.FindName("PickerDropDown");
        dropDown.SelectedColor = Color.FromRgb(0x98, 0xC9, 0xF2);
        log.WriteLine($"color dropdown: set #98C9F2 -> inline picker {picker.HexText}, dropdown {dropDown.HexText}");
        dropDown.IsDropDownOpen = true;
        await Task.Delay(900);
        if (dropDown.Template.FindName("PART_Popup", dropDown) is System.Windows.Controls.Primitives.Popup colorPopup && (colorPopup.Child is FrameworkElement colorChild)) {
            SaveVisual(colorChild, Path.Combine(directory, "pickers-color-dropdown.png"));
        }
        dropDown.IsDropDownOpen = false;
        await ExerciseFeedbackAsync(window, directory, log);
    }

    private static async Task ExerciseFeedbackAsync(MainWindow window, string directory, StreamWriter log) {
        window.NavigateTo(11);
        await Task.Delay(900);
        if ((window.FindName("PageHost") as ContentControl)?.Content is not Pages.FeedbackPage page) {
            log.WriteLine("feedback: page not found");
            return;
        }
        Save(window, Path.Combine(directory, "feedback-top.png"));

        Theme.Controls.Badge inbox = (Theme.Controls.Badge)page.FindName("InboxBadge");
        inbox.Value = 150;
        log.WriteLine($"badge: 150 -> '{inbox.DisplayText}' (expect 99+), visible {inbox.IsBadgeVisible}");
        inbox.Value = 0;
        log.WriteLine($"badge: 0 -> visible {inbox.IsBadgeVisible} (expect False)");
        inbox.Value = 4;
        Button bell = (Button)page.FindName("BellButton");
        Theme.MuzzleAssist.SetBadge(bell, 12);
        await Task.Delay(300);
        log.WriteLine($"badge: attached on bell = {Theme.MuzzleAssist.GetBadge(bell)}");

        // Banners: dismiss the first, then reopen.
        StackPanel banners = (StackPanel)page.FindName("Banners");
        Theme.Controls.InfoBar first = (Theme.Controls.InfoBar)banners.Children[0];
        ScrollViewer? scroller = FindFirst<ScrollViewer>(page);
        scroller?.ScrollToVerticalOffset(520);
        await Task.Delay(600);
        Save(window, Path.Combine(directory, "feedback-middle.png"));
        first.IsOpen = false;
        await Task.Delay(700);
        log.WriteLine($"infobar: after close visibility = {first.Visibility} (expect Collapsed)");
        Save(window, Path.Combine(directory, "feedback-banner-closed.png"));
        first.IsOpen = true;
        await Task.Delay(400);
        log.WriteLine($"infobar: reopened visibility = {first.Visibility}, height = {first.ActualHeight:0}");

        // Skeleton -> content.
        ((Theme.Controls.ToggleSwitch)page.FindName("LoadingSwitch")).IsChecked = false;
        await Task.Delay(700);
        scroller?.ScrollToVerticalOffset(300);
        await Task.Delay(400);
        Save(window, Path.Combine(directory, "feedback-loaded.png"));

        // Popover.
        Theme.Controls.Popover popover = (Theme.Controls.Popover)page.FindName("ProfilePopover");
        popover.BringIntoView();
        await Task.Delay(300);
        popover.IsOpen = true;
        await Task.Delay(900);
        if (popover.Template.FindName("PART_Popup", popover) is System.Windows.Controls.Primitives.Popup pop && (pop.Child is FrameworkElement bubble)) {
            SaveVisual(bubble, Path.Combine(directory, "feedback-popover.png"));
            if (popover.Template.FindName("PART_Bubble", popover) is FrameworkElement card) {
                Point anchor = popover.PointToScreen(new Point(popover.ActualWidth / 2, popover.ActualHeight));
                Point cardTop = card.PointToScreen(new Point(card.ActualWidth / 2, 0));
                log.WriteLine($"popover: open = {pop.IsOpen}, card top - anchor bottom = {cardTop.Y - anchor.Y:0} px (expect 12), centre offset = {cardTop.X - anchor.X:0} px (expect 0)");
            }
        }
        popover.IsOpen = false;
        await Task.Delay(400);

        // Real clicks: a click opens it and it stays open after release; clicking the anchor again closes it;
        // an outside click closes it without the release reopening it.
        window.Activate();
        await Task.Delay(300);
        FrameworkElement anchorButton = (FrameworkElement)popover.Content;
        if (await RealClickAsync(window, anchorButton)) {
            await Task.Delay(500);
            log.WriteLine($"popover: after one real click open = {popover.IsOpen} (expect True)");
            await RealClickAsync(window, anchorButton);
            await Task.Delay(500);
            log.WriteLine($"popover: click anchor again open = {popover.IsOpen} (expect False)");
            await RealClickAsync(window, anchorButton);
            await Task.Delay(500);
            FrameworkElement lead = (FrameworkElement)page.FindName("Banners");
            await RealClickAsync(window, lead, new Point(lead.ActualWidth - 20, 10));
            await Task.Delay(500);
            log.WriteLine($"popover: reopen then outside click open = {popover.IsOpen} (expect False)");
        }
        else {
            log.WriteLine("popover: real-click check skipped (window not in foreground)");
        }
        popover.IsOpen = false;
    }

    private static async Task ExerciseDataMediaAsync(MainWindow window, string directory, StreamWriter log) {
        window.NavigateTo(12);
        await Task.Delay(1000);
        if ((window.FindName("PageHost") as ContentControl)?.Content is not Pages.DataMediaPage page) {
            log.WriteLine("data: page not found");
            return;
        }
        ScrollViewer? scroller = FindFirst<ScrollViewer>(page);

        // DataGrid: sort by Age through the header, then edit a text cell and a combo cell.
        DataGrid grid = (DataGrid)page.FindName("CatGrid");
        DataGridColumnHeader? ageHeader = FindFirst<DataGridColumnHeader>(grid, static h => (h.Column?.Header as string) == "Age");
        MethodInfoInvoke(ageHeader);
        await Task.Delay(700);
        int firstAge = (grid.Items[0] as Models.CatRecord)?.Age ?? -1;
        log.WriteLine($"datagrid: sorted Age {ageHeader?.SortDirection} (expect Ascending), first age = {firstAge} (expect 1)");
        MethodInfoInvoke(ageHeader);
        await Task.Delay(700);
        log.WriteLine($"datagrid: sorted again {ageHeader?.SortDirection} (expect Descending), first age = {(grid.Items[0] as Models.CatRecord)?.Age} (expect 9)");
        Save(window, Path.Combine(directory, "data-grid-sorted.png"));
        DataGridTextColumn treats = (DataGridTextColumn)grid.Columns[5];
        log.WriteLine($"datagrid: auto styles text = {treats.ElementStyle is not null}/{treats.EditingElementStyle is not null}, check = {((DataGridCheckBoxColumn)grid.Columns[3]).EditingElementStyle is not null}, combo = {((DataGridComboBoxColumn)grid.Columns[1]).EditingElementStyle is not null} (expect True)");
        grid.Focus();
        grid.SelectedIndex = 1;
        grid.CurrentCell = new DataGridCellInfo(grid.Items[1], grid.Columns[5]);
        grid.BeginEdit();
        await Task.Delay(500);
        TextBox? editor = FindFirst<TextBox>(grid, static t => t.IsVisible);
        log.WriteLine($"datagrid: editing TextBox style = {(editor?.Style == grid.TryFindResource("Muzzle.DataGrid.TextBox.Editing") ? "Muzzle" : "other")}");
        Save(window, Path.Combine(directory, "data-grid-editing-text.png"));
        grid.CommitEdit(DataGridEditingUnit.Row, true);
        grid.CurrentCell = new DataGridCellInfo(grid.Items[2], grid.Columns[1]);
        grid.SelectedIndex = 2;
        grid.BeginEdit();
        await Task.Delay(500);
        Save(window, Path.Combine(directory, "data-grid-editing-combo.png"));
        grid.CancelEdit(DataGridEditingUnit.Row);
        grid.SelectedIndex = 0;
        await Task.Delay(300);

        // Real clicks: a check box toggles on the first click, on an unselected row.
        window.Activate();
        await Task.Delay(300);
        grid.SelectedIndex = -1;
        await Task.Delay(200);
        if (grid.Items[3] is Models.CatRecord clickCat
            && (FindAll<DataGridCell>(grid).FirstOrDefault(c => ReferenceEquals(c.DataContext, clickCat) && (c.Column is DataGridCheckBoxColumn)) is DataGridCell checkCell)) {
            bool before = clickCat.IsIndoor;
            CheckBox? box = FindFirst<CheckBox>(checkCell);
            if (await RealClickAsync(window, box ?? (FrameworkElement)checkCell)) {
                await Task.Delay(300);
                log.WriteLine($"datagrid: one real click on an unselected row's check box: {before} -> {clickCat.IsIndoor} (expect {!before})");
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            else {
                log.WriteLine("datagrid: real-click check skipped (window not in foreground)");
            }
        }
        grid.SelectedIndex = 0;
        await Task.Delay(300);

        // Avatars.
        log.WriteLine($"avatar: initials '{Theme.Controls.Avatar.GetInitials("Ada Lovelace")}' '{Theme.Controls.Avatar.GetInitials("mochi")}' '{Theme.Controls.Avatar.GetInitials("  ")}' '{Theme.Controls.Avatar.GetInitials("Jean-Luc Picard")}' (expect AL, MO, '', JP)");
        log.WriteLine($"avatar: colour index stable = {Theme.Controls.Avatar.GetColorIndex("Mochi") == Theme.Controls.Avatar.GetColorIndex(" mochi ")} (expect True)");
        Theme.Controls.Avatar broken = (Theme.Controls.Avatar)page.FindName("BrokenAvatar");
        for (int i = 0; (i < 30) && broken.IsImageVisible; i++) {
            await Task.Delay(100);
        }
        log.WriteLine($"avatar: broken source -> image visible = {broken.IsImageVisible} (expect False), initials = {broken.Initials}");
        ((FrameworkElement)page.FindName("SizeRow")).BringIntoView();
        await Task.Delay(500);
        Save(window, Path.Combine(directory, "data-avatars.png"));

        // Carousel.
        Theme.Controls.Carousel carousel = (Theme.Controls.Carousel)page.FindName("SlideShow");
        carousel.BringIntoView();
        await Task.Delay(500);
        carousel.Next();
        await Task.Delay(180);
        Save(window, Path.Combine(directory, "data-carousel-sliding.png"));
        await Task.Delay(800);
        log.WriteLine($"carousel: next -> {carousel.SelectedIndex} (expect 1)");
        carousel.SelectedIndex = 0;
        await Task.Delay(800);
        carousel.Previous();
        await Task.Delay(900);
        log.WriteLine($"carousel: previous from 0 -> {carousel.SelectedIndex} (expect 3, looping)");
        Save(window, Path.Combine(directory, "data-carousel.png"));

        // ImageViewer.
        Theme.Controls.ImageViewer viewer = (Theme.Controls.ImageViewer)page.FindName("Viewer");
        viewer.BringIntoView();
        await Task.Delay(500);
        viewer.ZoomTo(2.5, new Point(viewer.ActualWidth * 0.4, viewer.ActualHeight * 0.6));
        await Task.Delay(500);
        log.WriteLine($"imageviewer: zoom = {viewer.Zoom:0.##} (expect 2.5), offset = {viewer.Offset.X:0},{viewer.Offset.Y:0}");
        Save(window, Path.Combine(directory, "data-imageviewer-zoomed.png"));
        viewer.ZoomTo(100, default);
        log.WriteLine($"imageviewer: clamp to max = {viewer.Zoom:0.##} (expect 8)");
        viewer.ResetView();
        await Task.Delay(900);
        log.WriteLine($"imageviewer: reset -> zoom {viewer.Zoom:0.##}, offset {viewer.Offset.X:0},{viewer.Offset.Y:0} (expect 1, 0,0)");

        // TreeView.
        TreeView tree = (TreeView)page.FindName("HouseTree");
        tree.BringIntoView();
        await Task.Delay(400);
        if (tree.ItemContainerGenerator.ContainerFromIndex(1) is TreeViewItem kitchen) {
            kitchen.IsExpanded = true;
            kitchen.IsSelected = true;
            tree.Focus();
        }
        await Task.Delay(900);
        log.WriteLine($"treeview: kitchen expanded = {((tree.ItemContainerGenerator.ContainerFromIndex(1) as TreeViewItem)?.IsExpanded)} (expect True)");
        scroller?.ScrollToVerticalOffset(scroller.VerticalOffset + 300);
        await Task.Delay(400);
        Save(window, Path.Combine(directory, "data-tree.png"));
    }

    /// <summary>Every base theme × accent, accent/theme independence, custom accents and live updates of open windows.</summary>
    private static async Task ExerciseAccentsAsync(MainWindow window, string directory, StreamWriter log) {
        static string Resolve(string key) {
            return (Application.Current.TryFindResource(key) is Color color) ? color.ToString() : "missing";
        }

        window.NavigateTo(1);
        await Task.Delay(900);
        Button? primaryButton = FindFirst<Button>(window, static b => (b.Content as string) == "Primary");
        log.WriteLine($"accent: default {Theme.MuzzleThemeManager.CurrentAccent} (expect Coral), Primary {Resolve("Muzzle.Color.Primary")} (expect #FFF497A9), OnPrimary {Resolve("Muzzle.Color.OnPrimary")} (expect #FF4A2333)");

        string[] expectLight = ["#FFF497A9", "#FFA896E6", "#FF7CCBB9", "#FFF5A67E"];
        string[] expectDark = ["#FFF6A2B3", "#FFB8A9F2", "#FF9FD8CB", "#FFF7B492"];
        Theme.MuzzleAccent[] accents = [Theme.MuzzleAccent.Coral, Theme.MuzzleAccent.Lavender, Theme.MuzzleAccent.Mint, Theme.MuzzleAccent.Peach];
        foreach (Theme.MuzzleTheme theme in (Theme.MuzzleTheme[])[Theme.MuzzleTheme.Light, Theme.MuzzleTheme.Dark]) {
            Theme.MuzzleThemeManager.ApplyTheme(theme);
            for (int i = 0; i < accents.Length; i++) {
                Theme.MuzzleThemeManager.ApplyAccent(accents[i]);
                await Task.Delay(500);
                string expected = (theme == Theme.MuzzleTheme.Dark) ? expectDark[i] : expectLight[i];
                string primary = Resolve("Muzzle.Color.Primary");
                string secondary = Resolve("Muzzle.Color.Secondary");
                string tertiary = Resolve("Muzzle.Color.Tertiary");
                string live = (primaryButton?.Background as SolidColorBrush)?.Color.ToString() ?? "none";
                log.WriteLine($"accent: {theme}+{accents[i]} primary {primary} (expect {expected}), live button {live}, secondary {secondary} / tertiary {tertiary} distinct = {(secondary != primary) && (tertiary != primary)}");
                Save(window, Path.Combine(directory, $"accent-{theme}-{accents[i]}.png"));
            }
        }

        // Independence: switching theme keeps the accent, switching accent keeps the theme.
        Theme.MuzzleThemeManager.ApplyAccent(Theme.MuzzleAccent.Lavender);
        Theme.MuzzleThemeManager.ApplyTheme(Theme.MuzzleTheme.Light);
        log.WriteLine($"accent: Dark->Light keeps {Theme.MuzzleThemeManager.CurrentAccent} (expect Lavender), primary {Resolve("Muzzle.Color.Primary")} (expect #FFA896E6)");
        Theme.MuzzleThemeManager.ApplyTheme(Theme.MuzzleTheme.Dark);
        Theme.MuzzleThemeManager.ApplyAccent(Theme.MuzzleAccent.Mint);
        log.WriteLine($"accent: accent change keeps theme {Theme.MuzzleThemeManager.EffectiveTheme} (expect Dark), canvas {Resolve("Muzzle.Color.Canvas")} (expect #FF181622)");

        // Swatches follow the manager.
        await Task.Delay(400);
        string active = string.Join(",", FindAll<Theme.Controls.AccentSwatch>(window).Where(static s => s.IsActive).Select(static s => s.Accent));
        log.WriteLine($"accent: active swatches = {active} (expect Mint)");
        Save(window, Path.Combine(directory, "accent-titlebar-dark-mint.png"));

        // Custom accent, kept across a theme switch.
        Theme.MuzzleThemeManager.ApplyCustomAccent(Color.FromRgb(0x5F, 0xA8, 0xD3));
        await Task.Delay(400);
        log.WriteLine($"accent: custom primary {Resolve("Muzzle.Color.Primary")} (expect #FF5FA8D3), name {Application.Current.TryFindResource("Muzzle.AccentName")}, soft {Resolve("Muzzle.Color.PrimarySoft")}, on {Resolve("Muzzle.Color.OnPrimary")}");
        Theme.MuzzleThemeManager.ApplyTheme(Theme.MuzzleTheme.Light);
        await Task.Delay(400);
        log.WriteLine($"accent: custom after Light {Theme.MuzzleThemeManager.CurrentAccent} primary {Resolve("Muzzle.Color.Primary")} (expect Custom #FF5FA8D3), soft {Resolve("Muzzle.Color.PrimarySoft")}, text {Resolve("Muzzle.Color.PrimaryText")}");
        Save(window, Path.Combine(directory, "accent-Light-Custom.png"));

        // Back to the defaults for the passes that follow.
        Theme.MuzzleThemeManager.ApplyAccent(Theme.MuzzleAccent.Coral);
        await Task.Delay(400);
        log.WriteLine($"accent: reset {Theme.MuzzleThemeManager.CurrentTheme}+{Theme.MuzzleThemeManager.CurrentAccent}, Primary {Resolve("Muzzle.Color.Primary")} (expect #FFF497A9), Secondary {Resolve("Muzzle.Color.Secondary")} (expect #FFB8A9E6)");
    }

    private static async Task ExerciseUtilitiesAsync(MainWindow window, string directory, StreamWriter log) {
        log.WriteLine($"utilities: App.Resources is {Application.Current.Resources.GetType().Name} (expect MuzzleThemeResourceDictionary), theme {Theme.MuzzleThemeManager.EffectiveTheme}+{Theme.MuzzleThemeManager.CurrentAccent}");
        window.NavigateTo(window.IndexOf("Utilities & behaviors"));
        await Task.Delay(1100);
        if ((window.FindName("PageHost") as ContentControl)?.Content is not Pages.UtilitiesPage page) {
            log.WriteLine("utilities: page not found");
            return;
        }
        Save(window, Path.Combine(directory, "utilities-top.png"));
        log.WriteLine($"icons: {page.VisibleIconCount} listed (expect >= 120), Search geometry found = {Theme.Controls.MuzzleIcon.GetGeometry(Theme.Controls.MuzzleIconKind.Search) is not null}");
        log.WriteLine($"converters: initials '{Theme.Converters.StringToInitialsConverter.Instance.Convert("Grace Hopper", typeof(string), null, System.Globalization.CultureInfo.InvariantCulture)}' (expect GH), bool invert {Theme.Converters.BoolToVisibilityConverter.Instance.Convert(true, typeof(Visibility), "Invert", System.Globalization.CultureInfo.InvariantCulture)} (expect Collapsed), null {Theme.Converters.NullToVisibilityConverter.Instance.Convert(null, typeof(Visibility), null, System.Globalization.CultureInfo.InvariantCulture)} (expect Collapsed), tint {((SolidColorBrush)Theme.Converters.ColorToSoftTintConverter.Instance.Convert(Color.FromRgb(0xF4, 0x97, 0xA9), typeof(Brush), 0.2, System.Globalization.CultureInfo.InvariantCulture)).Color} (expect #33F497A9)");

        Border clipped = (Border)page.FindName("ClippedFrame");
        log.WriteLine($"cliphelper: child clip = {(clipped.Child as UIElement)?.Clip?.GetType().Name ?? "none"} (expect RectangleGeometry), bounds {(clipped.Child as UIElement)?.Clip?.Bounds}");

        ScrollViewer smooth = (ScrollViewer)page.FindName("SmoothScroller");
        smooth.BringIntoView();
        await Task.Delay(500);
        MouseWheelEventArgs wheel = new(Mouse.PrimaryDevice, Environment.TickCount, -120) { RoutedEvent = UIElement.PreviewMouseWheelEvent, Source = smooth };
        smooth.RaiseEvent(wheel);
        await Task.Delay(60);
        double mid = smooth.VerticalOffset;
        await Task.Delay(600);
        log.WriteLine($"scrollassist: wheel handled = {wheel.Handled} (expect True), offset mid-glide {mid:0} then {smooth.VerticalOffset:0} (expect 0 < mid < 96, then 96)");

        // Focus trap + Esc.
        Theme.Controls.ToggleSwitch panelSwitch = (Theme.Controls.ToggleSwitch)page.FindName("PanelSwitch");
        Border panel = (Border)page.FindName("TrapPanel");
        panelSwitch.BringIntoView();
        panelSwitch.IsChecked = true;
        await Task.Delay(700);
        bool inside = panel.IsKeyboardFocusWithin;
        bool stayed = true;
        for (int i = 0; i < 7; i++) {
            if (Keyboard.FocusedElement is UIElement focused) {
                focused.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                stayed &= panel.IsKeyboardFocusWithin;
            }
        }
        Save(window, Path.Combine(directory, "utilities-focus-trap.png"));
        if (Keyboard.FocusedElement is UIElement target && (PresentationSource.FromVisual(panel) is PresentationSource source)) {
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        }
        await Task.Delay(300);
        log.WriteLine($"focustrap: focus inside on open = {inside} (expect True), stayed inside over 7 tabs = {stayed} (expect True); Esc closed it = {panelSwitch.IsChecked == false} (expect True)");

        ((FrameworkElement)page.FindName("IconGrid")).BringIntoView();
        await Task.Delay(600);
        Save(window, Path.Combine(directory, "utilities-icons.png"));
    }

    private static async Task ExerciseDesktopAsync(MainWindow window, string directory, StreamWriter log) {
        window.NavigateTo(window.IndexOf("Desktop controls"));
        await Task.Delay(1100);
        if ((window.FindName("PageHost") as ContentControl)?.Content is not Pages.DesktopControlsPage page) {
            log.WriteLine("desktop: page not found");
            return;
        }
        Save(window, Path.Combine(directory, "desktop-top.png"));
        Theme.Controls.CommandPalette palette = window.Palette;

        // Command palette through real Ctrl+K, fuzzy filtering and Enter.
        if (await RealKeysAsync(window, 0x11, 0x4B)) {
            await Task.Delay(600);
            log.WriteLine($"palette: real Ctrl+K open = {palette.IsOpen} (expect True), {palette.Results.Count} items, search focused = {palette.IsKeyboardFocusWithin}");
        }
        else {
            palette.Open();
            await Task.Delay(600);
            log.WriteLine("palette: Ctrl+K skipped (window not in foreground), opened in code");
        }
        Save(window, Path.Combine(directory, "palette-open.png"));
        palette.SearchText = "drk thm";
        await Task.Delay(300);
        log.WriteLine($"palette: 'drk thm' -> first '{(palette.Results.Count > 0 ? palette.Results[0] : "none")}' (expect Dark theme), {palette.Results.Count} matches");
        Save(window, Path.Combine(directory, "palette-search.png"));
        palette.SearchText = "zzzqqq";
        await Task.Delay(300);
        log.WriteLine($"palette: nonsense -> has results {palette.HasResults} (expect False)");
        Save(window, Path.Combine(directory, "palette-empty.png"));
        palette.SearchText = "data media";
        await Task.Delay(200);
        if (palette.Template.FindName("PART_SearchBox", palette) is TextBox search && PresentationSource.FromVisual(search) is PresentationSource source) {
            search.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        }
        await Task.Delay(1000);
        log.WriteLine($"palette: Enter on 'data media' -> open {palette.IsOpen} (expect False), page {((window.FindName("PageHost") as ContentControl)?.Content?.GetType().Name)} (expect DataMediaPage)");
        window.NavigateTo(window.IndexOf("Desktop controls"));
        await Task.Delay(1100);
        page = (Pages.DesktopControlsPage)((ContentControl)window.FindName("PageHost")).Content;
        palette.Open();
        await Task.Delay(400);
        bool escaped = await RealKeysAsync(window, 0x1B);
        await Task.Delay(400);
        log.WriteLine($"palette: real Esc ({(escaped ? "sent" : "skipped")}) -> open {palette.IsOpen} (expect False)");
        palette.Close();

        // Split button: real click on the chevron opens the menu; a real click on an item runs it and closes it.
        Theme.Controls.SplitButton split = (Theme.Controls.SplitButton)page.FindName("SaveSplit");
        split.BringIntoView();
        await Task.Delay(500);
        Save(window, Path.Combine(directory, "splitbutton-before.png"));
        if (split.Template.FindName("PART_ToggleButton", split) is FrameworkElement chevron && await RealClickAsync(window, chevron)) {
            await Task.Delay(700);
            log.WriteLine($"splitbutton: real chevron click -> open {split.IsDropDownOpen} (expect True)");
            if (split.Template.FindName("PART_MenuCard", split) is FrameworkElement card) {
                SaveVisual(card, Path.Combine(directory, "splitbutton-menu.png"));
                MenuItem? saveAs = FindFirst<MenuItem>(card, static m => (m.Header as string) == "Save a copy");
                if (saveAs is not null && await RealClickAsync(window, saveAs)) {
                    await Task.Delay(500);
                    log.WriteLine($"splitbutton: real item click -> open {split.IsDropDownOpen} (expect False), log '{((TextBlock)page.FindName("ActionLog")).Text}' (expect Save a copy)");
                }
            }
            if (split.Template.FindName("PART_PrimaryButton", split) is FrameworkElement primary && await RealClickAsync(window, primary)) {
                await Task.Delay(300);
                log.WriteLine($"splitbutton: primary click -> log '{((TextBlock)page.FindName("ActionLog")).Text}' (expect Save (primary segment)), open {split.IsDropDownOpen} (expect False)");
            }
        }
        else {
            log.WriteLine("splitbutton: real-click check skipped");
        }
        split.Close();

        // Menu: a real click drops the File menu; its Export row opens a nested submenu to the side.
        MenuItem fileMenu = (MenuItem)page.FindName("DemoFileMenu");
        MenuItem exportMenu = (MenuItem)page.FindName("DemoExportMenu");
        // Scroll the whole card in, so the menus drop inside the window that the screen capture covers.
        (((FrameworkElement)page.FindName("DemoMenu")).Parent as FrameworkElement)?.BringIntoView();
        await Task.Delay(500);
        if (await RealClickAsync(window, fileMenu)) {
            await Task.Delay(700);
            log.WriteLine($"menu: real click on File -> open {fileMenu.IsSubmenuOpen} (expect True), role {fileMenu.Role} (expect TopLevelHeader)");
            exportMenu.IsSubmenuOpen = true;
            await Task.Delay(700);
            log.WriteLine($"menu: Export submenu -> open {exportMenu.IsSubmenuOpen} (expect True), role {exportMenu.Role} (expect SubmenuHeader)");
            SaveScreen(window, Path.Combine(directory, "menu-open.png"));
            exportMenu.IsSubmenuOpen = false;
            bool menuEscaped = await RealKeysAsync(window, 0x1B);
            await Task.Delay(400);
            log.WriteLine($"menu: real Esc ({(menuEscaped ? "sent" : "skipped")}) -> open {fileMenu.IsSubmenuOpen} (expect False)");
        }
        else {
            log.WriteLine("menu: real-click check skipped");
        }
        fileMenu.IsSubmenuOpen = false;

        // Range slider coercion and the password meter.
        Theme.Controls.RangeSlider range = (Theme.Controls.RangeSlider)page.FindName("PriceRange");
        range.LowerValue = 95;
        log.WriteLine($"rangeslider: lower 95 -> {range.LowerValue} (expect 60: upper 70 - minimum range 10), upper {range.UpperValue}");
        range.LowerValue = 15;
        PasswordBox secret = (PasswordBox)page.FindName("SecretBox");
        Theme.Controls.PasswordStrengthMeter meter = (Theme.Controls.PasswordStrengthMeter)page.FindName("StrengthMeter");
        string scores = string.Empty;
        foreach (string password in (string[])["password", "mochi2024", "Sunny-Window-7", "purr!Biscuit#Nap42"]) {
            secret.Password = password;
            scores += $"{password}={meter.StrengthScore}/{meter.StrengthLabel} ";
        }
        log.WriteLine($"password: {scores.Trim()} (expect rising 0 → 4)");
        meter.BringIntoView();
        await Task.Delay(800);
        Save(window, Path.Combine(directory, "desktop-range-password.png"));

        ((FrameworkElement)page.FindName("SampleCode")).BringIntoView();
        await Task.Delay(500);
        Save(window, Path.Combine(directory, "desktop-code.png"));

        // System tray icon: add, show its menu card, remove.
        Theme.SystemTrayIcon tray = (Theme.SystemTrayIcon)page.FindName("Tray");
        tray.IsIconVisible = true;
        await Task.Delay(400);
        bool created = tray.IsCreated;
        tray.ShowMenu();
        await Task.Delay(700);
        if (FindOpenPopupChild(tray) is FrameworkElement trayCard) {
            SaveVisual(trayCard, Path.Combine(directory, "tray-menu.png"));
        }
        tray.IsIconVisible = false;
        await Task.Delay(200);
        log.WriteLine($"tray: created {created} (expect True), removed {!tray.IsCreated} (expect True)");

        // Backdrop: switch to Mica and capture the real screen pixels, then switch back.
        ((FrameworkElement)page.FindName("BackdropNone")).BringIntoView();
        await Task.Delay(300);
        Theme.BackdropHelper.SetBackdropType(window, Theme.BackdropType.Mica);
        await Task.Delay(700);
        log.WriteLine($"backdrop: supported {Theme.BackdropHelper.IsSupported}, Mica active {Theme.BackdropHelper.GetIsBackdropActive(window)}, background {(window.Background as SolidColorBrush)?.Color}");
        SaveScreen(window, Path.Combine(directory, "backdrop-mica-screen.png"));
        Theme.BackdropHelper.SetBackdropType(window, Theme.BackdropType.None);
        await Task.Delay(400);
        log.WriteLine($"backdrop: off -> active {Theme.BackdropHelper.GetIsBackdropActive(window)} (expect False), background {(window.Background as SolidColorBrush)?.Color} (expect #FFFAF8F6)");

        // Window state saver against a temporary folder.
        string previousFolder = Theme.WindowStateSaver.StorageFolder;
        Theme.WindowStateSaver.StorageFolder = Path.Combine(directory, "window-state");
        Theme.WindowStateSaver.IsEnabled = true;
        try {
            Window probe = new() { Width = 420, Height = 300, Left = 120, Top = 140, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false, ShowInTaskbar = false, Title = "probe" };
            Theme.WindowStateSaver.SetKey(probe, "Probe");
            probe.Show();
            await Task.Delay(300);
            probe.Close();
            Window restored = new() { Width = 200, Height = 200, ShowActivated = false, ShowInTaskbar = false };
            Theme.WindowStateSaver.SetKey(restored, "Probe");
            bool applied = Theme.WindowStateSaver.Restore(restored);
            log.WriteLine($"windowstate: saved+restored {applied} -> {restored.Left:0},{restored.Top:0} {restored.Width:0}x{restored.Height:0} (expect 120,140 420x300)");
            string json = File.ReadAllText(Theme.WindowStateSaver.FilePath).Replace("\"Left\": 120", "\"Left\": -40000");
            File.WriteAllText(Theme.WindowStateSaver.FilePath, json);
            Window offscreen = new() { Width = 200, Height = 200, ShowActivated = false, ShowInTaskbar = false };
            Theme.WindowStateSaver.SetKey(offscreen, "Probe");
            Theme.WindowStateSaver.Restore(offscreen);
            Rect area = SystemParameters.WorkArea;
            log.WriteLine($"windowstate: off-screen saved left -40000 -> restored left {offscreen.Left:0} (expect on screen, within {area.Left:0}..{area.Right:0})");
        }
        finally {
            Theme.WindowStateSaver.IsEnabled = false;
            Theme.WindowStateSaver.StorageFolder = previousFolder;
        }
    }

    private static FrameworkElement? FindOpenPopupChild(DependencyObject owner) {
        foreach (PresentationSource source in PresentationSource.CurrentSources) {
            if ((source.RootVisual is FrameworkElement root) && (root.Parent is Popup { IsOpen: true } popup) && (popup.Child is FrameworkElement child)) {
                if (FindFirst<Theme.Controls.SoftBorder>(child) is FrameworkElement card) {
                    return card;
                }
            }
        }
        return null;
    }

    private static void MethodInfoInvoke(ButtonBase? button) {
        typeof(ButtonBase).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(button, null);
    }

    /// <summary>Switches to Dark through the title-bar toggle, re-captures pages and overlays, then cycles back to Light.</summary>
    private static async Task ExerciseDarkAsync(MainWindow window, string directory, StreamWriter log) {
        window.NavigateTo(0);
        await Task.Delay(700);
        Theme.Controls.ThemeToggleButton toggle = (Theme.Controls.ThemeToggleButton)window.FindName("ThemeToggle");
        typeof(ButtonBase).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(toggle, null);
        await Task.Delay(150);
        Save(window, Path.Combine(directory, "dark-crossfade.png"));
        await Task.Delay(900);
        Color canvas = (Application.Current.TryFindResource("Muzzle.Color.Canvas") is Color c) ? c : default;
        log.WriteLine($"theme: toggle -> {Theme.MuzzleThemeManager.CurrentTheme} / effective {Theme.MuzzleThemeManager.EffectiveTheme} (expect Dark), canvas = {canvas} (expect #FF181622), button theme = {toggle.Theme}");
        log.WriteLine($"theme: existing window updated, background = {(window.Background as SolidColorBrush)?.Color} (expect #FF181622), foreground = {(window.Foreground as SolidColorBrush)?.Color} (expect #FFF2EFF7)");
        Theme.MuzzleThemeManager.ApplyTheme(Theme.MuzzleTheme.Dark);
        log.WriteLine($"theme: re-apply Dark is a no-op, palette = {Theme.MuzzleThemeManager.DetectAppliedTheme()}");
        System.Windows.Media.Effects.Effect? shadow = Theme.MuzzleShadows.Resolve(Theme.ShadowDepth.Raised);
        log.WriteLine($"theme: live Raised shadow opacity = {(shadow as System.Windows.Media.Effects.DropShadowEffect)?.Opacity} (expect 0.45)");

        for (int i = 0; i < window.PageCount; i++) {
            window.NavigateTo(i);
            await Task.Delay(1100);
            Save(window, Path.Combine(directory, $"dark-page-{i}.png"));
        }
        log.WriteLine("dark: captured all pages");

        window.NavigateTo(8);
        await Task.Delay(600);
        Theme.Controls.Growl.Info("Biscuit is napping in the moonlight.", "Night mode");
        Theme.Controls.Growl.Success("Dinner is served.", "Yum");
        Theme.Controls.Growl.Warning("The treat jar is getting low.", "Heads up");
        Theme.Controls.Growl.Error("Someone knocked a glass off the table.", "Oops");
        await Task.Delay(1000);
        Save(window, Path.Combine(directory, "dark-growl.png"));
        Theme.Controls.Growl.ClearAll();
        await Task.Delay(600);
        Task<Dialogs.AdoptionRequest?> formTask = Theme.Controls.Dialog.ShowAsync<Dialogs.AdoptionRequest>(new Dialogs.AdoptionFormDialog());
        await Task.Delay(900);
        Save(window, Path.Combine(directory, "dark-dialog.png"));
        FindFirst<Theme.Controls.Dialog>(window)?.CloseWithResult(null);
        _ = await formTask;
        Theme.Controls.Drawer drawer = Theme.Controls.Drawer.Show(new TextBlock { Text = "Right drawer content", TextWrapping = TextWrapping.Wrap }, Dock.Right, "Night drawer");
        await Task.Delay(900);
        Save(window, Path.Combine(directory, "dark-drawer.png"));
        drawer.IsOpen = false;
        await Task.Delay(500);

        window.NavigateTo(10);
        await Task.Delay(900);
        if ((window.FindName("PageHost") as ContentControl)?.Content is Pages.PickersPage pickers
            && FindFirst<Theme.Controls.ColorPickerDropDown>(pickers) is Theme.Controls.ColorPickerDropDown dropDown) {
            dropDown.BringIntoView();
            await Task.Delay(300);
            dropDown.IsDropDownOpen = true;
            await Task.Delay(900);
            if (dropDown.Template.FindName("PART_Popup", dropDown) is Popup popup && (popup.Child is FrameworkElement child)) {
                SaveVisual(child, Path.Combine(directory, "dark-colordropdown.png"));
            }
            dropDown.IsDropDownOpen = false;
        }
        if (FindFirst<DatePicker>(window) is DatePicker date) {
            date.BringIntoView();
            await Task.Delay(300);
            date.IsDropDownOpen = true;
            await Task.Delay(900);
            if (date.Template.FindName("PART_Popup", date) is Popup popup && (popup.Child is FrameworkElement child)) {
                SaveVisual(child, Path.Combine(directory, "dark-calendar.png"));
            }
            date.IsDropDownOpen = false;
        }

        window.NavigateTo(11);
        await Task.Delay(900);
        if ((window.FindName("PageHost") as ContentControl)?.Content is Pages.FeedbackPage feedback) {
            Theme.Controls.Popover popover = (Theme.Controls.Popover)feedback.FindName("ProfilePopover");
            popover.BringIntoView();
            await Task.Delay(300);
            popover.IsOpen = true;
            await Task.Delay(900);
            if (popover.Template.FindName("PART_Popup", popover) is Popup pop && (pop.Child is FrameworkElement bubble)) {
                SaveVisual(bubble, Path.Combine(directory, "dark-popover.png"));
            }
            popover.IsOpen = false;
        }

        // Dark -> System -> Light through the toggle cycle.
        toggle.Theme = Theme.Controls.ThemeToggleButton.Next(toggle.Theme);
        await Task.Delay(500);
        log.WriteLine($"theme: toggle -> {Theme.MuzzleThemeManager.CurrentTheme}, effective {Theme.MuzzleThemeManager.EffectiveTheme} (system dark = {Theme.MuzzleThemeManager.IsSystemDark()})");
        toggle.Theme = Theme.Controls.ThemeToggleButton.Next(toggle.Theme);
        await Task.Delay(700);
        canvas = (Application.Current.TryFindResource("Muzzle.Color.Canvas") is Color back) ? back : default;
        log.WriteLine($"theme: toggle -> {Theme.MuzzleThemeManager.CurrentTheme}, canvas = {canvas} (expect #FFFAF8F6)");
        window.NavigateTo(0);
        await Task.Delay(1100);
        Save(window, Path.Combine(directory, "light-again.png"));
    }

    private static void SaveVisual(FrameworkElement element, string path) {
        DpiScale dpi = VisualTreeHelper.GetDpi(element);
        int width = (int)Math.Ceiling(Math.Max(1, element.ActualWidth) * dpi.DpiScaleX);
        int height = (int)Math.Ceiling(Math.Max(1, element.ActualHeight) * dpi.DpiScaleY);
        RenderTargetBitmap bitmap = new(width, height, 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
        DrawingVisual backdrop = new();
        using (DrawingContext context = backdrop.RenderOpen()) {
            Brush canvas = (Application.Current.TryFindResource("Muzzle.Brush.Canvas") as Brush) ?? Brushes.White;
            context.DrawRectangle(canvas, null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        }
        bitmap.Render(backdrop);
        bitmap.Render(element);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static T? FindFirst<T>(DependencyObject root, Func<T, bool>? predicate = null) where T : DependencyObject {
        foreach (T match in FindAll<T>(root)) {
            if ((predicate is null) || predicate(match)) {
                return match;
            }
        }
        return null;
    }

    private static System.Collections.Generic.IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) {
                yield return match;
            }
            foreach (T nested in FindAll<T>(child)) {
                yield return nested;
            }
        }
    }

    private static void Save(Window window, string path) {
        DpiScale dpi = VisualTreeHelper.GetDpi(window);
        int width = (int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX);
        int height = (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY);
        RenderTargetBitmap bitmap = new(width, height, 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY, PixelFormats.Pbgra32);
        bitmap.Render(window);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void ProbeHitTest(MainWindow window, StreamWriter log) {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (window.Template.FindName("PART_MaximizeButton", window) is FrameworkElement maximize) {
            log.WriteLine("hit maximize = " + HitTest(handle, maximize, new Point(maximize.ActualWidth / 2, maximize.ActualHeight / 2)) + " (expect 9 = HTMAXBUTTON)");
        }
        if (window.Template.FindName("PART_CloseButton", window) is FrameworkElement close) {
            log.WriteLine("hit close = " + HitTest(handle, close, new Point(close.ActualWidth / 2, close.ActualHeight / 2)) + " (expect 1 = HTCLIENT)");
        }
        if (window.FindName("SearchBox") is Control search) {
            log.WriteLine("hit search = " + HitTest(handle, search, new Point(10, search.ActualHeight / 2)) + " (expect 1 = HTCLIENT)");
        }
        log.WriteLine("hit title gap = " + HitTest(handle, window, new Point(260, 26)) + " (expect 2 = HTCAPTION)");
    }

    private static long HitTest(IntPtr handle, Visual relativeTo, Point point) {
        Point screen = relativeTo.PointToScreen(point);
        long lParam = ((long)(short)screen.Y << 16) | ((long)(short)screen.X & 0xFFFF);
        return SendMessage(handle, WmNcHitTest, IntPtr.Zero, new IntPtr(lParam)).ToInt64();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Real left click at the centre of <paramref name="target"/> (some behaviour, like the DataGrid check box toggle,
    /// hit-tests the actual cursor). Only clicks when the gallery window is in the foreground and owns the point,
    /// and puts the cursor back afterwards. Returns false (and does nothing) otherwise.
    /// </summary>
    private static async Task<bool> RealClickAsync(Window window, FrameworkElement target, Point? local = null) {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (GetForegroundWindow() != handle) {
            // Windows only lets a process take the foreground right after input; a bare Alt tap counts.
            keybd_event(0x12, 0, 0, IntPtr.Zero);
            keybd_event(0x12, 0, 0x0002, IntPtr.Zero);
            _ = SetForegroundWindow(handle);
            await Task.Delay(250);
        }
        Point screen = target.PointToScreen(local ?? new Point(target.ActualWidth / 2, target.ActualHeight / 2));
        NativePoint point = new() { X = (int)Math.Round(screen.X), Y = (int)Math.Round(screen.Y) };
        if ((GetForegroundWindow() != handle) || !IsOwnWindow(WindowFromPoint(point))) {
            return false;
        }
        _ = GetCursorPos(out NativePoint original);
        _ = SetCursorPos(point.X, point.Y);
        await Task.Delay(60);
        mouse_event(0x0002, 0, 0, 0, IntPtr.Zero);
        await Task.Delay(90);
        mouse_event(0x0004, 0, 0, 0, IntPtr.Zero);
        await Task.Delay(60);
        _ = SetCursorPos(original.X, original.Y);
        return true;
    }

    /// <summary>True when the window under a point belongs to this process (the gallery or one of its popups).</summary>
    private static bool IsOwnWindow(IntPtr hwnd) {
        if (hwnd == IntPtr.Zero) {
            return false;
        }
        _ = GetWindowThreadProcessId(hwnd, out uint processId);
        return processId == (uint)Environment.ProcessId;
    }

    /// <summary>Real key presses (only while the gallery is in the foreground).</summary>
    private static async Task<bool> RealKeysAsync(Window window, params byte[] keys) {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (GetForegroundWindow() != handle) {
            keybd_event(0x12, 0, 0, IntPtr.Zero);
            keybd_event(0x12, 0, 0x0002, IntPtr.Zero);
            _ = SetForegroundWindow(handle);
            await Task.Delay(250);
        }
        if (GetForegroundWindow() != handle) {
            return false;
        }
        foreach (byte key in keys) {
            keybd_event(key, 0, 0, IntPtr.Zero);
            await Task.Delay(30);
        }
        for (int i = keys.Length - 1; i >= 0; i--) {
            keybd_event(keys[i], 0, 0x0002, IntPtr.Zero);
            await Task.Delay(30);
        }
        return true;
    }

    /// <summary>Copies what is actually on screen (DWM backdrops included) inside a window's bounds.</summary>
    private static void SaveScreen(Window window, string path) {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (!GetWindowRect(handle, out NativeRectangle rect)) {
            return;
        }
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        IntPtr screen = GetDC(IntPtr.Zero);
        IntPtr memory = CreateCompatibleDC(screen);
        IntPtr bitmap = CreateCompatibleBitmap(screen, width, height);
        IntPtr previous = SelectObject(memory, bitmap);
        _ = BitBlt(memory, 0, 0, width, height, screen, rect.Left, rect.Top, 0x00CC0020);
        _ = SelectObject(memory, previous);
        BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        _ = DeleteObject(bitmap);
        _ = DeleteDC(memory);
        _ = ReleaseDC(IntPtr.Zero, screen);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRectangle rect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr handle);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr hdc);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte key, byte scan, uint flags, IntPtr extraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, IntPtr extraInfo);
}