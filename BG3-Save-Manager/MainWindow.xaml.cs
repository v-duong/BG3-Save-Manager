using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace BG3_Save_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ObservableCollection<SaveMetadata> SaveList { get; set; } = new ObservableCollection<SaveMetadata>();

        public MainWindow()
        {
            InitializeComponent();

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
                        saveReader.ReadFile(file);
                    }
                }
            }
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
    }
}
