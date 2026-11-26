namespace Muzzle.Gallery;

/// <summary>Copy-pasteable XAML shown under each gallery section. Assumes xmlns:mz="http://schemas.muzzle.dev/wpf".</summary>
public static class Snippets {
    public const string Setup = """
        <!-- App.xaml -->
        <Application.Resources>
            <ResourceDictionary>
                <ResourceDictionary.MergedDictionaries>
                    <ResourceDictionary Source="pack://application:,,,/Muzzle.Theme;component/Themes/Muzzle.xaml" />
                </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
        </Application.Resources>

        <!-- Any view -->
        xmlns:mz="http://schemas.muzzle.dev/wpf"
        """;

    public const string Buttons = """
        <Button Content="Primary" Style="{StaticResource Muzzle.Button.Primary}" />
        <Button Content="Secondary" Style="{StaticResource Muzzle.Button.Secondary}" />
        <Button Content="Mint" Style="{StaticResource Muzzle.Button.Mint}" />
        <Button Content="Neutral" />
        <Button Content="Ghost" Style="{StaticResource Muzzle.Button.Ghost}" />
        <Button Content="Pill" Style="{StaticResource Muzzle.Button.Pill}" />

        <!-- Tune any button without touching its template -->
        <Button Content="Extra squishy"
                Style="{StaticResource Muzzle.Button.Primary}"
                mz:MuzzleAssist.CornerRadius="18"
                mz:MuzzleAssist.ShadowDepth="Floating"
                mz:MuzzleAssist.JellyIntensity="2.5" />
        """;

    public const string Inputs = """
        <TextBox mz:MuzzleAssist.Placeholder="Your cat's name" />
        <PasswordBox mz:MuzzleAssist.Placeholder="Secret treat stash" />
        <TextBox AcceptsReturn="True" TextWrapping="Wrap" Height="96"
                 VerticalContentAlignment="Top" VerticalScrollBarVisibility="Auto" />

        <ComboBox SelectedIndex="0">
            <ComboBoxItem Content="Tuna" />
            <ComboBoxItem Content="Salmon" />
        </ComboBox>
        <ComboBox IsEditable="True" mz:MuzzleAssist.Placeholder="Type or pick" />
        """;

    public const string Selection = """
        <CheckBox Content="Fed the cat" IsChecked="True" />
        <CheckBox Content="Maybe napping" IsThreeState="True" IsChecked="{x:Null}" />

        <RadioButton GroupName="Size" Content="Kitten" />
        <RadioButton GroupName="Size" Content="Loaf" IsChecked="True" />

        <mz:ToggleSwitch Content="Zoomies mode" IsChecked="True" />
        """;

    public const string Range = """
        <Slider Minimum="0" Maximum="100" Value="40" />
        <Slider TickPlacement="BottomRight" TickFrequency="10" IsSnapToTickEnabled="True" />
        <Slider Orientation="Vertical" Height="140" />

        <!-- The fill glides to new values automatically -->
        <ProgressBar Value="{Binding Value, ElementName=MySlider}" />
        <ProgressBar Style="{StaticResource Muzzle.ProgressBar.Mint}" IsIndeterminate="True" />
        """;

    public const string Containers = """
        <mz:Card mz:MuzzleAssist.ShadowDepth="Raised" mz:MuzzleAssist.CornerRadius="20">
            <TextBlock Text="Cozy card" Style="{StaticResource Muzzle.Text.Subtitle}" />
        </mz:Card>

        <!-- Plain Border with the same look -->
        <Border Style="{StaticResource Muzzle.Surface}" mz:MuzzleAssist.ShadowDepth="Floating" />

        <Expander Header="Snack schedule" IsExpanded="True">
            <TextBlock Text="Breakfast, second breakfast, elevenses…" />
        </Expander>

        <TabControl>
            <TabItem Header="Naps" />
            <TabItem Header="Snacks" />
        </TabControl>
        """;

    public const string PropertyGrid = """
        <mz:PropertyGrid SelectedObject="{Binding Settings}"
                         ShowSearchBox="True"
                         ShowCategories="True" />

        // Annotate your model with System.ComponentModel attributes:
        [Category("Shape")]
        [DisplayName("Corner radius")]
        [Description("Rounds the card corners.")]
        [Range(0, 40)]                // DataAnnotations: becomes a slider
        public double CornerRadius { get; set; }

        [Browsable(false)]            // hidden from the grid
        public string Secret { get; set; }
        """;

