using System;
using System.Windows.Controls;

using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Models;

/// <summary>One entry in the gallery's sidebar: a title, search keywords, a MuzzleIcon and a page factory.</summary>
public sealed class NavigationItem(string title, string keywords, MuzzleIconKind icon, Func<UserControl> createPage) {
    public string Title { get; } = title;

    public string Keywords { get; } = keywords;

    public MuzzleIconKind Icon { get; } = icon;

    public UserControl CreatePage() {
        return createPage();
    }

    public bool Matches(string query) {
        return (query.Length == 0)
            || Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || Keywords.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    public override string ToString() {
        return Title;
    }
}