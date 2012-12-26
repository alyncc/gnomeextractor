using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Game;
using GameLibrary;

namespace GnomeExtractor
{
    public class Gnome : INotifyPropertyChanged
    {
        string name;
        int id;
        int level;
        int row;
        int column;
        int position;
        Profession profession;
        int mining;
        List<Skill> laborSkills = new List<Skill>();
        List<Skill> combatSkills = new List<Skill>();

        public Gnome(Character gnome, int level, int row, int column, int position, int index)
        {
            this.name = gnome.Name();
            this.id = index;
            this.level = level;
            this.row = row;
            this.column = column;
            this.position = position;
            this.profession = gnome.Mind.Profession;
            this.mining = gnome.SkillLevel(CharacterSkillType.Mining);
            foreach (var skill in SkillDef.AllLaborSkills())
                this.laborSkills.Add(new Skill(skill, gnome.SkillLevel(skill), gnome.Mind.IsSkillAllowed(skill)));
            foreach (var skill in SkillDef.AllCombatSkills())
                this.combatSkills.Add(new Skill(skill, gnome.SkillLevel(skill), gnome.Mind.IsSkillAllowed(skill)));
        }

        public int Level
        { get { return this.level; } }

        public int Row
        { get { return this.row; } }

        public int Column
        { get { return this.row; } }

        public int Position
        { get { return this.position; } }

        public int ID
        { get { return this.id; } }

        public string Name
        { get { return this.name; } set { this.name = value; OnPropertyChanged("Name"); } }

        public Profession Profession
        { get { return this.profession; } set { this.profession = value; OnPropertyChanged("Profession"); } }

        public int Mining
        { get { return this.mining; } set { this.mining = value; OnPropertyChanged("Mining"); } }

        public List<Skill> LaborSkills
        { get { return this.laborSkills; } }

        public List<Skill> CombatSkills
        { get { return this.combatSkills; } }

        public void SetAllowedSkills(SkillGroup allowedSkills)
        {
            profession.AllowedSkills = allowedSkills;
            foreach (var skill in laborSkills)
                skill.IsAllowed = allowedSkills.IsSkillAllowed(skill.Type);
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

    }

    public class Skill : INotifyPropertyChanged
    {
        int level;
        string name;
        CharacterSkillType type;
        bool isAllowed;

        public Skill(CharacterSkillType type, int level, bool isAllowed)
        {
            this.name = type.ToString();
            this.level = level;
            this.type = type;
            this.isAllowed = isAllowed;
        }

        public CharacterSkillType Type
        { get { return this.type; } }

        public bool IsAllowed
        { get { return this.isAllowed; } set { this.isAllowed = value; OnPropertyChanged("IsAllowed"); } }

        public int Level
        { get { return this.level; } set { this.level = value; OnPropertyChanged("Level"); } }

        public string Name
        { get { return this.name; } set { this.name = value; OnPropertyChanged("Name"); } }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