    public const string Jelly = """
        <!-- Make anything jelly: squish on press, grow on hover -->
        <Border mz:MuzzleAssist.Jelly="Hover, Press" Style="{StaticResource Muzzle.Surface}" />

        <!-- Turn the wobble up (or off with 0) for a whole subtree; it inherits -->
        <StackPanel mz:MuzzleAssist.JellyIntensity="2">...</StackPanel>

        <!-- Reusable storyboards and easings -->
        <Border RenderTransformOrigin="0.5,0.5">
            <Border.RenderTransform><ScaleTransform /></Border.RenderTransform>
            <Border.Triggers>
                <EventTrigger RoutedEvent="MouseLeftButtonUp">
                    <BeginStoryboard Storyboard="{StaticResource Muzzle.Storyboard.Boop}" />
                </EventTrigger>
            </Border.Triggers>
        </Border>

        // Code-behind
        Jelly.Wobble(myElement);
        """;

    public const string Window = """
        <mz:MuzzleWindow x:Class="MyApp.MainWindow"
                         xmlns:mz="http://schemas.muzzle.dev/wpf"
                         Title="My cozy app"
                         NonClientAreaHeight="48"
                         ShowTitle="True">
            <mz:MuzzleWindow.NonClientAreaContent>
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <TextBox Width="220" mz:MuzzleAssist.Placeholder="Search…" />
                    <Button Content="Share" Style="{StaticResource Muzzle.Button.Ghost}" />
                </StackPanel>
            </mz:MuzzleWindow.NonClientAreaContent>

            <!-- window content -->
        </mz:MuzzleWindow>

        // MainWindow.xaml.cs
        public partial class MainWindow : MuzzleWindow { ... }
        """;

    public const string Growl = """
        <!-- Host toasts in a window: a GrowlContainer is added to this Grid's top-right corner -->
        <Grid mz:Growl.GrowlParent="True"> ... </Grid>

        <!-- Or target a specific region with a token -->
        <Grid mz:Growl.GrowlParent="True" mz:Growl.Token="Sidebar" />

        // C#
        Growl.Info("Biscuit is napping in the sunbeam.");
        Growl.Success("Dinner is served.", title: "Yum");
        Growl.Warning("The treat jar is getting low.", waitTime: TimeSpan.FromSeconds(10));
        Growl.Error("Someone knocked a glass off the table.", token: "Sidebar");
        Growl.Ask("Open a new bag of treats?", confirmed =>
            Growl.Info(confirmed ? "Crinkle crinkle!" : "Maybe later."));
        Growl.Show(new GrowlInfo { Message = "Desktop corner", IsGlobal = true, StaysOpen = true });
        Growl.Clear();          // this window's toasts
        Growl.ClearAll();       // everywhere
        """;

    public const string Dialog = """
        // Show any content (UserControl or ViewModel + DataTemplate) over the active window
        Dialog dialog = Dialog.Show(new AdoptionFormDialog());
        Dialog.Show<AdoptionFormDialog>();

        // Await a typed result
        AdoptionRequest? request = await Dialog.ShowAsync<AdoptionRequest>(new AdoptionFormDialog());

        // Inside the dialog content
        Dialog.GetDialog(this)?.CloseWithResult(request);
        <Button Content="Cancel" Command="{x:Static mz:Dialog.CloseCommand}" />

        // Or implement IDialogResultable<T> on the content / ViewModel
        public partial class ConfirmDialog : UserControl, IDialogResultable<bool> {
            public bool Result { get; set; }
            public Action? CloseAction { get; set; }   // wired by Dialog.ShowAsync<bool>
        }
        bool ok = await Dialog.ShowAsync<bool>(new ConfirmDialog("Trim claws?", "..."));

        // Options and scoping
        Dialog quick = Dialog.Show(content);
        quick.CloseOnBackdropClick = true;
        <mz:DialogContainer mz:Dialog.Token="Editor"> ... </mz:DialogContainer>
        Dialog.Show(content, token: "Editor");
        Dialog.Close("Editor");
        """;

