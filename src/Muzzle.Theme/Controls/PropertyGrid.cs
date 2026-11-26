using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A reflection-based inspector. Set <see cref="SelectedObject"/> and every public, writable, [Browsable] property
/// is grouped by [Category] and given a Muzzle-styled editor; [DisplayName] and [Description] are honoured, and
/// System.ComponentModel.DataAnnotations.[Range] turns numeric editors into sliders.
/// <see cref="SortMode"/> keeps a declared order, <see cref="ShowHeader"/> and <see cref="ShowTypeNames"/> trim the
/// inspector chrome for an end-user settings pane, and <see cref="RowTemplate"/>, <see cref="CategoryTemplate"/>
/// and <see cref="EditorTemplateSelector"/> replace parts of it.
/// </summary>
public class PropertyGrid : Control {
    private const string MiscCategory = "Misc";

    /// <summary>Identifies the <see cref="SelectedObject"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(
        nameof(SelectedObject),
        typeof(object),
        typeof(PropertyGrid),
        new PropertyMetadata(null, OnSelectedObjectChanged));

    /// <summary>Identifies the <see cref="ShowSearchBox"/> dependency property.</summary>
    public static readonly DependencyProperty ShowSearchBoxProperty = DependencyProperty.Register(
        nameof(ShowSearchBox),
        typeof(bool),
        typeof(PropertyGrid),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowCategories"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCategoriesProperty = DependencyProperty.Register(
        nameof(ShowCategories),
        typeof(bool),
        typeof(PropertyGrid),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowDescription"/> dependency property.</summary>
    public static readonly DependencyProperty ShowDescriptionProperty = DependencyProperty.Register(
        nameof(ShowDescription),
        typeof(bool),
        typeof(PropertyGrid),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowHeader"/> dependency property.</summary>
    public static readonly DependencyProperty ShowHeaderProperty = DependencyProperty.Register(
        nameof(ShowHeader),
        typeof(bool),
        typeof(PropertyGrid),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowTypeNames"/> dependency property.</summary>
    public static readonly DependencyProperty ShowTypeNamesProperty = DependencyProperty.Register(
        nameof(ShowTypeNames),
        typeof(bool),
        typeof(PropertyGrid),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowResetButtons"/> dependency property.</summary>
    public static readonly DependencyProperty ShowResetButtonsProperty = DependencyProperty.Register(
        nameof(ShowResetButtons),
        typeof(bool),
        typeof(PropertyGrid),
        new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="SortMode"/> dependency property.</summary>
    public static readonly DependencyProperty SortModeProperty = DependencyProperty.Register(
        nameof(SortMode),
        typeof(PropertyGridSortMode),
        typeof(PropertyGrid),
        new PropertyMetadata(PropertyGridSortMode.Alphabetical, OnSelectedObjectChanged));

    /// <summary>Identifies the <see cref="LabelWidth"/> dependency property.</summary>
    public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(
        nameof(LabelWidth),
        typeof(GridLength),
        typeof(PropertyGrid),
        new PropertyMetadata(new GridLength(2, GridUnitType.Star)));

    /// <summary>Identifies the <see cref="MinLabelWidth"/> dependency property.</summary>
    public static readonly DependencyProperty MinLabelWidthProperty = DependencyProperty.Register(
        nameof(MinLabelWidth),
        typeof(double),
        typeof(PropertyGrid),
        new PropertyMetadata(90.0),
        static value => value is double width && double.IsFinite(width) && (width >= 0));

    /// <summary>Identifies the <see cref="MaxLabelWidth"/> dependency property.</summary>
    public static readonly DependencyProperty MaxLabelWidthProperty = DependencyProperty.Register(
        nameof(MaxLabelWidth),
        typeof(double),
        typeof(PropertyGrid),
        new PropertyMetadata(double.PositiveInfinity),
        static value => value is double width && !double.IsNaN(width) && (width >= 0));

    /// <summary>Identifies the <see cref="RowTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty RowTemplateProperty = DependencyProperty.Register(
        nameof(RowTemplate),
        typeof(DataTemplate),
        typeof(PropertyGrid),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CategoryTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty CategoryTemplateProperty = DependencyProperty.Register(
        nameof(CategoryTemplate),
        typeof(DataTemplate),
        typeof(PropertyGrid),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="EditorTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty EditorTemplateSelectorProperty = DependencyProperty.Register(
        nameof(EditorTemplateSelector),
        typeof(DataTemplateSelector),
        typeof(PropertyGrid),
        new PropertyMetadata(null, OnSelectedObjectChanged));

    /// <summary>Identifies the <see cref="SearchText"/> dependency property.</summary>
    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
        nameof(SearchText),
        typeof(string),
        typeof(PropertyGrid),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchTextChanged));

    /// <summary>Identifies the <see cref="SelectedProperty"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedPropertyProperty = DependencyProperty.Register(
        nameof(SelectedProperty),
        typeof(PropertyItem),
        typeof(PropertyGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedPropertyChanged));

    private static readonly DependencyPropertyKey CategoriesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Categories),
        typeof(ObservableCollection<PropertyCategory>),
        typeof(PropertyGrid),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Categories"/> read-only dependency property.</summary>
    public static readonly DependencyProperty CategoriesProperty = CategoriesPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey PropertiesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Properties),
        typeof(ObservableCollection<PropertyItem>),
        typeof(PropertyGrid),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Properties"/> read-only dependency property.</summary>
    public static readonly DependencyProperty PropertiesProperty = PropertiesPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey SelectedObjectTypeNamePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectedObjectTypeName),
        typeof(string),
        typeof(PropertyGrid),
        new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="SelectedObjectTypeName"/> read-only dependency property.</summary>
    public static readonly DependencyProperty SelectedObjectTypeNameProperty = SelectedObjectTypeNamePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey HasNoMatchesPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasNoMatches),
        typeof(bool),
        typeof(PropertyGrid),
        new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasNoMatches"/> read-only dependency property.</summary>
    public static readonly DependencyProperty HasNoMatchesProperty = HasNoMatchesPropertyKey.DependencyProperty;

    static PropertyGrid() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid), new FrameworkPropertyMetadata(typeof(PropertyGrid)));
    }

    /// <summary>Initializes a new instance of the <see cref="PropertyGrid"/> class.</summary>
    public PropertyGrid() {
        SetValue(CategoriesPropertyKey, new ObservableCollection<PropertyCategory>());
        SetValue(PropertiesPropertyKey, new ObservableCollection<PropertyItem>());
        AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown), true);
        AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus), true);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Gets or sets the selected object. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? SelectedObject {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    /// <summary>Gets or sets whether to show the search box. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowSearchBox {
        get => (bool)GetValue(ShowSearchBoxProperty);
        set => SetValue(ShowSearchBoxProperty, value);
    }

    /// <summary>Gets or sets whether to show the categories. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowCategories {
        get => (bool)GetValue(ShowCategoriesProperty);
        set => SetValue(ShowCategoriesProperty, value);
    }

    /// <summary>Gets or sets whether to show the description. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowDescription {
        get => (bool)GetValue(ShowDescriptionProperty);
        set => SetValue(ShowDescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the header (the overline, the type name and the property count) is shown.
    /// The default is <see langword="true"/>. This is a dependency property.
    /// </summary>
    public bool ShowHeader {
        get => (bool)GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets whether CLR type names are shown (the selected object's in the header, the selected property's
    /// in the description panel). The default is <see langword="true"/>. This is a dependency property.
    /// </summary>
    public bool ShowTypeNames {
        get => (bool)GetValue(ShowTypeNamesProperty);
        set => SetValue(ShowTypeNamesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether each row ends in a reset button that restores the property's default value and is
    /// disabled while the value is the default. The default is <see langword="false"/>. This is a dependency property.
    /// </summary>
    public bool ShowResetButtons {
        get => (bool)GetValue(ShowResetButtonsProperty);
        set => SetValue(ShowResetButtonsProperty, value);
    }

    /// <summary>
    /// Gets or sets how properties and categories are ordered. The default is
    /// <see cref="PropertyGridSortMode.Alphabetical"/>. This is a dependency property.
    /// </summary>
    public PropertyGridSortMode SortMode {
        get => (PropertyGridSortMode)GetValue(SortModeProperty);
        set => SetValue(SortModeProperty, value);
    }

    /// <summary>Gets or sets the width of the label column. The default is <c>2*</c> (against <c>3*</c> for the editor). This is a dependency property.</summary>
    public GridLength LabelWidth {
        get => (GridLength)GetValue(LabelWidthProperty);
        set => SetValue(LabelWidthProperty, value);
    }

    /// <summary>Gets or sets the smallest width of the label column. The default is 90. This is a dependency property.</summary>
    public double MinLabelWidth {
        get => (double)GetValue(MinLabelWidthProperty);
        set => SetValue(MinLabelWidthProperty, value);
    }

    /// <summary>Gets or sets the largest width of the label column. The default is <see cref="double.PositiveInfinity"/>. This is a dependency property.</summary>
    public double MaxLabelWidth {
        get => (double)GetValue(MaxLabelWidthProperty);
        set => SetValue(MaxLabelWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the template for one property row (its data context is a <see cref="PropertyItem"/>).
    /// The default is the theme's <c>Muzzle.PropertyGrid.RowTemplate</c>. This is a dependency property.
    /// </summary>
    public DataTemplate? RowTemplate {
        get => (DataTemplate?)GetValue(RowTemplateProperty);
        set => SetValue(RowTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the template for one category (its data context is a <see cref="PropertyCategory"/>).
    /// The default is the theme's <c>Muzzle.PropertyGrid.CategoryTemplate</c>. This is a dependency property.
    /// </summary>
    public DataTemplate? CategoryTemplate {
        get => (DataTemplate?)GetValue(CategoryTemplateProperty);
        set => SetValue(CategoryTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets a selector asked for each property's editor template before the built-in editors; return
    /// <see langword="null"/> from it to keep the built-in editor. The default is <see langword="null"/>. This is a dependency property.
    /// </summary>
    public DataTemplateSelector? EditorTemplateSelector {
        get => (DataTemplateSelector?)GetValue(EditorTemplateSelectorProperty);
        set => SetValue(EditorTemplateSelectorProperty, value);
    }

    /// <summary>Gets or sets the search text. The default is an empty string. This is a dependency property.</summary>
    public string SearchText {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <summary>Gets or sets the selected property. The default is <see langword="null"/>. This is a dependency property.</summary>
    public PropertyItem? SelectedProperty {
        get => (PropertyItem?)GetValue(SelectedPropertyProperty);
        set => SetValue(SelectedPropertyProperty, value);
    }

    /// <summary>Gets the categories. This is a read-only dependency property.</summary>
    public ObservableCollection<PropertyCategory> Categories => (ObservableCollection<PropertyCategory>)GetValue(CategoriesProperty);

    /// <summary>Gets the properties. This is a read-only dependency property.</summary>
    public ObservableCollection<PropertyItem> Properties => (ObservableCollection<PropertyItem>)GetValue(PropertiesProperty);

    /// <summary>Gets the selected object type name. This is a read-only dependency property.</summary>
    public string SelectedObjectTypeName => (string)GetValue(SelectedObjectTypeNameProperty);

    /// <summary>Gets whether it has no matches. This is a read-only dependency property.</summary>
    public bool HasNoMatches => (bool)GetValue(HasNoMatchesProperty);

    /// <summary>Re-reads the property list of <see cref="SelectedObject"/>.</summary>
    public void Refresh() {
        Rebuild();
    }

    private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((PropertyGrid)d).Rebuild();
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((PropertyGrid)d).ApplyFilter();
    }

    private static void OnSelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (e.OldValue is PropertyItem oldItem) {
            oldItem.IsSelected = false;
        }
        if (e.NewValue is PropertyItem newItem) {
            newItem.IsSelected = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        foreach (PropertyItem item in Properties) {
            item.Attach();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        foreach (PropertyItem item in Properties) {
            item.Detach();
        }
    }

    private void Rebuild() {
        foreach (PropertyItem item in Properties) {
            item.Detach();
        }
        SelectedProperty = null;
        Categories.Clear();
        Properties.Clear();

        object? target = SelectedObject;
        SetValue(SelectedObjectTypeNamePropertyKey, target?.GetType().Name ?? string.Empty);
        if (target is null) {
            ApplyFilter();
            return;
        }

        List<PropertyItem> items = [];
        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(target, [BrowsableAttribute.Yes])) {
            if (descriptor.IsReadOnly) {
                continue;
            }
            items.Add(new PropertyItem(target, descriptor));
        }
        bool declared = SortMode == PropertyGridSortMode.Declared;
        // OrderBy is stable, so properties without an explicit order keep the order the type declares them in.
        // 10000 is where DataAnnotations places a property that has no [Display(Order)].
        items = declared
            ? [.. items.OrderBy(static item => item.Order ?? 10000)]
            : [.. items.OrderBy(static item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)];
        foreach (PropertyItem item in items) {
            Properties.Add(item);
            if (IsLoaded) {
                item.Attach();
            }
        }

        // GroupBy yields groups in order of first appearance, which is the declared order of categories.
        IEnumerable<IGrouping<string, PropertyItem>> groups = items.GroupBy(static item => item.Category);
        if (!declared) {
            groups = groups
                .OrderBy(static group => group.Key == MiscCategory ? 1 : 0)
                .ThenBy(static group => group.Key, StringComparer.CurrentCultureIgnoreCase);
        }
        foreach (IGrouping<string, PropertyItem> group in groups) {
            Categories.Add(new PropertyCategory(group.Key, group));
        }
        ApplyFilter();
    }

    private void ApplyFilter() {
        string query = SearchText?.Trim() ?? string.Empty;
        int visible = 0;
        foreach (PropertyCategory category in Categories) {
            visible += category.ApplyFilter(query);
        }
        SetValue(HasNoMatchesPropertyKey, (SelectedObject is not null) && (visible == 0));
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
        SelectFrom(e.OriginalSource as DependencyObject);
    }

    private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
        SelectFrom(e.NewFocus as DependencyObject);
    }

    private void SelectFrom(DependencyObject? source) {
        DependencyObject? current = source;
        while ((current is not null) && !ReferenceEquals(current, this)) {
            object? context = current switch {
                FrameworkElement element => element.DataContext,
                FrameworkContentElement element => element.DataContext,
                _ => null
            };
            if (context is PropertyItem item) {
                SelectedProperty = item;
                return;
            }
            DependencyObject? parent = (current is Visual) || (current is Visual3D) ? VisualTreeHelper.GetParent(current) : null;
            current = parent ?? LogicalTreeHelper.GetParent(current);
        }
    }
}