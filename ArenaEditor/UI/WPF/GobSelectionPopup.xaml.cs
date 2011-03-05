using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using AW2.Game;

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

        /// <summary>
        /// Set the gob list and select the first item.
        /// </summary>
        public void SetGobs(IEnumerable<Gob> gobs)
        {
            Gobs.Clear();
            foreach (var gob in gobs) Gobs.Add(gob);
            if (Gobs.Any()) GobList.SelectedIndex = 0;
        }
    }
}
