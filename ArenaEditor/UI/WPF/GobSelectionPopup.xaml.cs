using System.Collections.ObjectModel;
using System.Windows;
using AW2.Game;
using System.Collections.Generic;

namespace AW2.UI.WPF
{
    public partial class GobSelectionPopup : Window
    {
        public ObservableCollection<Gob> Gobs { get; set; }

        public GobSelectionPopup()
        {
            Gobs = new ObservableCollection<Gob>();
            InitializeComponent();
        }

        public void SetGobs(IEnumerable<Gob> gobs)
        {
            Gobs.Clear();
            foreach (var gob in gobs) Gobs.Add(gob);
        }
    }
}
