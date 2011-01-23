using System;
using System.Activities.Presentation.PropertyEditing;
using System.Windows;
using System.Windows.Input;

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

    public partial class PropertyValueEditorTemplates
    {
        public void TextBox_KeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.Enter)
            {
                var textBox = sender as System.Windows.Controls.TextBox;
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
            }
        }
    }
}