    public const string Drawer = """
        <!-- Declarative: drawers slide out from the edges of a DrawerContainer (or any Grid) -->
        <mz:DrawerContainer>
            <ToggleSwitch IsChecked="{Binding IsOpen, ElementName=Settings}" Content="Settings" />
            <mz:Drawer x:Name="Settings" Dock="Right" DrawerSize="300"
                       Header="Settings" ShowMask="True" MaskCanClose="True"
                       CloseButtonVisibility="Visible">
                <StackPanel> ... </StackPanel>
            </mz:Drawer>
        </mz:DrawerContainer>

        // Programmatic: over the active window's client area
        Drawer drawer = Drawer.Show(new SettingsPanel(), Dock.Left, header: "Settings");
        drawer.Closed += (_, _) => Growl.Info("Saved!");
        drawer.IsOpen = false;   // slides back out and removes itself
        """;

    public const string Pickers = """
        <mz:NumericUpDown Minimum="0" Maximum="15" Increment="0.25" DecimalPlaces="2"
                          ValueFormat="0.00 'kg'" Value="{Binding Weight}" />

        <mz:TagInput Tags="{Binding Traits}" Delimiter=",;" AllowDuplicates="False"
                     MaxTags="10" Placeholder="Describe your cat…" />

        <mz:SearchBar SuggestionsSource="{Binding CatNames}" SearchCommand="{Binding SearchCommand}"
                      DebounceDelay="0:0:0.3" mz:MuzzleAssist.Placeholder="Search…"
                      SearchRequested="OnSearchRequested" />

        <mz:ColorPicker SelectedColor="{Binding CollarColor}" ShowAlpha="True" />
        <!-- or SelectedBrush="{Binding CollarBrush}"; Presets="{Binding MyPalette}" -->

        <DatePicker SelectedDate="{Binding VetDate}" />          <!-- restyled standard control -->
        <Calendar SelectedDate="{Binding VetDate}" />
        <mz:TimePicker SelectedTime="{Binding VetTime}" MinuteIncrement="15" Is24HourClock="False" />

        <mz:Rating Value="{Binding Score}" Maximum="5" AllowHalf="True" IconShape="Star" />
        <mz:Rating Value="4" IconShape="Heart" ItemSize="28" />
        <mz:Rating Value="2" IconShape="PawPrint" FillBrush="{DynamicResource Muzzle.Brush.Secondary}" />
        """;

    public const string Feedback = """
        <!-- Badge as a decorator, or attached to anything -->
        <mz:Badge Value="{Binding Unread}" MaxValue="99" Kind="Error">
            <Button Content="Inbox" />
        </mz:Badge>
        <mz:Badge IsDot="True" Kind="Success"> <Border … /> </mz:Badge>
        <Button mz:MuzzleAssist.Badge="7" mz:MuzzleAssist.BadgeKind="Info" … />

        <!-- Skeleton: shimmer until loaded, then cross-fade into the content -->
        <mz:Skeleton IsLoading="{Binding IsBusy}" SkeletonShape="TextLines" LineCount="3">
            <TextBlock Text="{Binding Bio}" TextWrapping="Wrap" />
        </mz:Skeleton>
        <mz:Skeleton IsLoading="{Binding IsBusy}" SkeletonShape="Circle" Width="52" Height="52">…</mz:Skeleton>

        <!-- Spinners -->
        <mz:LoadingSpinner IsActive="{Binding IsBusy}" Width="36" Height="36" Thickness="4" />
        <mz:LoadingSpinner SpinnerStyle="BouncingDots" AccentBrush="{DynamicResource Muzzle.Brush.Primary}" />

        <!-- InfoBar -->
        <mz:InfoBar Severity="Warning" Title="Low on treats" Message="Only 4 left." IsOpen="{Binding ShowWarning}">
            <mz:InfoBar.ActionContent>
                <Button Content="Reorder" Style="{StaticResource Muzzle.Button.Secondary}" />
            </mz:InfoBar.ActionContent>
        </mz:InfoBar>

        <!-- Popover: wraps its anchor -->
        <mz:Popover Placement="Bottom" TriggerMode="Click" ShowArrow="True" StaysOpen="False">
            <Button Content="Profile" />
            <mz:Popover.PopoverContent>
                <TextBlock Text="Hello!" />
            </mz:Popover.PopoverContent>
        </mz:Popover>

        <!-- Colour picker as a dropdown -->
        <mz:ColorPickerDropDown SelectedColor="{Binding CollarColor}" Width="220" />
        """;

