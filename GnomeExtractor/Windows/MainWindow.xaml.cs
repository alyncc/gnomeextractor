using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Game;
using GameLibrary;
using Infralution.Localization.Wpf;
using Microsoft.Win32;

namespace GnomeExtractor
{
    public partial class MainWindow : Window
    {
        bool isWindowsXP = false;
        bool isLatestVersion = false;
        bool isUpdatesNeeded = false;
        
        bool isLabelsVertical;
        bool isAutoUpdateEnabled;
        string filePath;
        string lastBackupFileName;
        string appStartupPath = System.IO.Path.GetDirectoryName(Application.ResourceAssembly.Location);
        string[] dataGridNames = new string[] { "dataGridProfessions", "dataGridCombat", "dataGridAttributes" };

        Version latestVersion;
        Statistics mapStatistics;
        GnomanEmpire gnomanEmpire;
        ResourceManager resourceManager;
        Settings settings = new Settings();
        BackgroundWorker bkgw = new BackgroundWorker();
        BackgroundWorker updater = new BackgroundWorker();
        BackgroundWorker backgrWorker = new BackgroundWorker();
        OperatingSystem osInfo = Environment.OSVersion;

        #region WindowHandling
        public MainWindow()
        {
            Globals.Logger.Info("Gnome Extractor is running...");

            if (osInfo.Version.Major == 5 && osInfo.Version.Minor != 0)
                isWindowsXP = true;

            // Read settings from Xml file
            settings.ReadXml();

            // При первом запуске выставляем культуру установленную в компе, при последующих - предыдущую
            // First run changing localization same like in computer
            if (settings.Fields.ProgramLanguage == "")
            {
                string lang = "en-US";
                if (CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "ru")
                    if (File.Exists("ru-RU\\GnomeExtractor.resources.dll"))
                    {
                        lang = "ru-RU";
                        Globals.Logger.Info("Localization ru-RU resource file has been found");
                    }
                
                CultureManager.UICulture = new CultureInfo(lang);
                settings.Fields.ProgramLanguage = lang;
            }
            else
                CultureManager.UICulture = new CultureInfo(settings.Fields.ProgramLanguage);

            Globals.Logger.Debug("Language selected: {0}", CultureManager.UICulture.Name);

            CultureManager.UICultureChanged += new EventHandler(CultureManager_UICultureChanged);
            resourceManager = new ResourceManager("GnomeExtractor.Resources.Loc", Assembly.GetExecutingAssembly());

            InitializeComponent();

            UpdateLanguageMenus();

            // Загружаем настроечки с прошлого запуска
            // Loading settings
            this.WindowState = settings.Fields.LastRunWindowState;
            this.Left = settings.Fields.LastRunLocation.X;
            this.Top = settings.Fields.LastRunLocation.Y;
            this.Width = settings.Fields.LastRunSize.Width;
            this.Height = settings.Fields.LastRunSize.Height;
            Globals.ViewModel.IsCheatsEnabled = settings.Fields.LastRunCheatMode;
            this.isLabelsVertical = settings.Fields.LastRunIsLablesVertical;
            this.tabControl.SelectedIndex = settings.Fields.TabItemSelected;
            this.isAutoUpdateEnabled = settings.Fields.IsAutoUpdateEnabled;

            Globals.Logger.Debug("Settings have been loaded");
            Globals.Logger.Debug("Game initialization...");
            typeof(GnomanEmpire).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(GnomanEmpire.Instance, null);
            if (GnomanEmpire.Instance.Graphics.IsFullScreen) GnomanEmpire.Instance.Graphics.ToggleFullScreen();
            GnomanEmpire.Instance.AudioManager.MusicVolume = 0;
            Globals.Logger.Debug("Game initialized");

            Globals.Initialize();

            if (settings.Fields.IsAutoUpdateEnabled) CheckingUpdates(false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ControlStates();

            Globals.Logger.Info("Running is complete");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Globals.Logger.Debug("Trying to close program...");
            if (backgrWorker.IsBusy) e.Cancel = true;
            Globals.Logger.Info("Program closes...");
            settings.Fields.LastRunWindowState = this.WindowState;
            settings.Fields.LastRunLocation = new Point(Left, Top);
            settings.Fields.LastRunSize = new Size(Width, Height);
            settings.Fields.LastRunCheatMode = Globals.ViewModel.IsCheatsEnabled;
            settings.Fields.LastRunIsLablesVertical = !this.isLabelsVertical;
            settings.Fields.TabItemSelected = tabControl.SelectedIndex;
            settings.Fields.IsAutoUpdateEnabled = isAutoUpdateEnabled;
            Globals.Logger.Debug("Settings have been saved");
            settings.WriteXml();
            if (gnomanEmpire != null) gnomanEmpire = null;

            // Отправляем сейвы обратно, затираем их в локальной папке
            // Move savefiles in /worlds forder
            if (isWindowsXP)
            {
                Globals.Logger.Debug("Looking for saves in AppDir...");
                DirectoryInfo dir = new DirectoryInfo(appStartupPath);
                FileInfo[] fi = dir.GetFiles("*.sav");
                foreach (FileInfo file in fi)
                {
                    File.Copy(file.FullName, GnomanEmpire.SaveFolderPath("\\Worlds") + file.Name, true);
                    File.Delete(file.FullName);
                    Globals.Logger.Info("Save file {0} has been moved to the Worlds path", file.Name);
                }
                Globals.Logger.Debug("All save files has been moved");
            }

            Globals.Logger.Info("Program is closed");
        }
        #endregion

        #region ButtonClickHandlers
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("About button has been clicked");
            About about = new About();
            about.Owner = this;
            about.Show();
        }

