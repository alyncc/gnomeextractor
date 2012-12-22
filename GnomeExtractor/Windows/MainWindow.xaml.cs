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
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using Microsoft.Win32;
using System.IO;
using System.Collections;
using Game;
using GameLibrary;
using System.Data.Common;
using Infralution.Localization.Wpf;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Media.TextFormatting;
using System.Net;
using System.Diagnostics;

namespace GnomeExtractor
{
    public partial class MainWindow : Window
    {
        bool isCheatsOn;
        bool isLabelsVertical;
        bool isAutoUpdateEnabled;
        bool isLatestVersion = false;
        bool isUpdatesNeeded = false;
        bool isWindowsXP = false;
        //int[] version = { 0, 3, 27 };
        Version latestVersion;
        string filePath;
        string lastBackupFileName;
        string appStartupPath = System.IO.Path.GetDirectoryName(Application.ResourceAssembly.Location);

        Settings settings = new Settings();
        GnomanEmpire gnomanEmpire;
        BackgroundWorker backgrWorker = new BackgroundWorker();
        BackgroundWorker updater = new BackgroundWorker();
        BackgroundWorker bkgw = new BackgroundWorker();
        OperatingSystem osInfo = Environment.OSVersion;
        ResourceManager resourceManager;
        Statistics mapStatistics;
        DataSet dataSetTables = new DataSet();

        #region WindowHandling
        public MainWindow()
        {
            Globals.logger.Info("Gnome Extractor is running...");
            //Globals.logger.Info("G

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
                        Globals.logger.Info("Localization ru-RU resource file has been found");
                    }
                
                CultureManager.UICulture = new CultureInfo(lang);
                settings.Fields.ProgramLanguage = lang;
            }
            else
                CultureManager.UICulture = new CultureInfo(settings.Fields.ProgramLanguage);

            Globals.logger.Debug("Language selected: {0}", CultureManager.UICulture.Name);

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
            this.isCheatsOn = settings.Fields.LastRunCheatMode;
            this.isLabelsVertical = settings.Fields.LastRunIsLablesVertical;
            this.tabControl.SelectedIndex = settings.Fields.TabItemSelected;
            this.isAutoUpdateEnabled = settings.Fields.IsAutoUpdateEnabled;

            Globals.logger.Debug("Settings have been loaded");
            Globals.logger.Debug("Game initialization...");
            typeof(GnomanEmpire).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(GnomanEmpire.Instance, null);
            if (GnomanEmpire.Instance.Graphics.IsFullScreen) GnomanEmpire.Instance.Graphics.ToggleFullScreen();
            GnomanEmpire.Instance.AudioManager.MusicVolume = 0;
            Globals.logger.Debug("Game initialized");

            Globals.Initialize();

            if (settings.Fields.IsAutoUpdateEnabled) CheckingUpdates(false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ControlStates();

            Globals.logger.Info("Running is complete");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Globals.logger.Debug("Trying to close program...");
            if (backgrWorker.IsBusy) e.Cancel = true;
            Globals.logger.Info("Program closes...");
            settings.Fields.LastRunWindowState = this.WindowState;
            settings.Fields.LastRunLocation = new Point(Left, Top);
            settings.Fields.LastRunSize = new Size(Width, Height);
            settings.Fields.LastRunCheatMode = this.isCheatsOn;
            settings.Fields.LastRunIsLablesVertical = !this.isLabelsVertical;
            settings.Fields.TabItemSelected = tabControl.SelectedIndex;
            settings.Fields.IsAutoUpdateEnabled = isAutoUpdateEnabled;
            Globals.logger.Debug("Settings have been saved");
            settings.WriteXml();
            if (gnomanEmpire != null) gnomanEmpire = null;

            // Отправляем сейвы обратно, затираем их в локальной папке
            // Move savefiles in /worlds forder
            if (isWindowsXP)
            {
                Globals.logger.Debug("Looking for saves in AppDir...");
                DirectoryInfo dir = new DirectoryInfo(appStartupPath);
                FileInfo[] fi = dir.GetFiles("*.sav");
                foreach (FileInfo file in fi)
                {
                    File.Copy(file.FullName, GnomanEmpire.SaveFolderPath("\\Worlds") + file.Name, true);
                    File.Delete(file.FullName);
                    Globals.logger.Info("Save file {0} has been moved to the Worlds path", file.Name);
                }
                Globals.logger.Debug("All save files has been moved");
            }

            Globals.logger.Info("Program is closed");
        }
        #endregion

