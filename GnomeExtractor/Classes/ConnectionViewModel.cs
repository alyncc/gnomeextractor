using System.Collections.Generic;
using System.ComponentModel;
using Game;

namespace GnomeExtractor
{
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        List<Gnome> gnomes = new List<Gnome>();
        List<Profession> professions = new List<Profession>();
        List<Skill> skills = new List<Skill>();

        public List<Gnome> Gnomes
        { get { return this.gnomes; } set { this.gnomes = value; OnPropertyChanged("Gnomes"); } }

        public List<Profession> Professions
        { get { return this.professions; } set { this.professions = value; OnPropertyChanged("Professions"); } }

        public List<Skill> Skills
        { get { return this.skills; } set { this.skills = value; OnPropertyChanged("Skills"); } }



        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