    public const string Navigation = """
        <!-- NavigationView: a ListBox-based side menu (ItemsSource, SelectedItem, keyboard, automation) -->
        <mz:NavigationView IsPaneOpen="{Binding IsMenuOpen}" OpenPaneLength="232" CompactPaneLength="64"
                           SelectedItem="{Binding CurrentSection}">
            <mz:NavigationView.PaneHeader>
                <TextBlock Text="Cat café" FontWeight="SemiBold" />
            </mz:NavigationView.PaneHeader>
            <mz:NavigationViewItem Content="Lounge" IconGeometry="M 2,8 L 8,2.5 L 14,8 M 4,6.5 V 14 H 12 V 6.5" />
            <mz:NavigationViewItem Content="Menu" IconGeometry="M 3,3 H 13 M 3,8 H 13 M 3,13 H 9" />
            <mz:NavigationView.PaneFooter>
                <Button Content="Settings" Style="{StaticResource Muzzle.Button.Ghost}" />
            </mz:NavigationView.PaneFooter>
        </mz:NavigationView>

        <!-- Animated content host -->
        <mz:TransitioningContentControl Content="{Binding CurrentPage}" Transition="SlideUp" />
        <!-- Transitions: None, CrossFade, SlideLeft, SlideRight, SlideUp, SlideDown, ScalePop -->

        <!-- Breadcrumbs -->
        <mz:BreadcrumbBar ItemsSource="{Binding Path}" ItemCommand="{Binding NavigateCommand}"
                          ItemClicked="OnCrumbClicked" />

        <!-- Pagination -->
        <mz:Pagination PageCount="24" CurrentPage="{Binding Page}" MaxDisplayedPages="7"
                       ShowJumpBox="True" ShowFirstLastButtons="True" />

        <!-- StepBar (0-based StepIndex; Items.Count = all done) -->
        <mz:StepBar StepIndex="{Binding Step}" Orientation="Horizontal">
            <mz:StepBarItem Content="Pick a kitten" />
            <mz:StepBarItem Content="Meet &amp; greet" />
            <mz:StepBarItem Content="Adoption day" />
        </mz:StepBar>
        """;

    public const string DataGrid = """
        <!-- Plain DataGrid: Muzzle styles rows, cells, headers and stock column editors automatically. -->
        <DataGrid ItemsSource="{Binding Cats}" AutoGenerateColumns="False" CanUserAddRows="False">
            <DataGrid.Columns>
                <DataGridTemplateColumn Header="Cat" Width="2*" SortMemberPath="Name">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <mz:Avatar Size="28" DisplayName="{Binding Name}" Status="{Binding Status}" />
                                <TextBlock Margin="10,0,0,0" VerticalAlignment="Center" Text="{Binding Name}" />
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                    <DataGridTemplateColumn.CellEditingTemplate>
                        <DataTemplate>
                            <TextBox Text="{Binding Name}" Style="{StaticResource Muzzle.DataGrid.TextBox.Editing}" />
                        </DataTemplate>
                    </DataGridTemplateColumn.CellEditingTemplate>
                </DataGridTemplateColumn>
                <DataGridComboBoxColumn Header="Breed" SelectedItemBinding="{Binding Breed}" ItemsSource="{x:Static local:CatRecord.Breeds}" />
                <DataGridTextColumn Header="Age" Binding="{Binding Age}" />
                <DataGridCheckBoxColumn Header="Indoor" Binding="{Binding IsIndoor}" />
            </DataGrid.Columns>
        </DataGrid>

        <!-- Opt a grid out of the automatic column editors -->
        <DataGrid mz:DataGridAssist.AutoStyleColumns="False" />
        """;

    public const string Avatar = """
        <mz:Avatar DisplayName="Ada Lovelace" />                               <!-- "AL" on a stable pastel -->
        <mz:Avatar DisplayName="Mochi" Size="56" Shape="Squircle" Status="Away" />
        <mz:Avatar Source="{Binding PhotoUrl}" DisplayName="{Binding Name}" Status="{Binding Presence}" />
        <!-- Status: None, Online, Away, Busy, Offline. Failed downloads fall back to initials. -->
        """;

    public const string Carousel = """
        <mz:Carousel ItemsSource="{Binding Slides}" ItemTemplate="{StaticResource SlideTemplate}"
                     SelectedIndex="{Binding CurrentSlide}" AutoPlay="True" Interval="0:0:4"
                     ShowIndicators="True" ShowNavigationButtons="True" IsLooping="True" Height="240" />

        <!-- Or inline content -->
        <mz:Carousel Height="200">
            <Border Background="{DynamicResource Muzzle.Brush.PrimarySoft}" />
            <mz:ImageViewer Source="/Assets/cat.jpg" />
        </mz:Carousel>

        <!-- Pan & zoom: wheel zooms around the pointer, drag pans, double-click springs back to 1x -->
        <mz:ImageViewer Source="/Assets/garden.jpg" Zoom="{Binding Zoom}" MinZoom="0.5" MaxZoom="8" ZoomStep="1.25" />
        """;

