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
        static string[] attributeNames;
        static CharacterAttributeType[] attributes;
        static Version programVersion = new Version("0.3.27");
        static Logger logger = LogManager.GetLogger("SampleLogger");
        static ConnectionViewModel viewModel = new ConnectionViewModel();
        
        /// <summary>
        /// Initialize global variables
        /// </summary>
        public static void Initialize()
        {
            Globals.Logger.Debug("Global variables initialization...");

            foreach (var skill in SkillDef.AllLaborSkills())
                viewModel.Skills.Add(new Skill(skill, 0, false));
            viewModel.Skills[viewModel.Skills.Count - 1].Name = "Hauling";

            //Создаем тру список скиллов
            //Making arrays of skills
            var attributeNamesTemp = new ArrayList(Enum.GetNames(typeof(CharacterAttributeType)));
            var attributesTemp = new ArrayList(Enum.GetValues(typeof(CharacterAttributeType)));

            attributeNamesTemp.RemoveAt(5); //count
            attributesTemp.RemoveAt(5);


            // Аттрибуты
            // attribute arrays
            attributes = (CharacterAttributeType[])attributesTemp.ToArray(typeof(CharacterAttributeType));
            attributeNames = (string[])attributeNamesTemp.ToArray(typeof(string));

            Globals.Logger.Debug("Global variables initialized");
        }

        public static int FirstColumnCount { get { return firstColumnNames; } }

        public static string[] AttributeNames { get { return attributeNames; } }

        public static CharacterAttributeType[] Attributes { get { return attributes; } }

        public static Version ProgramVersion { get { return programVersion; } }

        public static Logger Logger { get { return logger; } }

        public static ConnectionViewModel ViewModel { get { return viewModel; } }
    }
}
