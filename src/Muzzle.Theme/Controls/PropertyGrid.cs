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
        items.Sort(static (left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase));
        foreach (PropertyItem item in items) {
            Properties.Add(item);
            if (IsLoaded) {
                item.Attach();
            }
        }

        IEnumerable<IGrouping<string, PropertyItem>> groups = items
            .GroupBy(static item => item.Category)
            .OrderBy(static group => group.Key == MiscCategory ? 1 : 0)
            .ThenBy(static group => group.Key, StringComparer.CurrentCultureIgnoreCase);
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