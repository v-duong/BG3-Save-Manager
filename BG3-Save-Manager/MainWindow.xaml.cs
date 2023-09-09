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
            foreach(string directory in Directory.GetDirectories(Properties.Settings.Default.saveFolderPath))
            {
                foreach(string file in Directory.GetFiles(directory))
                {
                    if (file.EndsWith(".lsv"))
                    {
                        var metadata = saveReader.ReadFile(file);
                        if (metadata == null)
                            continue;
                        metadata.FolderName = Path.GetFileName(directory);
                        metadata.FileName = Path.GetFileNameWithoutExtension(file);

                        SaveList.Add(metadata);

                        if (!CharacterFilter.Any(x=>x.UniqueID == metadata.GameSessionId))
                            CharacterFilter.Add(new CharacterFilterCondition(metadata.LeaderName, true, metadata.GameSessionId));
                    }
                }
            }
            SaveListView = CollectionViewSource.GetDefaultView(SaveList);
            SaveListView.Filter = SaveListFilter;
            SaveGrid.ItemsSource = SaveListView;

            CharacterFilterList.DataContext = CharacterFilter;
            SaveFilterList.DataContext = SaveTypeFilter;
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
            var selectedSave = grid?.SelectedItem as SaveMetadata;
            DetailsPanel.DataContext = selectedSave;
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

    }
}
