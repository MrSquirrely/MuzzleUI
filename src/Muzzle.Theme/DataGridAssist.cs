using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Muzzle.Theme;

/// <summary>
/// DataGrid helpers. <see cref="AutoStyleColumnsProperty"/> (on by default in Muzzle's DataGrid style) gives the stock
/// text, check box and combo box columns Muzzle display and editing elements, unless a column sets its own
/// <c>ElementStyle</c> / <c>EditingElementStyle</c>.
/// </summary>
public static class DataGridAssist {
    /// <summary>Resource key of the display TextBlock style applied to text columns.</summary>
    public const string TextBlockStyleKey = "Muzzle.DataGrid.TextBlock";
    /// <summary>Resource key of the editing TextBox style applied to text columns.</summary>
    public const string TextBoxEditingStyleKey = "Muzzle.DataGrid.TextBox.Editing";
    /// <summary>Resource key of the display CheckBox style applied to check box columns.</summary>
    public const string CheckBoxStyleKey = "Muzzle.DataGrid.CheckBox";
    /// <summary>Resource key of the editing CheckBox style applied to check box columns.</summary>
    public const string CheckBoxEditingStyleKey = "Muzzle.DataGrid.CheckBox.Editing";
    /// <summary>Resource key of the editing ComboBox style applied to combo box columns.</summary>
    public const string ComboBoxEditingStyleKey = "Muzzle.DataGrid.ComboBox.Editing";

    /// <summary>Identifies the <c>AutoStyleColumns</c> attached property.</summary>
    public static readonly DependencyProperty AutoStyleColumnsProperty = DependencyProperty.RegisterAttached(
        "AutoStyleColumns", typeof(bool), typeof(DataGridAssist), new PropertyMetadata(false, OnAutoStyleColumnsChanged));

    /// <summary>
    /// Check box columns toggle on the first click (the stock DataGrid needs one click to select the cell and a second
    /// to toggle). On by default in Muzzle's DataGrid style.
    /// </summary>
    public static readonly DependencyProperty SingleClickCheckBoxesProperty = DependencyProperty.RegisterAttached(
        "SingleClickCheckBoxes", typeof(bool), typeof(DataGridAssist), new PropertyMetadata(false, OnSingleClickCheckBoxesChanged));

    /// <summary>Gets the value of the <c>SingleClickCheckBoxes</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>SingleClickCheckBoxes</c> value.</returns>
    public static bool GetSingleClickCheckBoxes(DependencyObject element) {
        return (bool)element.GetValue(SingleClickCheckBoxesProperty);
    }

