using System.Activities.Presentation.PropertyEditing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System;

namespace AW2.UI.WPF.PropertyValueEditors
{
    public class Vector2Editor : PropertyValueEditor
    {
        public Vector2Editor()
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("/UI/WPF/PropertyValueEditors/PropertyValueEditorTemplates.xaml", UriKind.RelativeOrAbsolute)
            };
            InlineEditorTemplate = (DataTemplate)dictionary["Vector2EditorTemplate"];
        }
    }
}
