using System;
using System.Collections;
using System.Collections.Generic;
using GameLibrary;
using NLog;

namespace GnomeExtractor
{
    public class Globals
    {
        static int firstColumnNames = 3;
        static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "//Gnome Extractor";
        static Version programVersion = new Version("0.4");
        static Logger logger = LogManager.GetLogger("SampleLogger");
        static ConnectionViewModel viewModel = new ConnectionViewModel();
        
        /// <summary>
        /// Initialize global variables
        /// </summary>
        public static void Initialize()
        {
            Globals.Logger.Debug("Global variables initialization...");

            foreach (var skill in SkillDef.AllLaborSkills())
                viewModel.Skills.Add(new SkillEntry(skill, 0, false));
            viewModel.Skills[viewModel.Skills.Count - 1].Name = "Hauling";

            Globals.Logger.Debug("Global variables initialized");
        }

        public static int FirstColumnCount { get { return firstColumnNames; } }

        public static string AppDataPath { get { return appDataPath; } }

        public static Version ProgramVersion { get { return programVersion; } }

        public static Logger Logger { get { return logger; } }

        public static ConnectionViewModel ViewModel { get { return viewModel; } }
    }
}