        private void fastEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FastEditor fastEditor = new FastEditor();
            fastEditor.Owner = this;
            fastEditor.ShowDialog();

            if (fastEditor.IsOkClicked)
            {
                Globals.Logger.Trace("FastEditor OK button has been clicked");

                if (fastEditor.fixedModeRadioButton.IsChecked == true)
                {
                    foreach (var gnome in Globals.ViewModel.Gnomes)
                    {
                        if (fastEditor.isLaborNeededCheckBox.IsChecked == true)
                            foreach (var skill in gnome.LaborSkills)
                                skill.Level = fastEditor.Value;
                        if (fastEditor.isCombatNeededCheckBox.IsChecked == true)
                            foreach (var skill in gnome.CombatSkills)
                                skill.Level = fastEditor.Value;
                        if (fastEditor.isAttributesNeededCheckBox.IsChecked == true)
                            foreach (var attribute in gnome.Attributes)
                                attribute.Level = fastEditor.Value;
                    }
                }
            }
        }

        private void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("Open button has been clicked");
            OpenFileDialog openDlg = new OpenFileDialog();

            if (isWindowsXP)
            {
                Globals.Logger.Info("Copying save files to AppPath...");
                openDlg.InitialDirectory = appStartupPath;
                DirectoryInfo dir = new DirectoryInfo(GnomanEmpire.SaveFolderPath("Worlds"));
                FileInfo[] fi = dir.GetFiles("*.sav");
                foreach (FileInfo file in fi)
                {
                    Globals.Logger.Debug("Save file{0} have been temporary copied to Program directory", file.Name);
                    File.Copy(file.FullName, appStartupPath + "\\" + file.Name, true);

                }
                if (!File.Exists("fixed7z.dll"))
                {
                    File.Copy("7z.dll", "fixed7z.dll");
                    Globals.Logger.Debug("Fixed 7z.dll has been created");
                }
                SevenZip.SevenZipExtractor.SetLibraryPath("fixed7z.dll");
            }
            else
            {
                SevenZip.SevenZipExtractor.SetLibraryPath("7z.dll");
                openDlg.InitialDirectory = System.IO.Path.GetFullPath(GnomanEmpire.SaveFolderPath("Worlds\\"));
            }

            openDlg.Filter = "Gnomoria Save Files (*.sav)|*.sav";