    /// <summary>Sets the value of the <c>SingleClickCheckBoxes</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetSingleClickCheckBoxes(DependencyObject element, bool value) {
        element.SetValue(SingleClickCheckBoxesProperty, value);
    }

    private static void OnSingleClickCheckBoxesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not DataGrid grid) {
            return;
        }
        grid.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)OnGridPreviewMouseLeftButtonDown);
        if (e.NewValue is true) {
            grid.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)OnGridPreviewMouseLeftButtonDown, true);
        }
    }

    // Make the clicked check box cell current and selected before the DataGrid sees the press: its own handler then
    // begins the edit, and DataGridCheckBoxColumn toggles the box when the edit starts from a click on it.
    private static void OnGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        if ((sender is not DataGrid grid) || grid.IsReadOnly || (Keyboard.Modifiers != ModifierKeys.None)) {
            return;
        }
        DataGridCell? cell = FindCell(e.OriginalSource as DependencyObject);
        if ((cell is null) || cell.IsEditing || cell.IsReadOnly || (cell.Column is not DataGridCheckBoxColumn)) {
            return;
        }
        if (!cell.IsKeyboardFocusWithin) {
            cell.Focus();
        }
        if (cell.IsSelected) {
            return;
        }
        if (grid.SelectionUnit == DataGridSelectionUnit.FullRow) {
            if (DataGridRow.GetRowContainingElement(cell) is DataGridRow row) {
                grid.SelectedItem = row.Item;
            }
        }
        else {
            grid.SelectedCells.Clear();
            cell.IsSelected = true;
        }
    }

    private static DataGridCell? FindCell(DependencyObject? source) {
        while (source is not null) {
            if (source is DataGridCell cell) {
                return cell;
            }
            if (source is DataGrid) {
                return null;
            }
            source = (source is Visual or System.Windows.Media.Media3D.Visual3D) ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        return null;
    }

    /// <summary>Gets the value of the <c>AutoStyleColumns</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>AutoStyleColumns</c> value.</returns>
    public static bool GetAutoStyleColumns(DependencyObject element) {
        return (bool)element.GetValue(AutoStyleColumnsProperty);
    }

    /// <summary>Sets the value of the <c>AutoStyleColumns</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetAutoStyleColumns(DependencyObject element, bool value) {
        element.SetValue(AutoStyleColumnsProperty, value);
    }

    // The per-grid Columns.CollectionChanged handler, kept so it can be removed again.
    private static readonly DependencyProperty ColumnsHandlerProperty = DependencyProperty.RegisterAttached(
        "ColumnsHandler", typeof(NotifyCollectionChangedEventHandler), typeof(DataGridAssist), new PropertyMetadata(null));

    private static void OnAutoStyleColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not DataGrid grid) {
            return;
        }
        if (grid.GetValue(ColumnsHandlerProperty) is NotifyCollectionChangedEventHandler existing) {
            grid.Columns.CollectionChanged -= existing;
            grid.ClearValue(ColumnsHandlerProperty);
        }
        grid.Loaded -= OnGridLoaded;
        if (e.NewValue is true) {
            NotifyCollectionChangedEventHandler handler = (_, args) => OnColumnsChanged(grid, args);
            grid.SetValue(ColumnsHandlerProperty, handler);
            grid.Columns.CollectionChanged += handler;
            grid.Loaded += OnGridLoaded;
            StyleColumns(grid);
        }
    }

    private static void OnGridLoaded(object sender, RoutedEventArgs e) {
        StyleColumns((DataGrid)sender);
    }

    private static void OnColumnsChanged(DataGrid grid, NotifyCollectionChangedEventArgs e) {
        if (e.NewItems is null) {
            return;
        }
        foreach (object item in e.NewItems) {
            if (item is DataGridColumn column) {
                StyleColumn(grid, column);
            }
        }
    }

    private static void StyleColumns(DataGrid grid) {
        foreach (DataGridColumn column in grid.Columns) {
            StyleColumn(grid, column);
        }
    }

    private static void StyleColumn(DataGrid grid, DataGridColumn column) {
        switch (column) {
            case DataGridCheckBoxColumn:
                Apply(grid, column, DataGridBoundColumn.ElementStyleProperty, CheckBoxStyleKey);
                Apply(grid, column, DataGridBoundColumn.EditingElementStyleProperty, CheckBoxEditingStyleKey);
                break;
            case DataGridTextColumn:
                Apply(grid, column, DataGridBoundColumn.ElementStyleProperty, TextBlockStyleKey);
                Apply(grid, column, DataGridBoundColumn.EditingElementStyleProperty, TextBoxEditingStyleKey);
                break;
            case DataGridComboBoxColumn:
                Apply(grid, column, DataGridComboBoxColumn.EditingElementStyleProperty, ComboBoxEditingStyleKey);
                break;
        }
    }

    // The stock columns default these properties to their own built-in styles (never null), so "not set by the app"
    // means "no local value".
    private static void Apply(DataGrid grid, DataGridColumn column, DependencyProperty property, string key) {
        if ((column.ReadLocalValue(property) == DependencyProperty.UnsetValue) && (grid.TryFindResource(key) is Style style)) {
            column.SetValue(property, style);
        }
    }
}