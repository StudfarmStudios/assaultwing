using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AW2.Game;

namespace AW2.UI.WPF
{
    public partial class GobSelectionList : ListBox
    {
        public ObservableCollection<Gob> Gobs { get; set; }

        public GobSelectionList()
        {
            Gobs = new ObservableCollection<Gob>();
            InitializeComponent();
        }

        /// <summary>
        /// Sets the gob list and select the first item.
        /// </summary>
        public void SetGobs(IEnumerable<Gob> gobs)
        {
            Gobs.Clear();
            foreach (var gob in gobs) Gobs.Add(gob);
            if (Gobs.Any()) SelectedIndex = 0;
        }
    }
}
