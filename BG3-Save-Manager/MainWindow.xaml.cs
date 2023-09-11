using System;
using System.Windows;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace BG3_Save_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<SaveMetadata> SaveList { get; set; } = new ObservableCollection<SaveMetadata>();
        private ICollectionView SaveListView { get; set; }
        private ObservableCollection<CharacterFilterCondition> CharacterFilter { get; set; } = new ObservableCollection<CharacterFilterCondition>();
        private ObservableCollection<FilterCondition> SaveTypeFilter { get; set; } = new ObservableCollection<FilterCondition>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeFilter();

            CharacterFilterList.CheckboxHandler = OnFilterChange;
            SaveFilterList.CheckboxHandler = OnFilterChange;

            if (string.IsNullOrEmpty(Properties.Settings.Default.saveFolderPath))
            {
                Properties.Settings.Default.saveFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Larian Studios\\Baldur\'s Gate 3\\PlayerProfiles\\Public\\Savegames\\Story";
            }

            if (!Directory.Exists(Properties.Settings.Default.saveFolderPath))
            {
                OpenFolderSelect();
            } else
            {
                ReadSavesFromFolder();
            }
        }

        private void InitializeFilter()
        {
            SaveTypeFilter.Add(new FilterCondition("Manual", true));
            SaveTypeFilter.Add(new FilterCondition("Quick", true));
            SaveTypeFilter.Add(new FilterCondition("Auto", true));
        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            OpenFolderSelect();
        }

        private void ReadSavesFromFolder()
        {
            var saveReader = new LarianReader();
            var noMetadataList = new List<string>();
            var missingFileList = new List<string>();

            foreach (string directory in Directory.GetDirectories(Properties.Settings.Default.saveFolderPath))
            {
                bool foundLSV = false;

                foreach(string file in Directory.GetFiles(directory))
                {
                    if (file.EndsWith(".lsv"))
                    {
                        var metadata = saveReader.ReadFile(file);
                        if (metadata == null)
                        {
                            noMetadataList.Add(directory);
                            continue;
                        }
                        metadata.FolderName = Path.GetFileName(directory);
                        metadata.FileName = Path.GetFileNameWithoutExtension(file);

                        SaveList.Add(metadata);

                        if (!CharacterFilter.Any(x=>x.UniqueID == metadata.GameSessionId))
                            CharacterFilter.Add(new CharacterFilterCondition(metadata.LeaderName, true, metadata.GameSessionId));

                        foundLSV = true;
                    }
                }

                if (!foundLSV)
                    missingFileList.Add(directory);
            }

            if (missingFileList.Count > 0)
                ShowMissingFilesWarning(missingFileList);

            if (noMetadataList.Count > 0)
                ShowLoadFailWarning(noMetadataList);

            SaveListView = CollectionViewSource.GetDefaultView(SaveList);
            SaveListView.Filter = SaveListFilter;
            SaveGrid.ItemsSource = SaveListView;

            CharacterFilterList.DataContext = CharacterFilter;
            SaveFilterList.DataContext = SaveTypeFilter;
        }

        private void ShowMissingFilesWarning(List<string> directoryList)
        {
            var res = MessageBox.Show($"There are {directoryList.Count} directories with a missing save data (.lsv) file. Would you like to permanently delete these directories?", "Invalid Save Directories", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            foreach (var save in directoryList)
            {
                if (Directory.Exists(save))
                    Directory.Delete(save, true);
            }

            MessageBox.Show($"Successfully deleted {directoryList.Count} save files.", "Deletion Complete");
        }

        private void ShowLoadFailWarning(List<string> directoryList)
        {
            var res = MessageBox.Show($"There were {directoryList.Count} directories that could not be loaded. This could be due to version differences or file corruption.", "Error Loading", MessageBoxButton.OK);
        }

        private void OpenFolderSelect()
        {
            var fileDialog = new VistaFolderBrowserDialog()
            {
                Description = "Select your Baldur's Gate 3 Save Folder",
                UseDescriptionForTitle = true,
                Multiselect = false
            };

            if (fileDialog.ShowDialog() == true)
            {
                Properties.Settings.Default.saveFolderPath = fileDialog.SelectedPath;
            }
        }

        private void OnFilterChange(FilterCondition filter)
        {
            SaveListView.Refresh();
        }

        private bool SaveListFilter(object obj)
        {
            var save = obj as SaveMetadata;
            if (save == null) return false;

            var charFilter = CharacterFilter.FirstOrDefault(x => x?.UniqueID == save.GameSessionId, null);
            if (charFilter != null && !charFilter.IsEnabled)
                return false;

            if (!SaveTypeFilter[(int)save.SaveGameType].IsEnabled)
                return false;

            return true;
        }

        private void SaveGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var grid = sender as DataGrid;

            if (grid == null)
                return;

            var selectedSave = grid.SelectedItem as SaveMetadata;
            DetailsPanel.DataContext = selectedSave;

            if (grid.SelectedItems.Count > 1)
            {
                Delete_Button.Content = $"Delete {grid.SelectedItems.Count} Saves";
            } else
            {
                Delete_Button.Content = "Delete Save";
            }
        }

        private void ColumnCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkbox)
                return;

            DataGridColumn target;

            switch(checkbox.Name)
            {
                case "GameVersionCheck":
                    target = GameVersionColumn;
                    break;
                case "ThumbnailCheck":
                    target = ThumbnailColumn;
                    break;
                default:
                    return;
            }

            target.Visibility = (bool)checkbox.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            if (SaveGrid.SelectedItems == null)
                return;


            var res = MessageBox.Show($"This will permanently delete {SaveGrid.SelectedItems.Count} save files. Are you sure?", "Save Deletion", MessageBoxButton.YesNo);

            if (res == MessageBoxResult.No)
                return;

            var listCopy = SaveGrid.SelectedItems.Cast<SaveMetadata>().ToList();
            SaveGrid.SelectedItems.Clear();

            foreach(var save in listCopy)
            {
                if (Directory.Exists(save.FullFolderPath))
                {
                    SaveList.Remove(save);
                    Directory.Delete(save.FullFolderPath, true);
                }
            }

            MessageBox.Show($"Successfully deleted {listCopy.Count} save files.","Deletion Complete");

            SaveListView.Refresh();
        }
    }
}
