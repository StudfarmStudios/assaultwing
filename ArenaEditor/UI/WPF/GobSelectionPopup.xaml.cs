﻿using System.Collections.ObjectModel;
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

        public void AddRange(IEnumerable<Gob> gobs)
        {
            foreach (var gob in gobs) Gobs.Add(gob);
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            Close();
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Close();
        }
    }
}