    public const string TreeView = """
        <!-- Data-bound -->
        <TreeView ItemsSource="{Binding Rooms}">
            <TreeView.ItemTemplate>
                <HierarchicalDataTemplate ItemsSource="{Binding Children}">
                    <TextBlock Text="{Binding Name}" />
                </HierarchicalDataTemplate>
            </TreeView.ItemTemplate>
        </TreeView>

        <!-- Inline -->
        <TreeView>
            <TreeViewItem Header="Living room" IsExpanded="True">
                <TreeViewItem Header="Sunny window sill" />
                <TreeViewItem Header="Cardboard castle" />
            </TreeViewItem>
        </TreeView>
        """;

    public const string JellyBehavior = """
        <!-- Jelly physics on any element, no retemplating -->
        <Border mz:JellyBehavior.EnableHoverBounce="True"
                mz:JellyBehavior.HoverScale="1.05"
                mz:JellyBehavior.EnablePressSquish="True"
                mz:JellyBehavior.PressSquishScale="0.92"
                mz:JellyBehavior.EntranceAnimation="PopIn"
                mz:JellyBehavior.EntranceDelay="0:0:0.1" />
        <!-- Entrances: None, PopIn, SlideUp, FadeIn. Replay in code: JellyBehavior.PlayEntrance(element) -->
        """;

    public const string ClipHelper = """
        <!-- On a Border the child is clipped inside the border and padding, with matching inner radii -->
        <Border CornerRadius="20" BorderThickness="3" BorderBrush="{DynamicResource Muzzle.Brush.Primary}"
                mz:ClipHelper.CornerRadius="20">
            <Image Source="/Assets/garden.png" Stretch="UniformToFill" />
        </Border>

        <!-- On anything else the element itself is clipped -->
        <ScrollViewer mz:ClipHelper.CornerRadius="16"> … </ScrollViewer>
        """;

    public const string ScrollAssist = """
        <ScrollViewer mz:ScrollViewerAssist.EnableSmoothScroll="True"
                      mz:ScrollViewerAssist.EnableShiftHorizontalScroll="True"
                      mz:ScrollViewerAssist.WheelStep="96" />
        <!-- Also works on a ListBox, TreeView or any element that contains a ScrollViewer -->
        """;

    public const string FocusEscape = """
        <!-- Tab / Shift+Tab stay inside; Esc sets IsOpen back to false -->
        <Border mz:FocusTrap.IsEnabled="True"
                mz:FocusTrap.FocusFirstOnShow="True"
                mz:EscapeKeyBehavior.IsOpen="{Binding IsPanelOpen}"
                mz:EscapeKeyBehavior.Command="{Binding CancelCommand}" />
        """;

    public const string Converters = """
        <TextBlock Visibility="{Binding IsBusy, Converter={x:Static mz:BoolToVisibilityConverter.Instance}}" />
        <TextBlock Visibility="{Binding IsBusy, Converter={x:Static mz:BoolToVisibilityConverter.Instance}, ConverterParameter=Invert}" />
        <TextBlock Visibility="{Binding SelectedCat, Converter={x:Static mz:NullToVisibilityConverter.Instance}}" />
        <TextBlock Text="{Binding Name, Converter={x:Static mz:StringToInitialsConverter.Instance}}" />
        <Border Background="{Binding Accent, Converter={x:Static mz:ColorToSoftTintConverter.Instance}, ConverterParameter=0.2}" />
        """;

    public const string ThemeDictionary = """
        <!-- App.xaml: the whole theme, base theme and accent in one line (this gallery does exactly this) -->
        <Application.Resources>
            <mz:MuzzleThemeResourceDictionary Theme="System" Accent="Lavender">
                <!-- your own resources can live in here too -->
            </mz:MuzzleThemeResourceDictionary>
        </Application.Resources>
        """;

    public const string Icons = """
        <mz:MuzzleIcon Kind="Search" IconSize="18" />
        <mz:MuzzleIcon Kind="Heart" IconSize="24" Foreground="{DynamicResource Muzzle.Brush.Primary}" StrokeThickness="2.2" />

        <!-- Or use the geometry directly (24 x 24 grid, stroke-drawn) -->
        <Path Data="{StaticResource Muzzle.Icon.Paw}" Stroke="Gray" StrokeThickness="1.8"
              StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round" />
        """;

