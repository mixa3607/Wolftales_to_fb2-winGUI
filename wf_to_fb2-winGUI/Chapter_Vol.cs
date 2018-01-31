using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace wf_to_fb2
{
    public class Vol : INotifyPropertyChanged
    {
        private bool? checked_;
        public bool? Checked  { get { return checked_; } set { checked_ = value; OnPropertyChanged("Checked"); } }
        public bool ThreeState { get; set; }
        public string Text { get; set; }
        public ObservableCollection<Chapter> Chapters { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
    public class Chapter : INotifyPropertyChanged
    {
        private bool checked_ ;
        public int InVolume { get; set; }
        public bool Checked { get { return checked_; } set { checked_ = value; OnPropertyChanged("Checked"); } }
        public string Text { get; set; }
        public string URL { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

}
