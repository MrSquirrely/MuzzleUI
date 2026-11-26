using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Muzzle.Theme.Controls;

/// <summary>A collapsible group of <see cref="PropertyItem"/>s sharing a [Category].</summary>
public sealed class PropertyCategory : INotifyPropertyChanged {
    private bool isExpanded = true;
    private bool isVisible = true;
    private int visibleCount;

    internal PropertyCategory(string name, IEnumerable<PropertyItem> items) {
        Name = name;
        Items = new ObservableCollection<PropertyItem>(items);
        visibleCount = Items.Count;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the category name (from <c>[Category]</c>, or "Misc").</summary>
    public string Name { get; }

    /// <summary>Gets the properties in this category.</summary>
    public ObservableCollection<PropertyItem> Items { get; }

    /// <summary>Gets or sets whether the category is expanded. The default is <see langword="true"/>.</summary>
    public bool IsExpanded {
        get => isExpanded;
        set => SetField(ref isExpanded, value);
    }

    /// <summary>Gets whether any of its properties match the current search.</summary>
    public bool IsVisible {
        get => isVisible;
        private set => SetField(ref isVisible, value);
    }

    /// <summary>Gets how many of its properties match the current search.</summary>
    public int VisibleCount {
        get => visibleCount;
        private set => SetField(ref visibleCount, value);
    }

    internal int ApplyFilter(string query) {
        bool categoryMatches = (query.Length > 0) && Name.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        int count = 0;
        foreach (PropertyItem item in Items) {
            bool visible = categoryMatches || item.Matches(query);
            item.IsVisible = visible;
            if (visible) {
                count++;
            }
        }
        VisibleCount = count;
        IsVisible = count > 0;
        if ((query.Length > 0) && (count > 0)) {
            IsExpanded = true;
        }
        return count;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}