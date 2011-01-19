using System;
using System.Activities.Presentation.PropertyEditing;
using System.Windows;

namespace AW2.UI.WPF.PropertyValueEditors
{
    public class RotationEditor : PropertyValueEditor
    {
        public RotationEditor()
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("/UI/WPF/PropertyValueEditors/PropertyValueEditorTemplates.xaml", UriKind.RelativeOrAbsolute)
            };
            InlineEditorTemplate = (DataTemplate)dictionary["RotationEditorTemplate"];
        }
    }
}
