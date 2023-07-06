using Prepper.Abstractions;
using Prepper.Preps;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Prepper
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private List<IPrep> currentPrep;
        public List<IPrep> CurrentPrep
        {
            get => currentPrep;
            private set
            {
                currentPrep = value;
                OnPropertyChanged();
            }
        }

        private List<IReferenceProvider> availableProviders;
        public List<IReferenceProvider> AvailableProviders
        {
            get { return availableProviders; }
            set
            {
                availableProviders = value;
                UpdateDisplayNames();
                OnPropertyChanged();
            }
        }

        private void UpdateDisplayNames()
        {
            List<string> newNames = new();
            availableProvidersDisplayNames ??= new List<string>();
            AvailableProvidersDisplayNames.AddRange(AvailableProviders.Select(ap => ap.GetProviderName()));
        }

        private List<string> availableProvidersDisplayNames;
        public List<string> AvailableProvidersDisplayNames
        {
            get
            {
                availableProviders ??= new List<IReferenceProvider>();
                return availableProvidersDisplayNames;
            }
            private set
            {
                availableProvidersDisplayNames = value;
                OnPropertyChanged();
            }
        }

        private IReferenceProvider selectedProvider;
        public IReferenceProvider SelectedProvider
        {
            get { return selectedProvider; }
            set
            {
                selectedProvider = value;
                SelectedProviderDisplayName = selectedProvider.GetProviderName();
                OnPropertyChanged();
            }
        }

        private string selectedProviderDisplayName;
        public string SelectedProviderDisplayName
        {
            get { return selectedProviderDisplayName ?? ""; }
            private set
            {
                selectedProviderDisplayName = value;
                OnPropertyChanged();
            }
        }

        private List<IEpisode>? currentPrepList;
        public List<IEpisode>? CurrentPrepList
        {
            get { return currentPrepList; }
            set
            {
                currentPrepList = value;
                OnPropertyChanged();
            }
        }

        private List<string>? filesToPrepList;
        public List<string>? FilesToPrepList
        {
            get { return filesToPrepList; }
            set
            {
                filesToPrepList = value;
                OnPropertyChanged();
            }
        }

        private string selectedDirectory;
        public string SelectedDirectory
        {
            get => selectedDirectory;
            set
            {
                selectedDirectory = value;
                OnPropertyChanged();
            }
        }           

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel(List<IReferenceProvider> providers)
        {
            AvailableProviders = providers;
            if (availableProviders == null || availableProviders?.Count <= 0) throw new Exception("At least one provider is necessary for Prep!");
            if (availableProviders != null) SelectedProvider = availableProviders[0];
            filesToPrepList = new List<string>();
        }

        public Task DoPrep() => Task.Run(action: DoPrepWork);

        public void AddToPrep(object? sender, AddToPrepEventArgs e)
        {
            if (currentPrepList == null)
            {
                CurrentPrepList = e.Episodes;
            }
            else
            {
                List<IEpisode> listToUpdate = currentPrepList;
                listToUpdate.AddRange(e.Episodes);
                CurrentPrepList = null;
                CurrentPrepList = listToUpdate;
            }
        }

        public void LoadFilesFromDirectory()
        {
            if (string.IsNullOrWhiteSpace(selectedDirectory)) return;
            var currentFiles = FilesToPrepList;
            foreach(string file in Directory.EnumerateFiles(selectedDirectory))
            {
                var fullFilePath = Path.Combine(selectedDirectory, file);
                if (!currentFiles!.Contains(fullFilePath))
                {
                    currentFiles.Add(fullFilePath);
                }
                FilesToPrepList = null;
                FilesToPrepList = currentFiles;
            }
        }

        public void PreparePrep()
        {
            if (currentPrepList == null) return;
            List<IPrep> preps = new();
            for (int i = 0; i < currentPrepList.Count; i++)
            {
                try
                {
                    if (filesToPrepList != null) preps.Add(PrepFactory.CreatePrep(true, true, filesToPrepList[i], currentPrepList[i]));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception during Prepare");
                    break;
                }
            }
            if (preps.Count > 0) currentPrep = preps;
        }

        private void DoPrepWork()
        {
            if (currentPrep == null) return;
            currentPrep.ForEach(p => p.DoPrep());
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #region Lists Management

        internal void AutoReorderFiles()
        {
            List<string>? files = FilesToPrepList;
            files = files?.AutoReorderFileListMatchingEpisodes(currentPrepList!, true, true);
            FilesToPrepList = null;
            FilesToPrepList = files;
        }

        internal void ReorderFile(string droppedData, string target)
        {
            int removedIdx = FilesToPrepList!.IndexOf(droppedData);
            int targetIdx = FilesToPrepList.IndexOf(target);

            if (removedIdx < targetIdx)
            {
                FilesToPrepList.Insert(targetIdx + 1, droppedData);
                FilesToPrepList.RemoveAt(removedIdx);
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (FilesToPrepList.Count + 1 > remIdx)
                {
                    FilesToPrepList.Insert(targetIdx, droppedData);
                    FilesToPrepList.RemoveAt(remIdx);
                }
            }
        }

        internal void MoveFileDown(int selectedIndex)
        {
            if (selectedIndex == -1) return;
            List<string>? files = filesToPrepList;
            string? file = files?[selectedIndex];
            int newIndex = selectedIndex + 1;
            files?.RemoveAt(selectedIndex);
            if (files != null && file != null) files?.Insert(newIndex, file);
            FilesToPrepList = null;
            FilesToPrepList = files;
        }

        internal void MovePrepUp(int selectedIndex)
        {
            if (selectedIndex == -1) return;
            List<IEpisode>? preps = currentPrepList;
            IEpisode? prep = preps?[selectedIndex];
            int newIndex = selectedIndex - 1;
            preps?.RemoveAt(selectedIndex);
            if (preps != null && prep != null) preps?.Insert(newIndex, prep);
            CurrentPrepList = null;
            CurrentPrepList = preps;
        }

        internal void MoveFileUp(int selectedIndex)
        {
            if (selectedIndex == -1) return;
            List<string>? files = filesToPrepList;
            string? file = files?[selectedIndex];
            int newIndex = selectedIndex - 1;
            files?.RemoveAt(selectedIndex);
            if (files != null && file != null) files?.Insert(newIndex, file);
            FilesToPrepList = null;
            FilesToPrepList = files;
        }

        internal void MovePrepDown(int selectedIndex)
        {
            if (selectedIndex == -1) return;
            List<IEpisode>? preps = currentPrepList;
            IEpisode? prep = preps?[selectedIndex];
            int newIndex = selectedIndex + 1;
            preps?.RemoveAt(selectedIndex);
            if (preps != null && prep != null) preps?.Insert(newIndex, prep);
            CurrentPrepList = null;
            CurrentPrepList = preps;
        }

        internal void RemoveFile(int selectedIndex)
        {
            if (selectedIndex == -1) return;
            List<string>? files = filesToPrepList;
            files?.RemoveAt(selectedIndex);
            FilesToPrepList = null;
            FilesToPrepList = files;
        }

        internal void RemovePrep(int selectedIndex)
        {
            if (selectedIndex == -1) return;
            List<IEpisode>? preps = currentPrepList;
            preps?.RemoveAt(selectedIndex);
            CurrentPrepList = null;
            CurrentPrepList = preps;
        }

        internal void RemoveAllFiles()
        {
            List<string>? files = filesToPrepList;
            files?.Clear();
            FilesToPrepList = null;
            FilesToPrepList = files;
        }

        internal void RemoveAllPreps()
        {
            List<IEpisode>? preps = currentPrepList;
            preps?.Clear();
            CurrentPrepList = null;
            CurrentPrepList = preps;
        }

        #endregion
    }
}
