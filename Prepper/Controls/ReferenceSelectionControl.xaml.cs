using Prepper.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Prepper.Controls
{
    /// <summary>
    /// Interaction logic for PrepControl.xaml
    /// </summary>
    public partial class ReferenceSelectionControl : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private const string AllSelection = "All";
        private readonly IReferenceProvider provider;
        private List<string> results;
        private List<IEpisode>? currentFullResults;

        private List<IEpisode> currentEpisodeResults;
        public List<IEpisode> CurrentEpisodeResults
        {
            get
            {
                if (currentEpisodeResults == null) CurrentEpisodeResults = new List<IEpisode>();
                return CurrentEpisodeResults;
            }
            set
            {
                currentEpisodeResults = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<AddToPrepEventArgs> AddEpisodesToPrep;

        public ReferenceSelectionControl(IReferenceProvider provider)
        {
            InitializeComponent();
            this.provider = provider;
            provider.SetMediaType(provider.GetMediaTypes()[0]);
            currentFullResults = new List<IEpisode>();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnAddToPrep(List<IEpisode> episodesToAdd)
        {
            AddEpisodesToPrep?.Invoke(this, new AddToPrepEventArgs(episodesToAdd));
        }

        public List<string> GetSelections()
        {
            List<string> selections = new();
            foreach (var item in ResultsList.SelectedItems)
            {
                if (item != null && item is string i) selections.Add(i);
            }
            return selections;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            DoSearch(SearchBox.Text);
        }

        private void DoSearch(string searchText)
        {
            results ??= new List<string>();
            results.Clear();
            provider.PerformTitleSearch(searchText);
            results.AddRange(provider.GetSearchResults());
            ResultsList.Items.Clear();
            results.ForEach(r => ResultsList.Items.Add(r));
        }

        private void MediaTypeComboxBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e?.AddedItems?.Count > 0)
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Do nothing for now...
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            DoLoad();
        }

        private void DoLoad()
        {
            if (ResultsList.SelectedItem == null) return;
            var title = ResultsList.SelectedItem.ToString();

            List<IEpisode>? episodes = provider.GetEpisodesForTitle(title);
            currentFullResults = episodes;
            if (episodes == null) return;
            episodes = SortEpisodesBySeasonThenEpisodeNumber(episodes);
            if (episodes != null) UpdateEpisodeResults(episodes);
            List<int> seasons = episodes!.DistinctBy(e => new { e.Season }).ToList().Select(e => e.Season).ToList();
            SeasonSelectionBox.Items.Clear();
            SeasonSelectionBox.SelectedIndex = -1;
            FilterBySeasonTextBox.Visibility = Visibility.Visible;
            SeasonSelectionBox.Items.Add(AllSelection);
            seasons.ForEach(s => { SeasonSelectionBox.Items.Add(s); });
        }

        internal static List<IEpisode> SortEpisodesBySeasonThenEpisodeNumber(List<IEpisode> episodes)
        {
            episodes!.Sort((e1, e2) =>
            {
                if (e1.Season != e2.Season) return e1.Season.CompareTo(e2.Season);
                else return e1.EpisodeNumber.CompareTo(e2.EpisodeNumber);
            });
            return episodes;
        }

        private void UpdateEpisodeResults(List<IEpisode> episodes)
        {
            SelectionList.Items.Clear();
            episodes.ForEach(e => SelectionList.Items.Add(e));
        }

        private void AddToPrepButton_Click(object sender, RoutedEventArgs e)
        {
            List<IEpisode> selections = new();
            foreach (object s in SelectionList.SelectedItems)
            {
                selections.Add((IEpisode)s);
            }

            OnAddToPrep(selections);
            SelectionList.UnselectAll();
        }

        private void SearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                DoSearch(SearchBox.Text);
            }
        }

        private void SeasonSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SeasonSelectionBox.SelectedIndex == -1)
            {
                FilterBySeasonTextBox.Visibility = Visibility.Visible;
                return;
            }
            FilterBySeasonTextBox.Visibility = Visibility.Hidden;
            string? selection = SeasonSelectionBox.SelectedItem.ToString();
            if (!int.TryParse(selection, out int seasonSelection) && !selection!.Equals(AllSelection)) return;
            if (currentFullResults == null) return;
            List<IEpisode> episodes = new(currentFullResults);
            if (!selection.Equals(AllSelection)) episodes.RemoveAll(e => e.Season != seasonSelection);
            episodes = SortEpisodesBySeasonThenEpisodeNumber(episodes);
            UpdateEpisodeResults(episodes);
        }

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            SearchBoxPrompt.Visibility = Visibility.Hidden;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == null || SearchBox.Text == string.Empty)
            {
                SearchBoxPrompt.Visibility = Visibility.Visible;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SelectionList.SelectAll();
        }
    }
}