        #region ButtonClickHandlers
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("About button has been clicked");
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
                Globals.logger.Trace("FastEditor OK button has been clicked");

                int from, to;
                if (fastEditor.isAllTablesCheckBox.IsChecked == true)
                {
                    to = tabControl.Items.Count;
                    from = 0;
                }
                else
                {
                    from = tabControl.SelectedIndex;
                    to = tabControl.SelectedIndex + 1;
                }
                Globals.logger.Trace("Changing values in tabs from {0} to {1}", from, to - 1);

                for (; from < to; from++)
                {
                    DataRow[] dataRows = dataSetTables.Tables[from].Select();
                    for (int row = 0; row < dataRows.Length; row++)
                        for (int col = Globals.FirstColumnNames.Length; col < dataRows[row].ItemArray.Length; col++)
                            dataRows[row][col] = fastEditor.Value;
                    Globals.logger.Debug("{0} table has been changed", dataSetTables.Tables[from].TableName);
                }
            }
        }

        private void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("Open button has been clicked");
            OpenFileDialog openDlg = new OpenFileDialog();

            if (isWindowsXP)
            {
                Globals.logger.Info("Copying save files to AppPath...");
                openDlg.InitialDirectory = appStartupPath;
                DirectoryInfo dir = new DirectoryInfo(GnomanEmpire.SaveFolderPath("Worlds"));
                FileInfo[] fi = dir.GetFiles("*.sav");
                foreach (FileInfo file in fi)
                {
                    Globals.logger.Debug("Save file{0} have been temporary copied to Program directory", file.Name);
                    File.Copy(file.FullName, appStartupPath + "\\" + file.Name, true);

                }
                if (!File.Exists("fixed7z.dll"))
                {
                    File.Copy("7z.dll", "fixed7z.dll");
                    Globals.logger.Debug("Fixed 7z.dll has been created");
                }
                SevenZip.SevenZipExtractor.SetLibraryPath("fixed7z.dll");
            }
            else
            {
                SevenZip.SevenZipExtractor.SetLibraryPath("7z.dll");
                openDlg.InitialDirectory = System.IO.Path.GetFullPath(GnomanEmpire.SaveFolderPath("Worlds\\"));
            }

            openDlg.Filter = "Gnomoria Save Files (*.sav)|*.sav";

            Globals.logger.Debug("Open file dialog creating...");
            if (openDlg.ShowDialog() == true)
            {
                Globals.logger.Debug("Open file dialog has been closed");

                DisableControlsForBackgroundWorker();
                filePath = openDlg.FileName;

                backgrWorker.DoWork += new DoWorkEventHandler(backgrWorker_LoadGame);
                backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgrWorker_LoadGameCompleted);
                backgrWorker.RunWorkerAsync(openDlg.SafeFileName);
            }
        }

        private void backgrWorker_LoadGameCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Initializing and configuring DataGrids
            // Инициализация и настройка DataGrid'ов
            var tempIndex = tabControl.SelectedIndex;
            var names = new string[] { "dataGridProfessions", "dataGridCombat", "dataGridAttributes" };

            for (int tableIndex = 0; tableIndex < dataSetTables.Tables.Count; tableIndex++)
            {
                tabControl.SelectedIndex = tableIndex;
                var dataGrid = FindName(names[tableIndex]) as DataGrid;
                dataGrid.DataContext = dataSetTables.Tables[tableIndex].DefaultView;
                dataGrid.UpdateLayout();

                for (int i = 0; i < Globals.FirstColumnNames.Length - 2; i++)
                {
                    dataGrid.Columns[i].IsReadOnly = true;
                    dataGrid.Columns[i].MaxWidth = 0.01;
                }
                dataGrid.Columns[Globals.FirstColumnNames.Length - 1].MaxWidth = (tableIndex == 1 || tableIndex == 2) ? 0.01 : 150;
                dataGrid.Columns[Globals.FirstColumnNames.Length - 1].IsReadOnly = true;
                dataGrid.Columns[Globals.FirstColumnNames.Length - 2].MaxWidth = 150;
            }

            tabControl.SelectedIndex = tempIndex;

            // Мутим datacontext'ы для привязки статистики
            // Datacontext's for statistics
            mapStatistics = new Statistics(gnomanEmpire);
            wrapPanelStatistics.DataContext = mapStatistics;

            GridState();
            backgrWorker.DoWork -= new DoWorkEventHandler(backgrWorker_LoadGame);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgrWorker_LoadGameCompleted);

            statusBarLabel.Content = resourceManager.GetString("chosenFile") + " " + System.IO.Path.GetFileName(filePath) +
                    " " + resourceManager.GetString("worldName") + " " + gnomanEmpire.World.AIDirector.PlayerFaction.Name;

            Globals.logger.Info("World initialization complete");
            ControlStates();
        }

        private void backgrWorker_LoadGame(object sender, DoWorkEventArgs e)
        {
            Globals.logger.Info("World initialization...");
            Globals.logger.Info("Loading {0} save file...", e.Argument);

            GnomanEmpire.Instance.LoadGame((string)e.Argument);

            gnomanEmpire = GnomanEmpire.Instance;

            dataSetTables.Tables.Clear();

            dataSetTables.Tables.Add(new DataTable("Professions"));
            dataSetTables.Tables.Add(new DataTable("Combat"));
            dataSetTables.Tables.Add(new DataTable("Attributes"));

            // Начальные столбцы
            // First columns
            for (int i = 0; i < Globals.FirstColumnNames.Length; i++)
            {
                dataSetTables.Tables["Professions"].Columns.Add(Globals.FirstColumnNames[i]);
                dataSetTables.Tables["Combat"].Columns.Add(Globals.FirstColumnNames[i]);
                dataSetTables.Tables["Attributes"].Columns.Add(Globals.FirstColumnNames[i]);
            }
            //for (int i = 0; i < 2; i++)
            //    for (int j = 0; j < Globals.FirstColumnNames.Length; j++)
            //        dataSetTables.Tables[i].Columns.Add(Globals.FirstColumnNames[j]);

            //dataSetTables.Tables[0].PrimaryKey = new DataColumn[] { dataSetTables.Tables[0].Columns["Name"] };
            //dataSetTables.Tables[1].PrimaryKey = new DataColumn[] { dataSetTables.Tables[1].Columns["Name"] };
            //dataSetTables.Tables[2].PrimaryKey = new DataColumn[] { dataSetTables.Tables[2].Columns["Name"] };

            // Заполняем имена скиллов и атрибутов
            // Fill the skill & attribute names
            foreach (string name in Globals.SkillNamesProfessions1)
                dataSetTables.Tables["Professions"].Columns.Add(name, typeof(int));
            foreach (string name in Globals.SkillNamesProfessions2)
                dataSetTables.Tables["Professions"].Columns.Add(name, typeof(int));
            foreach (string name in Globals.SkillNamesCombat)
                dataSetTables.Tables["Combat"].Columns.Add(name, typeof(int));
            foreach (string name in Globals.AttributeNames)
                dataSetTables.Tables["Attributes"].Columns.Add(name, typeof(int));

            // Перебор гномов на карте
            // Looking for gnomes at the map
            var rowIndex = 0;
            for (int level = 0; level < gnomanEmpire.Map.Levels.Length; level++)
                for (int row = 0; row < gnomanEmpire.Map.Levels[0].Length; row++)
                    for (int col = 0; col < gnomanEmpire.Map.Levels[0][0].Length; col++)
                        for (int num = 0; num < gnomanEmpire.Map.Levels[level][row][col].Characters.Count; num++)
                            if (gnomanEmpire.Map.Levels[level][row][col].Characters[num].RaceID == RaceID.Gnome)
                            {
                                var gnome = gnomanEmpire.Map.Levels[level][row][col].Characters[num];

                                // Создаем строки
                                // Creating rows
                                DataRow tmpRowProf = dataSetTables.Tables["Professions"].NewRow();
                                DataRow tmpRowComb = dataSetTables.Tables["Combat"].NewRow();
                                DataRow tmpRowAttr = dataSetTables.Tables["Attributes"].NewRow();

                                // Преобразование AllowedSkills в двоичный вид
                                // Make AllowedSkills as binary
                                for (int alowedSkillsIndex = 0; alowedSkillsIndex < 2; alowedSkillsIndex++)
                                {
                                    var temp = Convert.ToString(gnome.Mind.Profession.AllowedSkills.AllowedSkills[alowedSkillsIndex], 2);
                                    //MessageBox.Show(gnome.Mind.Profession.AllowedSkills.AllowedSkills[1].ToString());
                                    char[] arr = temp.ToCharArray();
                                    Array.Reverse(arr);
                                    // заполняем нулями недостающие разряды
                                    // Fill '0' empty digit
                                    List<char> tempchars = new List<char>(arr);
                                    for (int indxxx = 0; indxxx < Globals.SkillsProfessions1.Length - arr.Length; indxxx++)
                                        tempchars.Add('0');
                                    var allowedSkills = new string(tempchars.ToArray());
                                    tmpRowProf[4 + alowedSkillsIndex] = tmpRowComb[4 + alowedSkillsIndex] = tmpRowAttr[4 + alowedSkillsIndex] = allowedSkills;
                                }

                                // Заполняем начальные элементы строк
                                // Fill the first row elements
                                tmpRowProf[0] = tmpRowComb[0] = tmpRowAttr[0] = level;
                                tmpRowProf[1] = tmpRowComb[1] = tmpRowAttr[1] = row;
                                tmpRowProf[2] = tmpRowComb[2] = tmpRowAttr[2] = col;
                                tmpRowProf[3] = tmpRowComb[3] = tmpRowAttr[3] = num;
                                tmpRowProf[6] = tmpRowComb[6] = tmpRowAttr[6] = rowIndex;
                                tmpRowProf[7] = tmpRowComb[7] = tmpRowAttr[7] = gnome.Name();
                                tmpRowProf[8] = tmpRowComb[8] = tmpRowAttr[8] = gnome.Mind.Profession.Title;

                                //заполняем навыки
                                //Fill the skills
                                for (int j = 0; j < Globals.SkillsProfessions1.Length; j++)
                                    tmpRowProf[j + Globals.FirstColumnNames.Length] = gnome.SkillLevel(Globals.SkillsProfessions1[j]);
                                for (int j = 0; j < Globals.SkillsProfessions2.Length; j++)
                                    tmpRowProf[j + Globals.SkillsProfessions1.Length + Globals.FirstColumnNames.Length] = gnome.SkillLevel(Globals.SkillsProfessions2[j]);

                                for (int j = 0; j < Globals.SkillsCombat.Length; j++)
                                    tmpRowComb[j + Globals.FirstColumnNames.Length] = gnome.SkillLevel(Globals.SkillsCombat[j]);

                                for (int j = 0; j < Globals.Attributes.Length; j++)
                                    tmpRowAttr[j + Globals.FirstColumnNames.Length] = gnome.AttributeLevel(Globals.Attributes[j]) * 100;

                                // Добавляем полученные строки в таблицу
                                // Adding rows to table
                                dataSetTables.Tables["Professions"].Rows.Add(tmpRowProf);
                                dataSetTables.Tables["Combat"].Rows.Add(tmpRowComb);
                                dataSetTables.Tables["Attributes"].Rows.Add(tmpRowAttr);
                                Globals.logger.Debug("Gnome {0} has been read", gnome.Name());
                                rowIndex++;
                            }
        }

        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("Save button has been clicked");
            DisableControlsForBackgroundWorker();

            backgrWorker.DoWork += new DoWorkEventHandler(backgrWorker_SaveGame);
            backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgrWorker_SaveGameCompleted);
            backgrWorker.RunWorkerAsync();
        }

        private void backgrWorker_SaveGameCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            backgrWorker.DoWork -= new DoWorkEventHandler(backgrWorker_SaveGame);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgrWorker_SaveGameCompleted);

            statusBarLabel.Content = resourceManager.GetString("saveDoneMessage") + " " + gnomanEmpire.CurrentWorld + ", " +
                                     resourceManager.GetString("backupDoneMessage") + " " + lastBackupFileName;

            Globals.logger.Info("Save complete");
            ControlStates();
        }

        private void backgrWorker_SaveGame(object sender, DoWorkEventArgs e)
        {
            Globals.logger.Info("World saving...");

            var dir = GnomanEmpire.SaveFolderPath("Backup\\");
            Directory.CreateDirectory(dir);
            lastBackupFileName = DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" +
                                 DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + "_" + gnomanEmpire.CurrentWorld;
            File.Copy(filePath, dir + lastBackupFileName, true);
            Globals.logger.Info("Backup file created at" + dir + lastBackupFileName);

            DataRow[] rowProf = dataSetTables.Tables["Professions"].Select(null, null, DataViewRowState.CurrentRows);
            DataRow[] rowComb = dataSetTables.Tables["Combat"].Select(null, null, DataViewRowState.CurrentRows);
            DataRow[] rowAttr = dataSetTables.Tables["Attributes"].Select(null, null, DataViewRowState.CurrentRows);

            for (int i = 0; i < rowProf.Length; i++)
            {
                int x = Int32.Parse((string)(rowProf[i][0]));
                int y = Int32.Parse((string)(rowProf[i][1]));
                int z = Int32.Parse((string)(rowProf[i][2]));
                int num = Int32.Parse((string)(rowProf[i][3]));
                var allowedSkills1 = (string)rowProf[i][4];
                var allowedSkills2 = (string)rowProf[i][5];
                var gnomeName = (string)rowProf[i][Globals.FirstColumnNames.Length - 2];
                var gnome = gnomanEmpire.Map.Levels[x][y][z].Characters[num];
                var profession = (string)rowProf[i][Globals.FirstColumnNames.Length - 1];

                gnome.SetName(gnomeName);

                // Мутим гному новую профу
                // Creating new profession. Construction "new Profession(string)" is required for working
                if (profession == "Custom")
                {
                    gnome.Mind.Profession = new Profession("Custom");
                    gnome.Mind.Profession.AllowedSkills.ClearAll();
                }

                //tmpRowProf[j + StaticValues.FirstColumnNames.Length] = 
                for (int j = 0; j < Globals.SkillsProfessions1.Length; j++)
                {
                    gnome.SetSkillLevel(Globals.SkillsProfessions1[j], (int)rowProf[i][j + Globals.FirstColumnNames.Length]);
                    if (profession == "Custom")
                    {
                        if (allowedSkills1[j] == '1') gnome.Mind.Profession.AllowedSkills.AddSkill(Globals.SkillsProfessions1[j]);
                        else gnome.Mind.Profession.AllowedSkills.RemoveSkill(Globals.SkillsProfessions1[j]);
                    }
                }
                for (int j = 0; j < Globals.SkillsProfessions2.Length; j++)
                {
                    gnome.SetSkillLevel(Globals.SkillsProfessions2[j], (int)rowProf[i][j + Globals.SkillsProfessions1.Length + Globals.FirstColumnNames.Length]);
                    if (profession == "Custom")
                    {
                        if (allowedSkills2[j + 8] == '1') gnome.Mind.Profession.AllowedSkills.AddSkill(Globals.SkillsProfessions2[j]);
                        else gnome.Mind.Profession.AllowedSkills.RemoveSkill(Globals.SkillsProfessions2[j]);
                    }
                }

                for (int j = 0; j < Globals.SkillsCombat.Length; j++)
                    gnome.SetSkillLevel(Globals.SkillsCombat[j], (int)rowComb[i][j + Globals.FirstColumnNames.Length]);

                for (int j = 0; j < Globals.Attributes.Length; j++)
                    gnome.SetAttributeLevel(Globals.Attributes[j], (int)rowAttr[i][j + Globals.FirstColumnNames.Length]);

                Globals.logger.Debug("Gnome {0} written", gnome.Name());
            }

            gnomanEmpire.SaveGame();
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("Exit button clicked");

            Close();
        }

        private void russianMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("Russian language button clicked");

            if (File.Exists("ru-RU\\GnomeExtractor.resources.dll"))
            {
                CultureManager.UICulture = new CultureInfo("ru-RU");
                settings.Fields.ProgramLanguage = "ru-RU";
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");

                Globals.logger.Info("Localization changed to Russian");
            }
            else
            {
                MessageBox.Show(resourceManager.GetString("localizationNotFound"));

                Globals.logger.Error("Localization file for Russian language is not found");
            }
        }

        private void englishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("English language button clicked");

            CultureManager.UICulture = new CultureInfo("en-US");
            settings.Fields.ProgramLanguage = "en-US";
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            Globals.logger.Info("Localization changed to English");
        }

        private void cheatModeMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("Cheat mode button has been clicked");

            isCheatsOn = !isCheatsOn;
            ControlStates();
            GridState();

            Globals.logger.Info("Cheat mode {0}", (isCheatsOn) ? "enabled" : "disabled");
        }

        private void updatingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("Updating button has been clicked");

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
                Globals.logger.Info("Latest version is installed");
            }
            else if (isUpdatesNeeded)
            {
                Globals.logger.Info("Newest version found");
                if (MessageBoxResult.Yes == MessageBox.Show(resourceManager.GetString("newestVersion") + " " + latestVersion + ", " +
                    resourceManager.GetString("downloadNewVersion"), resourceManager.GetString("updateDialogCaption"), MessageBoxButton.YesNo)) Process.Start("http://gnomex.tk");
            }
            isLatestVersion = false;
            isUpdatesNeeded = false;
            Globals.logger.Info("Updating is complete");
            updatingMenuItem.IsEnabled = true;
        }

        private void updater_Update(object sender, DoWorkEventArgs e)
        {
            Globals.logger.Info("Updating is running...");
            WebRequest request = WebRequest.Create(new Uri("http://gnomex.tk/version/current"));

            WebResponse response = request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
            latestVersion = new Version(reader.ReadToEnd());
            Globals.logger.Debug("Latest version is {0}", latestVersion);
            reader.Close();
            response.Close();

            //if ((version[0] == Double.Parse(latestVersion[0]) && version[1] == Double.Parse(latestVersion[1]) && version[2] < Double.Parse(latestVersion[2])) ||
            //(version[0] == Double.Parse(latestVersion[0]) && version[1] < Double.Parse(latestVersion[1])) || (version[0] < Double.Parse(latestVersion[0])))
            if (Globals.ProgramVersion.CompareTo(latestVersion) < 0)
                isUpdatesNeeded = true;
            else
                isLatestVersion = true;

            

            if ((bool)e.Argument) e.Result = true;
            else e.Result = false;
        }

        private void autoUpdatingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Debug("Autoupdating button button has been clicked");

            isAutoUpdateEnabled = !isAutoUpdateEnabled;
            Globals.logger.Info("Auto updating is {0}", (isAutoUpdateEnabled) ? "enabled" : "disabled");
            ControlStates();
        }

        private void exportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Globals.logger.Info("Export is running...");

            DisableControlsForBackgroundWorker();

            backgrWorker.DoWork += new DoWorkEventHandler(backgrWorker_ExportToCSV);
            backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgrWorker_ExportToCSVCompleted);
            backgrWorker.RunWorkerAsync();
        }

        private void backgrWorker_ExportToCSV(object sender, DoWorkEventArgs e)
        {
            Directory.CreateDirectory("Export\\");
            foreach (DataTable table in dataSetTables.Tables)
            {
                var path = "Export\\" + table.TableName.ToLower() + ".csv";
                FileStream fs = new FileStream(path, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(table.ToCSV());
                sw.Close();
                Globals.logger.Info("File {0} is created", path);
            }
        }

        private void backgrWorker_ExportToCSVCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            backgrWorker.DoWork -= new DoWorkEventHandler(backgrWorker_ExportToCSV);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgrWorker_ExportToCSVCompleted);

            Globals.logger.Info("Export is complete");
            statusBarLabel.Content = resourceManager.GetString("exportDoneMessage");

            ControlStates();
        }
        #endregion

        #region State handling
        private void ControlStates()
        {
            cheatsMenuItem.Visibility = (settings.Fields.IsCheatsEnabled) ? Visibility.Visible : Visibility.Collapsed;
            if (!settings.Fields.IsCheatsEnabled) settings.Fields.LastRunCheatMode = false;
            fastEditMenuItem.IsEnabled = (gnomanEmpire != null && isCheatsOn);
            saveMenuItem.IsEnabled = exportMenuItem.IsEnabled = (gnomanEmpire != null && !backgrWorker.IsBusy);
            openMenuItem.IsEnabled = !(backgrWorker.IsBusy);
            cheatModeMenuItem.IsChecked = isCheatsOn;
            autoUpdatingMenuItem.IsChecked = isAutoUpdateEnabled;
            progressBarMain.Visibility = (backgrWorker.IsBusy) ? Visibility.Visible : Visibility.Hidden;
        }

        private void DisableControlsForBackgroundWorker()
        {
            fastEditMenuItem.IsEnabled = openMenuItem.IsEnabled = saveMenuItem.IsEnabled = exportMenuItem.IsEnabled = false;
            progressBarMain.Visibility = System.Windows.Visibility.Visible;
        }

        private void GridState()
        {
            if (gnomanEmpire != null)
            {
                var tempIndex = tabControl.SelectedIndex;
                tabControl.SelectedIndex = 0;
                dataGridProfessions.UpdateLayout();
                for (int i = Globals.FirstColumnNames.Length; i < dataSetTables.Tables["Professions"].Columns.Count; i++)
                    dataGridProfessions.Columns[i].IsReadOnly = !isCheatsOn;
                tabControl.SelectedIndex = 1;
                dataGridCombat.UpdateLayout();
                for (int i = Globals.FirstColumnNames.Length; i < dataSetTables.Tables["Combat"].Columns.Count; i++)
                    dataGridCombat.Columns[i].IsReadOnly = !isCheatsOn;
                tabControl.SelectedIndex = 2;
                dataGridAttributes.UpdateLayout();
                for (int i = Globals.FirstColumnNames.Length; i < dataSetTables.Tables["Attributes"].Columns.Count; i++)
                    dataGridAttributes.Columns[i].IsReadOnly = !isCheatsOn;
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
            if (e.Column.DisplayIndex == Globals.FirstColumnNames.Length - 1) e.Handled = true;

            if (e.Column.DisplayIndex > Globals.FirstColumnNames.Length - 1)
                e.Column.SortDirection = ListSortDirection.Ascending;
        }

        private void DataGridCell_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
            if (e.ChangedButton == MouseButton.Right || (e.ChangedButton == MouseButton.Left && !isCheatsOn))
            {
                var cell = sender as DataGridCell;
                var row = DataGridRow.GetRowContainingElement(cell);
                var realIndex = Int32.Parse((dataGridProfessions.Columns[6].GetCellContent(row) as TextBlock).Text);
                var skillName = cell.Column.Header.ToString();
                var index = 0;
                var check = -1;

                foreach (var item in Globals.SkillNamesProfessions1)
                {
                    if (item == skillName)
                    {
                        check = index;
                        break;
                    }
                    index++;
                }

                if (check != -1)
                {
                    var str = dataSetTables.Tables["Professions"].Rows[realIndex].ItemArray[4] as string;
                    char[] chars = str.ToArray();
                    chars[check] = (str[check] == '1') ? '0' : '1';
                    dataSetTables.Tables["Professions"].Rows[realIndex].BeginEdit();
                    dataSetTables.Tables["Professions"].Rows[realIndex][4] = new String(chars);
                    dataSetTables.Tables["Professions"].Rows[realIndex][Globals.FirstColumnNames.Length - 1] = "Custom";
                    dataSetTables.Tables["Professions"].Rows[realIndex].EndEdit();
                    BindingOperations.GetMultiBindingExpression(cell, DataGridCell.BackgroundProperty).UpdateTarget();
                    return;
                }

                index = 0;
                foreach (var item in Globals.SkillNamesProfessions2)
                {
                    if (item == skillName)
                    {
                        check = index;
                        break;
                    }
                    index++;
                }
                if (check != -1)
                {
                    var str = dataSetTables.Tables["Professions"].Rows[realIndex].ItemArray[5] as string;
                    char[] chars = str.ToArray();
                    chars[check + 8] = (str[check + 8] == '1') ? '0' : '1';
                    dataSetTables.Tables["Professions"].Rows[realIndex].BeginEdit();
                    dataSetTables.Tables["Professions"].Rows[realIndex][5] = new String(chars);
                    dataSetTables.Tables["Professions"].Rows[realIndex].EndEdit();
                    BindingOperations.GetMultiBindingExpression(cell, DataGridCell.BackgroundProperty).UpdateTarget();
                    return;
                }
            }
            else if (e.ChangedButton == MouseButton.Left && isCheatsOn)
            {
                var cell = sender as DataGridCell;
            }
        }

        private void DataGridRowHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRowHeader header = sender as DataGridRowHeader;
            if (header != null)
            {
                //MessageBox.Show("Sorting will be");
                e.Handled = true;
            }
        }

        private void DataGridColumnHeaderProfessions_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            int columnIndex = (sender as DataGridColumnHeader).Column.DisplayIndex;

            if (columnIndex == Globals.FirstColumnNames.Length - 1)
                (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString("Name");

            switch (tabControl.SelectedIndex)
            {
                case 0:
                    if (columnIndex > Globals.FirstColumnNames.Length - 1 && columnIndex < Globals.SkillNamesProfessions1.Length + Globals.FirstColumnNames.Length)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(Globals.SkillNamesProfessions1[columnIndex - Globals.FirstColumnNames.Length]);
                    else if (columnIndex > Globals.FirstColumnNames.Length - 1 + Globals.SkillNamesProfessions1.Length)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(Globals.SkillNamesProfessions2[columnIndex - Globals.FirstColumnNames.Length - Globals.SkillNamesProfessions1.Length]);
                    break;
                case 1:
                    if (columnIndex > Globals.FirstColumnNames.Length - 1)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(Globals.SkillNamesCombat[columnIndex - Globals.FirstColumnNames.Length]);
                    break;
                case 2:
                    if (columnIndex > Globals.FirstColumnNames.Length - 1)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(Globals.AttributeNames[columnIndex - Globals.FirstColumnNames.Length]);
                    break;
                default:
                    break;
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var value = (e.EditingElement as TextBox).Text;
            
            // Проверяем является ли выделенная ячейка именем
            // Checking if cell is name
            if (e.Column.DisplayIndex == Globals.FirstColumnNames.Length - 1)
            {
                if (value.Length > 24)
                    value = value.Substring(0, 24);

                (e.EditingElement as TextBox).Text = value;
                var realIndex = Int32.Parse((dataGridProfessions.Columns[6].GetCellContent(e.Row) as TextBlock).Text);

                for (int i = 0; i < 3; i++)
                {
                    dataSetTables.Tables[i].Rows[realIndex].BeginEdit();
                    dataSetTables.Tables[i].Rows[realIndex][Globals.FirstColumnNames.Length - 1] = value;
                    dataSetTables.Tables[i].Rows[realIndex].EndEdit();
                }
            }
            else if (e.Column.DisplayIndex > Globals.FirstColumnNames.Length)
            {
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
        #endregion

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Globals.TabControlSelectedIndex = tabControl.SelectedIndex;
            Globals.logger.Trace("TabControl.SelectedIndex is changed to {0}", tabControl.SelectedIndex);
        }
    }
}