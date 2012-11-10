using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameLibrary;

namespace GnomeExtractor
{
    public static class StaticValues
    {
        const int skillsFightingFirstIndex = 29; // Индекс первого вхождения боевых скиллов             // Index of first element of fight skills
        const int skillsFightingFirstCount = 10; // Количество боевых скиллов в первом вхождении        // Count of fight skills in first block
        const int skillsProfessionFirstCount = 29; // Кол-во первого блока скиллов-профессий            // Count of profession-skills in first block
        static int tabControlSelectedIndex;
        static string[] firstColumnNames;
        static string[] skillsProfessionNames1;
        static string[] skillsProfessionNames2;
        static string[] skillsFightingNames;
        static string[] attributeNames;
        static CharacterSkillType[] skillsProfession1;
        static CharacterSkillType[] skillsProfession2;
        static CharacterSkillType[] skillsFighting;
        static CharacterAttributeType[] attributes;
        
        /// <summary>
        /// Инициализировать значения вспомогательного класса StaticValues
        /// </summary>
        public static void Initialize()
        {
            //Создаем тру список скиллов
            //Making arrays of skills
            var skillsNamesTemp = new ArrayList(Enum.GetNames(typeof(CharacterSkillType)));
            var skillsTemp = new ArrayList(Enum.GetValues(typeof(CharacterSkillType)));
            var attributeNamesTemp = new ArrayList(Enum.GetNames(typeof(CharacterAttributeType)));
            var attributesTemp = new ArrayList(Enum.GetValues(typeof(CharacterAttributeType)));

            skillsTemp.RemoveAt(44); // Count
            skillsTemp.RemoveAt(39); // Discipline
            skillsTemp.RemoveAt(30); // LaborEnd
            skillsTemp.RemoveAt(0);  // LaborStart
            // заменять удаление скилла, на присваивание корректного имени. не терять очередность, потому что при удалении элементы сдвигаются.
            // replase removeing skills assigment good name. dont forget about sequence, it is very important
            skillsNamesTemp.RemoveAt(44);
            skillsNamesTemp.RemoveAt(39);
            skillsNamesTemp[31] = "Fighting"; //NaturalAttack
            skillsNamesTemp.RemoveAt(30);
            skillsNamesTemp.RemoveAt(0);

            attributeNamesTemp.RemoveAt(5);//count
            attributesTemp.RemoveAt(5);

            // массивы скиллов
            // skill arrays
            skillsFighting = (CharacterSkillType[])skillsTemp.GetRange(skillsFightingFirstIndex, skillsFightingFirstCount).ToArray(typeof(CharacterSkillType));
            skillsTemp.RemoveRange(skillsFightingFirstIndex, skillsFightingFirstCount);
            skillsProfession1 = (CharacterSkillType[])skillsTemp.GetRange(0, skillsProfessionFirstCount).ToArray(typeof(CharacterSkillType));
            skillsTemp.RemoveRange(0, skillsProfessionFirstCount);
            skillsProfession2 = (CharacterSkillType[])skillsTemp.ToArray(typeof(CharacterSkillType));

            // массивы имен скиллов
            // skill name arrays
            skillsFightingNames = (string[])skillsNamesTemp.GetRange(skillsFightingFirstIndex, skillsFightingFirstCount).ToArray(typeof(string));
            skillsNamesTemp.RemoveRange(skillsFightingFirstIndex, skillsFightingFirstCount);
            skillsProfessionNames1 = (string[])skillsNamesTemp.GetRange(0, skillsProfessionFirstCount).ToArray(typeof(string));
            skillsNamesTemp.RemoveRange(0, skillsProfessionFirstCount);
            skillsProfessionNames2 = (string[])skillsNamesTemp.ToArray(typeof(string));

            // Аттрибуты
            // attribute arrays
            attributes = (CharacterAttributeType[])attributesTemp.ToArray(typeof(CharacterAttributeType));
            attributeNames = (string[])attributeNamesTemp.ToArray(typeof(string));

            // Первичные столбцы
            // first columns
            firstColumnNames = new string[] { "X", "Y", "Z", "Num", "AllowedSkills1", "AllowedSkills2", "RealIndex", "Name", "Profession" };
        }

        public static int TabControlSelectedIndex { get { return tabControlSelectedIndex; } set { tabControlSelectedIndex = value; } }

        public static string[] FirstColumnNames { get { return firstColumnNames; } }

        public static string[] SkillNamesProfessions1 { get { return skillsProfessionNames1; } }

        public static string[] SkillNamesProfessions2 { get { return skillsProfessionNames2; } }

        public static string[] SkillNamesCombat { get { return skillsFightingNames; } }

        public static string[] AttributeNames { get { return attributeNames; } }

        public static CharacterSkillType[] SkillsProfessions1 { get { return skillsProfession1; } }

        public static CharacterSkillType[] SkillsProfessions2 { get { return skillsProfession2; } }

        public static CharacterSkillType[] SkillsCombat { get { return skillsFighting; } }

        public static CharacterAttributeType[] Attributes { get { return attributes; } }
    }
}
