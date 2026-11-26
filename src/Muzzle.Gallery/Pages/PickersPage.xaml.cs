using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Pages;

public partial class PickersPage : UserControl {
    private static readonly string[] CatNames = [
        "Mochi", "Biscuit", "Tofu", "Pudding", "Noodle", "Pickle", "Waffles", "Dumpling",
        "Marshmallow", "Sesame", "Peaches", "Bean", "Miso", "Nori", "Muffin", "Pretzel", "Mango", "Butterscotch"
    ];

    private readonly ObservableCollection<string> traits = ["fluffy", "orange", "sleepy"];

    public PickersPage() {
        InitializeComponent();
        Tags.Tags = traits;
        traits.CollectionChanged += OnTraitsChanged;
        UpdateTagSummary();
        CatSearch.SuggestionsSource = CatNames;
        VetDate.SelectedDate = DateTime.Today.AddDays(3);
        VetTime.SelectedTime = new TimeSpan(10, 15, 0);
        InlineCalendar.SelectedDate = DateTime.Today.AddDays(1);
        VetDate.SelectedDateChanged += OnAppointmentChanged;
        VetTime.SelectedTimeChanged += OnAppointmentChanged;
        UpdateAppointment();
    }

    private void OnTraitsChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        UpdateTagSummary();
    }

    private void UpdateTagSummary() {
        TagSummary.Text = traits.Count == 0 ? "No traits yet." : $"Bound collection ({traits.Count}): {string.Join(", ", traits)}";
    }

    private void OnSearchRequested(object sender, SearchRequestedEventArgs e) {
        string mode = e.IsSubmitted ? "submitted" : "debounced";
        SearchStatus.Text = e.Query.Length == 0 ? "Search cleared." : $"Searching for “{e.Query}” ({mode})";
    }

    private void OnAppointmentChanged(object? sender, RoutedEventArgs e) {
        UpdateAppointment();
    }

    private void UpdateAppointment() {
        if ((VetDate.SelectedDate is DateTime date) && (VetTime.SelectedTime is TimeSpan time)) {
            AppointmentSummary.Text = date.Add(time).ToString("dddd d MMMM 'at' h:mm tt", CultureInfo.CurrentCulture);
        }
        else {
            AppointmentSummary.Text = "Pick a date and a time.";
        }
    }
}