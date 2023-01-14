using System;

namespace AW2.Settings
{
    public class SystemSettings
    {
        public string ArenaEditorDefaultDirectory { get; set; }

        public SystemSettings()
        {
            Reset();
        }

        public void Reset()
        {
            ArenaEditorDefaultDirectory = Environment.CurrentDirectory;
        }
    }
}
