using System;

namespace AW2.Settings
{
    public class SystemSettings
    {
        private string _arenaEditorDefaultDirectory;

        public string ArenaEditorDefaultDirectory { get { return _arenaEditorDefaultDirectory; } set { _arenaEditorDefaultDirectory = value; } }

        public SystemSettings()
        {
            Reset();
        }

        public void Reset()
        {
            _arenaEditorDefaultDirectory = Environment.CurrentDirectory;
        }
    }
}
