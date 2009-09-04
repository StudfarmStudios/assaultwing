using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AW2
{
    /// <summary>
    /// Assault Wing Arena Editor main window.
    /// </summary>
    public partial class ArenaEditor : Window
    {
        public ArenaEditor()
        {
            InitializeComponent();
        }

        private void LoadArenaClick(object sender, RoutedEventArgs e)
        {
            var arenaToLoad = arenaName.Text;
            if (arenaToLoad == "") return;
            var data = AssaultWing.Instance.DataEngine;
            data.ProgressBar.Task = () => data.InitializeFromArena(arenaToLoad);
            data.ProgressBar.StartTask();
            while (!data.ProgressBar.TaskCompleted) System.Threading.Thread.Sleep(100);
            data.ProgressBar.FinishTask();
            AssaultWing.Instance.StartArena();
        }
    }
}
