using System;
using System.Windows;
using AW2.Core;
using AW2.Helpers;
using AW2.UI.WPF;

namespace AW2
{
    public class ArenaEditorProgram : IDisposable
    {
        private ArenaEditorWindow _editor;

        /// <summary>
        /// The main entry point for Assault Wing Arena Editor.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            using (var program = new ArenaEditorProgram(args))
            {
                program.Run();
            }
        }

        public ArenaEditorProgram(string[] args)
        {
            Log.Write("Assault Wing Arena Editor started");
            CanonicalString.IsForLocalUseOnly = true;
            _editor = new ArenaEditorWindow(args);
        }

        public void Run()
        {
            if (Application.Current == null) new Application();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.Run(_editor);
        }

        public void Dispose()
        {
            if (_editor != null)
            {
                _editor.Dispose();
                _editor = null;
            }
        }
    }
}
