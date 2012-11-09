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
        bool isUpdateFailed = false;
        bool isUpdatesNeeded = false;
        int[] version = { 0, 3, 22 };
        string[] latestVersion;
        string filePath;
        string appStartupPath = System.IO.Path.GetDirectoryName(Application.ResourceAssembly.Location);

        Settings settings = new Settings();
        GnomanEmpire gnomanEmpire;
        BackgroundWorker backgrWorker = new BackgroundWorker();
        BackgroundWorker updater = new BackgroundWorker();
        OperatingSystem osInfo = Environment.OSVersion;
        ResourceManager resourceManager;
        MapStatistics mapStatistics;
        DataSet dataSetTables = new DataSet();

        #region WindowHandling
        public MainWindow()
        {
            // Read settings from Xml file
            settings.ReadXml();

            // При первом запуске выставляем культуру установленную в компе, при последующих - предыдущую
            // First run changing localization same like in computer
            if (settings.Fields.ProgramLanguage == "")
            {
                string lang = "en-US";
                if (CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "ru")
                    if (File.Exists("ru-RU\\GnomeExtractor.resources.dll")) lang = "ru-RU";
                
                CultureManager.UICulture = new CultureInfo(lang);
                settings.Fields.ProgramLanguage = lang;
            }
            else
                CultureManager.UICulture = new CultureInfo(settings.Fields.ProgramLanguage);

            CultureManager.UICultureChanged += new EventHandler(CultureManager_UICultureChanged);
            resourceManager = new ResourceManager("GnomeExtractor.Resources.Loc", Assembly.GetExecutingAssembly());

            if (!File.Exists("loclib.dll")) MessageBox.Show("File loclib.dll not found, please reinstall the program");
            if (!File.Exists("Gnomoria.exe")) MessageBox.Show("File Gnomoria.exe not found, please install the program in game folder");

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
            
            ControlStates();


        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            typeof(GnomanEmpire).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(GnomanEmpire.Instance, null);
            Focus();
            GnomanEmpire.Instance.AudioManager.MusicVolume = 0;

            StaticValues.Initialize();

            if (settings.Fields.IsAutoUpdateEnabled) CheckingUpdates(false);
            ControlStates();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (backgrWorker.IsBusy) e.Cancel = true;
            settings.Fields.LastRunWindowState = this.WindowState;
            settings.Fields.LastRunLocation = new Point(Left, Top);
            settings.Fields.LastRunSize = new Size(Width, Height);
            settings.Fields.LastRunCheatMode = this.isCheatsOn;
            settings.Fields.LastRunIsLablesVertical = !this.isLabelsVertical;
            settings.Fields.TabItemSelected = tabControl.SelectedIndex;
            settings.Fields.IsAutoUpdateEnabled = isAutoUpdateEnabled;
            settings.WriteXml();

            // Отправляем сейвы обратно, затираем их в локальной папке
            // Move savefiles in /worlds forder
            if (gnomanEmpire != null) gnomanEmpire = null;
            DirectoryInfo dir = new DirectoryInfo(appStartupPath);
            FileInfo[] fi = dir.GetFiles("*.sav");
            foreach (FileInfo file in fi)
            {
                File.Copy(file.FullName, GnomanEmpire.SaveFolderPath("\\Worlds") + file.Name, true);
                File.Delete(file.FullName);
            }
        }
        #endregion

        #region ButtonClickHandlers
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Owner = this;
            about.ShowDialog();
        }

        private void fastEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FastEditor fastEditor = new FastEditor();
            fastEditor.Owner = this;
            fastEditor.ShowDialog();

            if (fastEditor.IsOkClicked)
            {
                int tempIndex, tempCount;
                if (fastEditor.isAllTablesCheckBox.IsChecked == true)
                {
                    tempCount = tabControl.Items.Count;
                    tempIndex = 0;
                }
                else
                {
                    tempIndex = tabControl.SelectedIndex;
                    tempCount = tabControl.SelectedIndex + 1;
                }
                for (tempIndex = 0; tempIndex < tempCount; tempIndex++)
                    switch (tempIndex)
                    {
                        case 0:
                            {
                                DataRow[] dataRows = dataSetTables.Tables["Professions"].Select();
                                for (int row = 0; row < dataRows.Length; row++)
                                    for (int col = 0; col < StaticValues.SkillNamesProfessions1.Length + StaticValues.SkillNamesProfessions2.Length; col++)
                                        dataRows[row][col + StaticValues.FirstColumnNames.Length] = fastEditor.Value;
                                break;
                            }
                        case 1:
                            {
                                DataRow[] dataRows = dataSetTables.Tables["Combat"].Select();
                                for (int row = 0; row < dataRows.Length; row++)
                                    for (int col = 0; col < StaticValues.SkillNamesCombat.Length; col++)
                                        dataRows[row][col + StaticValues.FirstColumnNames.Length] = fastEditor.Value;
                                break;
                            }
                        case 2:
                            {
                                DataRow[] dataRows = dataSetTables.Tables["Attributes"].Select();
                                for (int row = 0; row < dataRows.Length; row++)
                                    for (int col = 0; col < StaticValues.AttributeNames.Length; col++)
                                        dataRows[row][col + StaticValues.FirstColumnNames.Length] = fastEditor.Value;
                                break;
                            }
                        default:
                            break;
                    }
            }
        }

        private void openMenuItem_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openDlg = new OpenFileDialog();
            if (osInfo.Version.Major == 5 && osInfo.Version.Minor != 0)
            {
                openDlg.InitialDirectory = appStartupPath;
                DirectoryInfo dir = new DirectoryInfo(GnomanEmpire.SaveFolderPath("Worlds"));
                FileInfo[] fi = dir.GetFiles("*.sav");
                foreach (FileInfo file in fi)
                    File.Copy(file.FullName, appStartupPath + "\\" + file.Name, true);
                if (!File.Exists("fixed7z.dll")) File.Copy("7z.dll", "fixed7z.dll");
                SevenZip.SevenZipExtractor.SetLibraryPath("fixed7z.dll");
            }
            else
            {
                openDlg.InitialDirectory = GnomanEmpire.SaveFolderPath("Worlds\\");
                SevenZip.SevenZipExtractor.SetLibraryPath("7z.dll");
            }
            openDlg.Filter = "Gnomoria Save Files (world*.sav)|world*.sav";
            if ((bool)openDlg.ShowDialog())
            {
                fastEditMenuItem.IsEnabled = openMenuItem.IsEnabled = saveMenuItem.IsEnabled = false;
                progressBarMain.Visibility = System.Windows.Visibility.Visible;
                filePath = openDlg.FileName;

                backgrWorker.DoWork += new DoWorkEventHandler(backgrWorker_LoadGame);
                backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgrWorker_LoadGameCompleted);
                backgrWorker.RunWorkerAsync(openDlg.SafeFileName);
            }
        }

        private void backgrWorker_LoadGameCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var tempIndex = tabControl.SelectedIndex;
            // Устанавливаем свойства столбцов через жопу
            // Setting up properties of columns through the ass
            tabControl.SelectedIndex = 0;
            dataGridProfessions.DataContext = dataSetTables.Tables["Professions"].DefaultView;
            dataGridProfessions.UpdateLayout();
            dataGridProfessions.Columns[StaticValues.FirstColumnNames.Length - 1].MaxWidth = 150;
            for (int i = 0; i < StaticValues.FirstColumnNames.Length - 1; i++)
            {
                dataGridProfessions.Columns[i].IsReadOnly = true;
                dataGridProfessions.Columns[i].MaxWidth = 0.01;
            }
            tabControl.SelectedIndex = 1;
            dataGridCombat.DataContext = dataSetTables.Tables["Combat"].DefaultView;
            dataGridCombat.UpdateLayout();
            dataGridCombat.Columns[StaticValues.FirstColumnNames.Length - 1].MaxWidth = 150;
            for (int i = 0; i < StaticValues.FirstColumnNames.Length - 1; i++)
            {
                dataGridCombat.Columns[i].IsReadOnly = true;
                dataGridCombat.Columns[i].MaxWidth = 0.01;
            }
            tabControl.SelectedIndex = 2;
            dataGridAttributes.DataContext = dataSetTables.Tables["Attributes"].DefaultView;
            dataGridAttributes.UpdateLayout();
            dataGridAttributes.Columns[StaticValues.FirstColumnNames.Length - 1].MaxWidth = 150;
            for (int i = 0; i < StaticValues.FirstColumnNames.Length - 1; i++)
            {
                dataGridAttributes.Columns[i].IsReadOnly = true;
                dataGridAttributes.Columns[i].MaxWidth = 0.01;
            }
            tabControl.SelectedIndex = tempIndex;

            // Мутим datacontext'ы для привязки статистики
            // Datacontext's for statistics
            mapStatistics = new MapStatistics(gnomanEmpire);
            mineralsGrid.DataContext = mapStatistics;
            worldNameStat.DataContext = gnomanEmpire.World.AIDirector.PlayerFaction.Name;

            ControlStates();
            GridState();
            progressBarMain.Visibility = Visibility.Hidden;
            backgrWorker.DoWork -= new DoWorkEventHandler(backgrWorker_LoadGame);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgrWorker_LoadGameCompleted);

            statusBarLabel.Content = resourceManager.GetString("chosenFile") + " " + System.IO.Path.GetFileName(filePath) +
                    " " + resourceManager.GetString("worldName") + " " + gnomanEmpire.World.AIDirector.PlayerFaction.Name;
        }

        private void backgrWorker_LoadGame(object sender, DoWorkEventArgs e)
        {
            GnomanEmpire.Instance.LoadGame((string)e.Argument);

            gnomanEmpire = GnomanEmpire.Instance;

            dataSetTables.Tables.Clear();

            dataSetTables.Tables.Add(new DataTable("Professions"));
            dataSetTables.Tables.Add(new DataTable("Combat"));
            dataSetTables.Tables.Add(new DataTable("Attributes"));

            // Начальные столбцы
            // First columns
            for (int i = 0; i < StaticValues.FirstColumnNames.Length; i++)
            {
                dataSetTables.Tables["Professions"].Columns.Add(StaticValues.FirstColumnNames[i]);
                dataSetTables.Tables["Combat"].Columns.Add(StaticValues.FirstColumnNames[i]);
                dataSetTables.Tables["Attributes"].Columns.Add(StaticValues.FirstColumnNames[i]);
            }

            //dataSetTables.Tables[0].PrimaryKey = new DataColumn[] { dataSetTables.Tables[0].Columns["Name"] };
            //dataSetTables.Tables[1].PrimaryKey = new DataColumn[] { dataSetTables.Tables[1].Columns["Name"] };
            //dataSetTables.Tables[2].PrimaryKey = new DataColumn[] { dataSetTables.Tables[2].Columns["Name"] };

            // Заполняем имена скиллов и атрибутов
            // Fill the skill & attribute names
            foreach (string name in StaticValues.SkillNamesProfessions1)
                dataSetTables.Tables["Professions"].Columns.Add(name, typeof(int));
            foreach (string name in StaticValues.SkillNamesProfessions2)
                dataSetTables.Tables["Professions"].Columns.Add(name, typeof(int));
            foreach (string name in StaticValues.SkillNamesCombat)
                dataSetTables.Tables["Combat"].Columns.Add(name, typeof(int));
            foreach (string name in StaticValues.AttributeNames)
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
                                    for (int indxxx = 0; indxxx < StaticValues.SkillsProfessions1.Length - arr.Length; indxxx++)
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

                                //заполняем навыки
                                //Fill the skills
                                for (int j = 0; j < StaticValues.SkillsProfessions1.Length; j++)
                                    tmpRowProf[j + StaticValues.FirstColumnNames.Length] = gnome.SkillLevel(StaticValues.SkillsProfessions1[j]);
                                for (int j = 0; j < StaticValues.SkillsProfessions2.Length; j++)
                                    tmpRowProf[j + StaticValues.SkillsProfessions1.Length + StaticValues.FirstColumnNames.Length] = gnome.SkillLevel(StaticValues.SkillsProfessions2[j]);

                                for (int j = 0; j < StaticValues.SkillsCombat.Length; j++)
                                    tmpRowComb[j + StaticValues.FirstColumnNames.Length] = gnome.SkillLevel(StaticValues.SkillsCombat[j]);

                                for (int j = 0; j < StaticValues.Attributes.Length; j++)
                                    tmpRowAttr[j + StaticValues.FirstColumnNames.Length] = gnome.AttributeLevel(StaticValues.Attributes[j]) * 100;

                                // Добавляем полученные строки в таблицу
                                // Adding rows to table
                                dataSetTables.Tables["Professions"].Rows.Add(tmpRowProf);
                                dataSetTables.Tables["Combat"].Rows.Add(tmpRowComb);
                                dataSetTables.Tables["Attributes"].Rows.Add(tmpRowAttr);
                                rowIndex++;
                            }
        }

        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dir = GnomanEmpire.SaveFolderPath("Backup\\");
            Directory.CreateDirectory(dir);
            File.Copy(filePath, dir + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + 
                                      DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + "_" + gnomanEmpire.CurrentWorld);

            fastEditMenuItem.IsEnabled = openMenuItem.IsEnabled = saveMenuItem.IsEnabled = false;
            progressBarMain.Visibility = System.Windows.Visibility.Visible;

            backgrWorker.DoWork += new DoWorkEventHandler(backgrWorker_SaveGame);
            backgrWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgrWorker_SaveGameCompleted);
            backgrWorker.RunWorkerAsync();
        }

        private void backgrWorker_SaveGameCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBarMain.Visibility = System.Windows.Visibility.Hidden;
            backgrWorker.DoWork -= new DoWorkEventHandler(backgrWorker_SaveGame);
            backgrWorker.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(backgrWorker_SaveGameCompleted);

            statusBarLabel.Content = resourceManager.GetString("saveDoneMessage") + " " + gnomanEmpire.CurrentWorld;

            ControlStates();
        }

        private void backgrWorker_SaveGame(object sender, DoWorkEventArgs e)
        {
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
                var gnomeName = (string)rowProf[i][StaticValues.FirstColumnNames.Length - 1];
                var gnome = gnomanEmpire.Map.Levels[x][y][z].Characters[num];

                gnome.SetName(gnomeName);

                // Мутим гному новую профу
                // Creating new profession. Construction "new Profession(string)" is required for working
                if (gnome.Mind.Profession.Title != "prof #" + gnomeName)
                {
                    gnome.Mind.Profession = new Profession("prof #" + gnomeName);
                    gnome.Mind.Profession.AllowedSkills.ClearAll();
                }

                //tmpRowProf[j + StaticValues.FirstColumnNames.Length] = 
                for (int j = 0; j < StaticValues.SkillsProfessions1.Length; j++)
                {
                    gnome.SetSkillLevel(StaticValues.SkillsProfessions1[j], (int)rowProf[i][j + StaticValues.FirstColumnNames.Length]);
                    if (allowedSkills1[j] == '1') gnome.Mind.Profession.AllowedSkills.AddSkill(StaticValues.SkillsProfessions1[j]);
                    else gnome.Mind.Profession.AllowedSkills.RemoveSkill(StaticValues.SkillsProfessions1[j]);
                }
                for (int j = 0; j < StaticValues.SkillsProfessions2.Length; j++)
                {
                    gnome.SetSkillLevel(StaticValues.SkillsProfessions2[j], (int)rowProf[i][j + StaticValues.SkillsProfessions1.Length + StaticValues.FirstColumnNames.Length]);
                    if (allowedSkills2[j + 8] == '1') gnome.Mind.Profession.AllowedSkills.AddSkill(StaticValues.SkillsProfessions2[j]);
                    else gnome.Mind.Profession.AllowedSkills.RemoveSkill(StaticValues.SkillsProfessions2[j]);
                }

                for (int j = 0; j < StaticValues.SkillsCombat.Length; j++)
                    gnome.SetSkillLevel(StaticValues.SkillsCombat[j], (int)rowComb[i][j + StaticValues.FirstColumnNames.Length]);

                for (int j = 0; j < StaticValues.Attributes.Length; j++)
                    gnome.SetAttributeLevel(StaticValues.Attributes[j], (int)rowAttr[i][j + StaticValues.FirstColumnNames.Length]);
            }

            gnomanEmpire.SaveGame();
        }

        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void russianMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("ru-RU\\GnomeExtractor.resources.dll"))
            {
                CultureManager.UICulture = new CultureInfo("ru-RU");
                settings.Fields.ProgramLanguage = "ru-RU";
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");
            }
            else
                MessageBox.Show(resourceManager.GetString("localizationNotFound"));
        }

        private void englishMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CultureManager.UICulture = new CultureInfo("en-US");
            settings.Fields.ProgramLanguage = "en-US";
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }

        private void cheatModeMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            isCheatsOn = !isCheatsOn;
            ControlStates();
            GridState();
        }

        private void updatingMenuItem_Click(object sender, RoutedEventArgs e)
        {
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
            if (isUpdateFailed) MessageBox.Show(resourceManager.GetString("latestVersion"));
            if (isUpdatesNeeded)
                if (MessageBoxResult.Yes == MessageBox.Show(resourceManager.GetString("newestVersion") + " " + latestVersion[0] + "." + latestVersion[1] + " build " + latestVersion[2] + ", " +
                    resourceManager.GetString("downloadNewVersion"), resourceManager.GetString("updateDialogCaption"), MessageBoxButton.YesNo))
                    Process.Start("http://gnomex.tk");
            isUpdateFailed = false;
            isUpdatesNeeded = false;
            updatingMenuItem.IsEnabled = true;
        }

        private void updater_Update(object sender, DoWorkEventArgs e)
        {
            WebRequest request = WebRequest.Create(new Uri("http://gnomex.tk/version/current"));

            WebResponse response = request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
            string log = reader.ReadToEnd();
            reader.Close();
            response.Close();

            latestVersion = log.Split('.', 'b');

            if ((version[0] == Double.Parse(latestVersion[0]) && version[1] == Double.Parse(latestVersion[1]) && version[2] < Double.Parse(latestVersion[2])) ||
            (version[0] == Double.Parse(latestVersion[0]) && version[1] < Double.Parse(latestVersion[1])) || (version[0] < Double.Parse(latestVersion[0])))
                isUpdatesNeeded = true;
            else
                if ((bool)e.Argument) isUpdateFailed = true;
        }

        private void autoUpdatingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            isAutoUpdateEnabled = !isAutoUpdateEnabled;
            ControlStates();
        }

        private void exportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("export\\");
            foreach (DataTable table in dataSetTables.Tables)
            {
                FileStream fs = new FileStream("export\\" + table.TableName.ToLower() + ".csv", FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(table.ToCSV());
                sw.Close();
            }
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

        private void GridState()
        {
            if (gnomanEmpire != null)
            {
                var tempIndex = tabControl.SelectedIndex;
                tabControl.SelectedIndex = 0;
                dataGridProfessions.UpdateLayout();
                for (int i = StaticValues.FirstColumnNames.Length; i < dataSetTables.Tables["Professions"].Columns.Count; i++)
                    dataGridProfessions.Columns[i].IsReadOnly = !isCheatsOn;
                tabControl.SelectedIndex = 1;
                dataGridCombat.UpdateLayout();
                for (int i = StaticValues.FirstColumnNames.Length; i < dataSetTables.Tables["Combat"].Columns.Count; i++)
                    dataGridCombat.Columns[i].IsReadOnly = !isCheatsOn;
                tabControl.SelectedIndex = 2;
                dataGridAttributes.UpdateLayout();
                for (int i = StaticValues.FirstColumnNames.Length; i < dataSetTables.Tables["Attributes"].Columns.Count; i++)
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

        #region DataGrids handling
        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.DisplayIndex > StaticValues.FirstColumnNames.Length - 1)
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

                foreach (var item in StaticValues.SkillNamesProfessions1)
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
                    dataSetTables.Tables["Professions"].Rows[realIndex].EndEdit();
                    BindingOperations.GetMultiBindingExpression(cell, DataGridCell.BackgroundProperty).UpdateTarget();
                    return;
                }

                index = 0;
                foreach (var item in StaticValues.SkillNamesProfessions2)
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

            if (columnIndex == StaticValues.FirstColumnNames.Length - 1)
                (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString("Name");

            switch (tabControl.SelectedIndex)
            {
                case 0:
                    if (columnIndex > StaticValues.FirstColumnNames.Length - 1 && columnIndex < StaticValues.SkillNamesProfessions1.Length + StaticValues.FirstColumnNames.Length)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(StaticValues.SkillNamesProfessions1[columnIndex - StaticValues.FirstColumnNames.Length]);
                    else if (columnIndex > StaticValues.FirstColumnNames.Length - 1 + StaticValues.SkillNamesProfessions1.Length)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(StaticValues.SkillNamesProfessions2[columnIndex - StaticValues.FirstColumnNames.Length - StaticValues.SkillNamesProfessions1.Length]);
                    break;
                case 1:
                    if (columnIndex > StaticValues.FirstColumnNames.Length - 1)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(StaticValues.SkillNamesCombat[columnIndex - StaticValues.FirstColumnNames.Length]);
                    break;
                case 2:
                    if (columnIndex > StaticValues.FirstColumnNames.Length - 1)
                        (sender as DataGridColumnHeader).ToolTip = resourceManager.GetString(StaticValues.AttributeNames[columnIndex - StaticValues.FirstColumnNames.Length]);
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
            if (e.Column.DisplayIndex == StaticValues.FirstColumnNames.Length - 1)
            {
                if (value.Length > 24)
                    value = value.Substring(0, 24);

                (e.EditingElement as TextBox).Text = value;
                var realIndex = Int32.Parse((dataGridProfessions.Columns[6].GetCellContent(e.Row) as TextBlock).Text);

                for (int i = 0; i < 3; i++)
                {
                    dataSetTables.Tables[i].Rows[realIndex].BeginEdit();
                    dataSetTables.Tables[i].Rows[realIndex][StaticValues.FirstColumnNames.Length - 1] = value;
                    dataSetTables.Tables[i].Rows[realIndex].EndEdit();
                }
            }
            else if (e.Column.DisplayIndex > StaticValues.FirstColumnNames.Length)
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
    }
}