            Globals.Logger.Debug("Open file dialog creating...");
            if (openDlg.ShowDialog() == true)
            {
                Globals.Logger.Debug("Open file dialog has been closed");

                DisableControlsForBackgroundWorker();
                filePath = openDlg.FileName;

                // Cleaning before loading
                this.DataContext = null;
                wrapPanelStatistics.DataContext = null;
                
                Globals.ViewModel.Gnomes.Clear();
                Globals.ViewModel.Professions.Clear();

                backgrWorker.DoWork += new DoWorkEventHandler(LoadGame_BackgroundWorker);
                backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoadGameCompleted_BackgroundWorker);
                backgrWorker.RunWorkerAsync(openDlg.SafeFileName);
            }
        }

        private void LoadGameCompleted_BackgroundWorker(object sender, RunWorkerCompletedEventArgs e)
        {
            this.DataContext = Globals.ViewModel;

            mapStatistics = new Statistics(gnomanEmpire);
            wrapPanelStatistics.DataContext = mapStatistics;

            GridState();
            backgrWorker.DoWork -= new DoWorkEventHandler(LoadGame_BackgroundWorker);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(LoadGameCompleted_BackgroundWorker);

            statusBarLabel.Content = resourceManager.GetString("chosenFile") + " " + System.IO.Path.GetFileName(filePath) +
                    " " + resourceManager.GetString("worldName") + " " + gnomanEmpire.World.AIDirector.PlayerFaction.Name;

            Globals.Logger.Info("World initialization complete");
            ControlStates();
        }

        private void LoadGame_BackgroundWorker(object sender, DoWorkEventArgs e)
        {
            Globals.Logger.Info("World initialization...");
            Globals.Logger.Info("Loading {0} save file...", e.Argument);

            GnomanEmpire.Instance.LoadGame((string)e.Argument);

            gnomanEmpire = GnomanEmpire.Instance;

            // Looking for Professions
            Globals.ViewModel.Professions.Add(new Profession("Custom"));
            Globals.ViewModel.Professions[0].AllowedSkills.ClearAll();
            foreach (var profession in gnomanEmpire.Fortress.Professions)
                Globals.ViewModel.Professions.Add(profession);

            // Перебор гномов на карте
            // Looking for gnomes at the map
            var gnomeIndex = 0;
            for (int level = 0; level < gnomanEmpire.Map.Levels.Length; level++)
                for (int row = 0; row < gnomanEmpire.Map.Levels[0].Length; row++)
                    for (int col = 0; col < gnomanEmpire.Map.Levels[0][0].Length; col++)
                        for (int num = 0; num < gnomanEmpire.Map.Levels[level][row][col].Characters.Count; num++)
                            if (gnomanEmpire.Map.Levels[level][row][col].Characters[num].RaceID == RaceID.Gnome)
                            {
                                var gnome = gnomanEmpire.Map.Levels[level][row][col].Characters[num];

                                Globals.ViewModel.Gnomes.Add(new Gnome(gnome, level, row, col, num, gnomeIndex));

                                gnomeIndex++;
                            }
        }

        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("Save button has been clicked");
            DisableControlsForBackgroundWorker();

            backgrWorker.DoWork += new DoWorkEventHandler(SaveGame_BackgroundWorker);
            backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SaveGameCompleted_BackgroundWorker);
            backgrWorker.RunWorkerAsync();
        }

        private void SaveGameCompleted_BackgroundWorker(object sender, RunWorkerCompletedEventArgs e)
        {
            backgrWorker.DoWork -= new DoWorkEventHandler(SaveGame_BackgroundWorker);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(SaveGameCompleted_BackgroundWorker);

            statusBarLabel.Content = resourceManager.GetString("saveDoneMessage") + " " + gnomanEmpire.CurrentWorld + ", " +
                                     resourceManager.GetString("backupDoneMessage") + " " + lastBackupFileName;

            Globals.Logger.Info("Save complete");
            ControlStates();
        }

        private void SaveGame_BackgroundWorker(object sender, DoWorkEventArgs e)
        {
            Globals.Logger.Info("World saving...");

            var dir = GnomanEmpire.SaveFolderPath("Backup\\");
            Directory.CreateDirectory(dir);
            lastBackupFileName = DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" +
                                 DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + "_" + gnomanEmpire.CurrentWorld;
            File.Copy(filePath, dir + lastBackupFileName, true);
            Globals.Logger.Info("Backup file created at" + dir + lastBackupFileName);

            foreach (var unsavedGnome in Globals.ViewModel.Gnomes)
            {
                var gnome = gnomanEmpire.Map.Levels[unsavedGnome.Level][unsavedGnome.Row][unsavedGnome.Column].Characters[unsavedGnome.Position];

                // Setting personal info
                gnome.SetName(unsavedGnome.Name);

                // Setting skill/attribute levels
                foreach (var laborSkill in unsavedGnome.LaborSkills)
                    gnome.SetSkillLevel(laborSkill.Type, laborSkill.Level);
                foreach (var combatSkill in unsavedGnome.CombatSkills)
                    gnome.SetSkillLevel(combatSkill.Type, combatSkill.Level);
                foreach (var attribute in unsavedGnome.Attributes)
                    gnome.SetAttributeLevel(attribute.Type, (int)attribute.Level);

                // Setting professions
                gnome.Mind.Profession = unsavedGnome.Profession;
                for (int i = gnomanEmpire.Fortress.Professions.Count; i > 0; i--)
                    gnomanEmpire.Fortress.Professions.RemoveAt(i - 1);
                foreach (var profession in Globals.ViewModel.Professions)
                    if (profession.Title != "Custom")
                        gnomanEmpire.Fortress.Professions.Add(profession);

                Globals.Logger.Debug("Gnome {0} written", gnome.Name());
            }

            gnomanEmpire.SaveGame();
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("Exit button clicked");

            Close();
        }

        private void russianMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("Russian language button clicked");

            if (File.Exists("ru-RU\\GnomeExtractor.resources.dll"))
            {
                CultureManager.UICulture = new CultureInfo("ru-RU");
                settings.Fields.ProgramLanguage = "ru-RU";
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");

                Globals.Logger.Info("Localization changed to Russian");
            }
            else
            {
                MessageBox.Show(resourceManager.GetString("localizationNotFound"));

                Globals.Logger.Error("Localization file for Russian language is not found");
            }
        }

        private void englishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("English language button clicked");

            CultureManager.UICulture = new CultureInfo("en-US");
            settings.Fields.ProgramLanguage = "en-US";
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            Globals.Logger.Info("Localization changed to English");
        }

        private void cheatModeMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("Cheat mode button has been clicked");

            Globals.ViewModel.IsCheatsEnabled = !Globals.ViewModel.IsCheatsEnabled;
            ControlStates();
            GridState();

            Globals.Logger.Info("Cheat mode {0}", (Globals.ViewModel.IsCheatsEnabled) ? "enabled" : "disabled");
        }

        private void updatingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("Updating button has been clicked");

            CheckingUpdates(true);
        }

        private void CheckingUpdates(bool isAnswerNeeded)
        {
            updatingMenuItem.IsEnabled = false;
            updater.DoWork += new DoWorkEventHandler(updater_Update);
            updater.RunWorkerCompleted += new RunWorkerCompletedEventHandler(updater_UpdateCompleted);
            updater.RunWorkerAsync(isAnswerNeeded);
        }

        private void updater_UpdateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            updater.DoWork -= new DoWorkEventHandler(updater_Update);
            updater.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(updater_UpdateCompleted);

            if (isLatestVersion)
            {
                if ((bool)e.Result) MessageBox.Show(resourceManager.GetString("latestVersion"));
                Globals.Logger.Info("Latest version is installed");
            }
            else if (isUpdatesNeeded)
            {
                Globals.Logger.Info("Newest version found");
                WebRequest request;

                if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "ru")
                    request = WebRequest.Create(new Uri("http://gnomex.tk/version/changesru"));
                else
                    request = WebRequest.Create(new Uri("http://gnomex.tk/version/changes"));
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                var changes = reader.ReadToEnd();

                if (MessageBoxResult.Yes == MessageBox.Show(resourceManager.GetString("newestVersion") + " " + latestVersion + ", " +
                    resourceManager.GetString("downloadNewVersion") + "\n\n" + changes, resourceManager.GetString("updateDialogCaption"), MessageBoxButton.YesNo)) Process.Start("http://gnomex.tk");
            }
            isLatestVersion = false;
            isUpdatesNeeded = false;
            Globals.Logger.Info("Updating is complete");
            updatingMenuItem.IsEnabled = true;
        }

        private void updater_Update(object sender, DoWorkEventArgs e)
        {
            Globals.Logger.Info("Updating is running...");
            WebRequest request = WebRequest.Create(new Uri("http://gnomex.tk/version/current"));
            WebResponse response;

            try
            {
                response = request.GetResponse();

                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                latestVersion = new Version(reader.ReadToEnd());
                Globals.Logger.Debug("Latest version is {0}", latestVersion);
                reader.Close();
                response.Close();

                if (Globals.ProgramVersion.CompareTo(latestVersion) < 0)
                    isUpdatesNeeded = true;
                else
                    isLatestVersion = true;
            }
            catch (WebException)
            {
                MessageBox.Show(resourceManager.GetString("connectionError"));
            }

            

            if ((bool)e.Argument) e.Result = true;
            else e.Result = false;
        }

        private void autoUpdatingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Debug("Autoupdating button button has been clicked");

            isAutoUpdateEnabled = !isAutoUpdateEnabled;
            Globals.Logger.Info("Auto updating is {0}", (isAutoUpdateEnabled) ? "enabled" : "disabled");
            ControlStates();
        }

        private void exportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.Logger.Info("Export is running...");

            DisableControlsForBackgroundWorker();

            backgrWorker.DoWork += new DoWorkEventHandler(backgrWorker_ExportToCSV);
            backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgrWorker_ExportToCSVCompleted);
            backgrWorker.RunWorkerAsync();
        }

        private void backgrWorker_ExportToCSV(object sender, DoWorkEventArgs e)
        {
            // Opening file stream
            Directory.CreateDirectory(Globals.AppDataPath);
            var path = Globals.AppDataPath + "\\export.csv";
            FileStream fs = new FileStream(path, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            // Writing first row
            sw.Write("Name;Profession;");
            foreach (var skill in SkillDef.AllLaborSkills())
                sw.Write(skill.ToString() + ";");
            foreach (var skill in SkillDef.AllCombatSkills())
                sw.Write(skill.ToString() + ";");

            // Writing gnomes
            foreach (var gnome in Globals.ViewModel.Gnomes)
            {
                sw.Write("\n" + gnome.Name + ";" + gnome.Profession.Title + ";");
                foreach (var skill in gnome.LaborSkills)
                    sw.Write(skill.Level + ";");
                foreach (var skill in gnome.CombatSkills)
                    sw.Write(skill.Level + ";");
                foreach (var attribute in gnome.Attributes)
                    sw.Write(attribute.Level + ";");
            }
            sw.Close();
            Globals.Logger.Info("File {0} is created", path);
        }

        private void backgrWorker_ExportToCSVCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            backgrWorker.DoWork -= new DoWorkEventHandler(backgrWorker_ExportToCSV);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgrWorker_ExportToCSVCompleted);

            Globals.Logger.Info("Export is complete");
            statusBarLabel.Content = resourceManager.GetString("exportDoneMessage");

            Process.Start(Globals.AppDataPath);

            ControlStates();
        }
        #endregion

        #region State handling
        private void ControlStates()
        {
            cheatsMenuItem.Visibility = (settings.Fields.IsCheatsEnabled) ? Visibility.Visible : Visibility.Collapsed;
            if (!settings.Fields.IsCheatsEnabled) settings.Fields.LastRunCheatMode = false;
            fastEditMenuItem.IsEnabled = (gnomanEmpire != null && Globals.ViewModel.IsCheatsEnabled);
            saveMenuItem.IsEnabled = exportMenuItem.IsEnabled = professionsEditingGroupBox.IsEnabled = (gnomanEmpire != null && !backgrWorker.IsBusy);
            openMenuItem.IsEnabled = !(backgrWorker.IsBusy);
            cheatModeMenuItem.IsChecked = Globals.ViewModel.IsCheatsEnabled;
            autoUpdatingMenuItem.IsChecked = isAutoUpdateEnabled;
            progressBarMain.Visibility = (backgrWorker.IsBusy) ? Visibility.Visible : Visibility.Hidden;
        }

        private void DisableControlsForBackgroundWorker()
        {
            fastEditMenuItem.IsEnabled = openMenuItem.IsEnabled = saveMenuItem.IsEnabled = 
                exportMenuItem.IsEnabled = professionsEditingGroupBox.IsEnabled = false;
            progressBarMain.Visibility = System.Windows.Visibility.Visible;
        }

        private void GridState()
        {
            if (gnomanEmpire != null)
            {
                var tempIndex = tabControl.SelectedIndex;
                tabControl.SelectedIndex = 0;
                dataGridProfessions.UpdateLayout();
                for (int i = Globals.FirstColumnCount; i < dataGridProfessions.Columns.Count; i++)
                    dataGridProfessions.Columns[i].IsReadOnly = !Globals.ViewModel.IsCheatsEnabled;
                tabControl.SelectedIndex = 1;
                dataGridCombat.UpdateLayout();
                for (int i = Globals.FirstColumnCount; i < dataGridCombat.Columns.Count; i++)
                    dataGridCombat.Columns[i].IsReadOnly = !Globals.ViewModel.IsCheatsEnabled;
                tabControl.SelectedIndex = tempIndex;
            }
        }

        private void UpdateLanguageMenus()
        {
            string lang = CultureManager.UICulture.TwoLetterISOLanguageName.ToLower();
            russianMenuItem.IsChecked = (lang == "ru");
            englishMenuItem.IsChecked = (lang == "en");
        }

        private void CultureManager_UICultureChanged(object sender, EventArgs e)
        {
            UpdateLanguageMenus();
        }
        #endregion

        #region DataGrids handling (is not logged)
        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.DisplayIndex == Globals.FirstColumnCount - 1) e.Handled = true;

            if (e.Column.DisplayIndex > Globals.FirstColumnCount - 1)
                e.Column.SortDirection = ListSortDirection.Ascending;
        }

        private void DataGridCell_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
            if (e.ChangedButton == MouseButton.Right || (e.ChangedButton == MouseButton.Left && !Globals.ViewModel.IsCheatsEnabled) && tabControl.SelectedIndex == 0)
            {
                // Getting indexes
                var cell = sender as DataGridCell;
                if (cell.Column.DisplayIndex > Globals.FirstColumnCount - 1)
                {
                    //var skillIndex = cell.Column.DisplayIndex - Globals.FirstColumnCount;
                    var skillIndex = dataGridProfessions.Columns.IndexOf(cell.Column) - Globals.FirstColumnCount;
                    var gnome = cell.DataContext as Gnome;
                    if (skillIndex > gnome.LaborSkills.Count) return;

                    // Changing skills
                    gnome.LaborSkills[skillIndex].IsAllowed = !gnome.LaborSkills[skillIndex].IsAllowed;
                    if (gnome.LaborSkills[skillIndex].IsAllowed)
                        gnome.Profession.AllowedSkills.AddSkill(gnome.LaborSkills[skillIndex].Type);
                    else
                        gnome.Profession.AllowedSkills.RemoveSkill(gnome.LaborSkills[skillIndex].Type);
                    var comboBox = professionsColumn.GetCellContent(DataGridRow.GetRowContainingElement(cell)) as ComboBox;
                    comboBox.SelectedIndex = 0;
                }
            }
            else
            {
                //var cell = sender as DataGridCell;
                //cell.IsSelected = true;
                //cell.IsEditing = true;
            }
        }

        private void DataGridRowHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRowHeader header = sender as DataGridRowHeader;
            if (header != null)
            {
                DataGrid dataGrid = FindName(dataGridNames[tabControl.SelectedIndex]) as DataGrid;
                DataGridRow row = DataGridRow.GetRowContainingElement(sender as DataGridRowHeader);

                // Reading
                DictionaryEntry[] values = new DictionaryEntry[dataGrid.Columns.Count - Globals.FirstColumnCount];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i].Key = i + Globals.FirstColumnCount;
                    values[i].Value = Int32.Parse((dataGrid.Columns[i + Globals.FirstColumnCount].GetCellContent(row) as TextBlock).Text);
                }

                // Sorting
                for (int i = 0; i < values.Length - 1; i++)
                    for (int j = i + 1; j < values.Length; j++)
                        if ((int)values[i].Value < (int)values[j].Value)
                        {
                            var temp = values[i];
                            values[i] = values[j];
                            values[j] = temp;
                        }

                // Reordering
                for (int i = 0; i < values.Length; i++)
                {
                    dataGrid.Columns[(int)values[i].Key].DisplayIndex = i + Globals.FirstColumnCount;

                    e.Handled = true;
                }
            }
        }

        private void DataGridColumnHeaderProfessions_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var header = e.Source as DataGridColumnHeader;
            header.ToolTip = resourceManager.GetString(header.Content.ToString().Replace(" ", String.Empty));
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.DisplayIndex == Globals.FirstColumnCount - 1)
            {
                //var value = (e.EditingElement as ComboBox).SelectedValue.ToString();
            }

            // Проверяем является ли выделенная ячейка именем
            // Checking if cell is name
            if (e.Column.DisplayIndex == Globals.FirstColumnCount - 2)
            {
                var value = (e.EditingElement as TextBox).Text;

                if (value.Length > 24)
                    value = value.Substring(0, 24);

                (e.EditingElement as TextBox).Text = value;
            }
            if (e.Column.DisplayIndex > Globals.FirstColumnCount - 1)
            {
                var value = (e.EditingElement as TextBox).Text;

                long num;
                if (Int64.TryParse(value, out num))
                {
                    if (Int64.Parse(value) > 5000)
                        (e.EditingElement as TextBox).Text = "5000";
                    else if (Int64.Parse(value) < 5)
                        (e.EditingElement as TextBox).Text = "5";
                }
                else
                    (e.EditingElement as TextBox).Text = "5";
            }
        }

        private void DataGridProfessionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            if (dataGridProfessions.SelectedCells.Count == 0) return;
            var profession = (Profession)e.AddedItems[0];
            Globals.Logger.Trace("Professions combobox selection changed to {0}", profession.Title);
            var gnome = Globals.ViewModel.Gnomes[((Gnome)dataGridProfessions.SelectedCells[0].Item).ID];
            if (profession.Title == "Custom") profession.AllowedSkills = gnome.Profession.AllowedSkills;
            gnome.Profession = profession;
            gnome.SetAllowedSkills(profession.AllowedSkills);

            Globals.Logger.Trace("Gnome {0} profession has been changed to {1}", gnome.Name, Globals.ViewModel.Gnomes[0].Profession.Title);
        }
        #endregion

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Globals.Logger.Trace("TabControl.SelectedIndex is changed to {0}", tabControl.SelectedIndex);
        }

        private void AddProfessionButton_Click(object sender, RoutedEventArgs e)
        {
            var name = professionsComboBox.Text;

            // Abort if already exists
            foreach (var profession in Globals.ViewModel.Professions)
            {
                if (profession.Title == name)
                {
                    MessageBox.Show(resourceManager.GetString("professionAlreadyExists"));
                    return;
                }
            }

            // Adding profession
            Profession newProfession = new Profession(name);
            newProfession.AllowedSkills.ClearAll();
            foreach (var skill in Globals.ViewModel.Skills)
                if (skill.IsAllowed)
                    newProfession.AllowedSkills.AddSkill(skill.Type);
            Globals.ViewModel.Professions.Add(newProfession);

            // Refreshing combobox
            professionsComboBox.ItemsSource = null;
            professionsComboBox.ItemsSource = Globals.ViewModel.Professions;
        }

        private void RemoveProfessionButton_Click(object sender, RoutedEventArgs e)
        {
            var profession = professionsComboBox.SelectedItem as Profession;
            if (profession.Title == "Custom") return;
            foreach (var gnome in Globals.ViewModel.Gnomes)
            {
                if (gnome.Profession.Title == profession.Title)
                {
                    var newProfession = new Profession("Custom");
                    newProfession.AllowedSkills = gnome.Profession.AllowedSkills;
                    gnome.Profession = newProfession;
                }
            }
            Globals.ViewModel.Professions.Remove(profession);

            professionsComboBox.ItemsSource = null;
            professionsComboBox.ItemsSource = Globals.ViewModel.Professions;
        }

        private void readGnomeButton_Click(object sender, RoutedEventArgs e)
        {
            var gnome = gnomesComboBox.SelectedItem as Gnome;
            Globals.ViewModel.Skills = gnome.GetClonedSkills();
        }

        private void professionsComboBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".IndexOf(e.Text) < 0;
        }

        private void professionsComboBox_PreviewDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void professionsComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.Insert))
                e.Handled = true;
        }
    }
}