    public const string CommandPalette = """
        <!-- Lay it over the window's content; Ctrl+K toggles it from anywhere in that window -->
        <Grid>
            <ContentControl Content="{Binding CurrentPage}" />
            <mz:CommandPalette ItemsSource="{Binding Commands}" ShortcutGesture="Ctrl+K"
                               Placeholder="Type a command or search…" ItemInvoked="OnCommandInvoked" />
        </Grid>
        """;

    public const string CommandPaletteCode = """
        Commands = [
            new CommandPaletteItem {
                Title = "Dark theme",
                Category = "Theme",
                Icon = MuzzleIconKind.Moon,
                Shortcut = "Ctrl+Shift+D",
                Keywords = "night",
                Command = new RelayCommand(_ => MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark, true))
            }
        ];
        """;

    public const string Buttons2 = """
        <mz:SplitButton Content="Save" Click="OnSave" SegmentStyle="{StaticResource Muzzle.Button.Primary}">
            <mz:SplitButton.MenuItems>
                <MenuItem Header="Save as…" InputGestureText="Ctrl+Shift+S" />
                <MenuItem Header="Save a copy" />
                <Separator />
                <MenuItem Header="Autosave" IsCheckable="True" IsChecked="True" />
            </mz:SplitButton.MenuItems>
        </mz:SplitButton>

        <mz:DropDownButton Content="Sort">
            <mz:DropDownButton.DropDownContent>
                <StackPanel> … any content … </StackPanel>
            </mz:DropDownButton.DropDownContent>
        </mz:DropDownButton>

        <!-- Splitters get a pill handle automatically -->
        <GridSplitter Grid.Column="1" Width="10" HorizontalAlignment="Center" />
        """;

    public const string Menus = """
        <!-- No keys needed: Menu, ContextMenu, MenuItem and Separator are styled implicitly -->
        <Menu>
            <MenuItem Header="_File">
                <MenuItem Header="_Save" InputGestureText="Ctrl+S" />
                <MenuItem Header="_Export">
                    <MenuItem Header="PDF" />
                </MenuItem>
                <Separator />
                <MenuItem Header="Autosave" IsCheckable="True" IsChecked="True" />
            </MenuItem>
        </Menu>

        <Border.ContextMenu>
            <ContextMenu>
                <MenuItem Header="Pet" />
            </ContextMenu>
        </Border.ContextMenu>
        """;

    public const string SmallControls = """
        <mz:EmptyState IconKind="Inbox" Title="No treats yet" Description="Treats you save show up here.">
            <mz:EmptyState.ActionContent>
                <Button Content="Add a treat" Style="{StaticResource Muzzle.Button.Primary}" />
            </mz:EmptyState.ActionContent>
        </mz:EmptyState>

        <mz:Keycap KeyText="Ctrl" />
        <mz:ShortcutBadge Shortcut="Ctrl+Shift+P" />

        <mz:RangeSlider Minimum="0" Maximum="100" LowerValue="{Binding MinPrice}" UpperValue="{Binding MaxPrice}" StepFrequency="5" />

        <PasswordBox x:Name="Secret" />
        <mz:PasswordStrengthMeter PasswordBox="{Binding ElementName=Secret}" ShowLabel="True" />

        <mz:CodeBlock Code="{Binding Snippet}" Language="xaml" ShowLineNumbers="True" />
        <mz:CopyableText Text="MZ-2026-PAWS" />
        """;

    public const string WindowHelpers = """
        <mz:MuzzleWindow mz:WindowStateSaver.Key="MainWindow"
                         mz:BackdropHelper.BackdropType="Mica">
            <Grid>
                <mz:SystemTrayIcon IconSource="{StaticResource AppIcon}" ToolTipText="Muzzle"
                                   LeftClickCommand="{Binding ShowWindowCommand}">
                    <mz:SystemTrayIcon.MenuContent>
                        <StackPanel>
                            <MenuItem Header="Open Muzzle" />
                            <Separator />
                            <MenuItem Header="Quit" />
                        </StackPanel>
                    </mz:SystemTrayIcon.MenuContent>
                </mz:SystemTrayIcon>
            </Grid>
        </mz:MuzzleWindow>
        """;
}