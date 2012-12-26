using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using Game;

namespace GnomeExtractor
{
    //public class ProfessionEntry
    //{
    //    public string Name { get; set; }
    //    public ProfessionEntry(string name)
    //    {
    //        Name = name;
    //    }
    //}

    public class ConnectionViewModel : INotifyPropertyChanged
    {
        private CollectionView _professionEntries;
        private string _professionEntry;

        public ConnectionViewModel(GnomanEmpire gnomanEmpire)
        {
            //IList<Profession> list = new List<Profession>();
            //list = gnomanEmpire.Fortress.Professions
            //gnomanEmpire.Fortress.Professions
            _professionEntries = new CollectionView(gnomanEmpire.Fortress.Professions);
        }

        public CollectionView ProfessionEntries
        {
            get { return _professionEntries; }
        }

        public string ProfessionEntry
        {
            get { return _professionEntry; }
            set
            {
                if (_professionEntry != value)
                {
                    _professionEntry = value;
                    OnPropertyChanged("professionEntry");
                }
            }
        }

        //public void Add(Profession entry)
        //{
        //    ArrayList list = new ArrayList();
        //    list.Add(entry);
        //    foreach (var item in _professionEntries.SourceCollection)
        //        list.Add(item as Profession);
        //    list.Sort(new ProfessionsComparer());
        //    _professionEntries = new CollectionView(list);
        //    OnPropertyChanged("professionEntries");
        //}

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    class ProfessionsComparer : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            Profession a = (Profession)x;
            Profession b = (Profession)y;
            return String.Compare(a.Title, b.Title);
        }
    }
}
