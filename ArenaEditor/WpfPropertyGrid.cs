// *********************************************************************
// PLEASE DO NOT REMOVE THIS DISCLAIMER
//
// WpfPropertyGrid - By Jaime Olivares
// Article site: http://www.codeproject.com/KB/grid/WpfPropertyGrid.aspx
// Author site: www.jaimeolivares.com
// License: Code Project Open License (CPOL)
//
// *********************************************************************

using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace System.Windows.Controls
{
    /// <summary>WPF Native PropertyGrid class, uses Workflow Foundation's PropertyInspector</summary>
    public class WpfPropertyGrid : Grid
    {
        #region Private fields
        private WorkflowDesigner Designer;
        private MethodInfo RefreshMethod;
        private MethodInfo OnSelectionChangedMethod;
        private TextBlock SelectionTypeLabel;
        private Border HelpText;
        private GridSplitter Splitter;
        private object[] TheSelectedObjects = null;
        private bool ShowDescription;
        private double HelpTextHeight = 60;
        #endregion

        #region Public properties
        /// <summary>Get or sets the selected object. Can be null.</summary>
        public object SelectedObject
        {
            get
            {
                if (this.TheSelectedObjects == null || this.TheSelectedObjects.Length == 0)
                    return null;
                else
                    return this.TheSelectedObjects[0];
            }
            set
            {
                if (value != null)
                {
                    this.TheSelectedObjects = new object[] { value };

                    var context = new EditingContext();
                    var mtm = new ModelTreeManager(context);
                    mtm.Load(value);
                    Selection selection = Selection.Select(context, mtm.Root);

                    OnSelectionChangedMethod.Invoke(Designer.PropertyInspectorView, new object[] { selection });
                    this.SelectionTypeLabel.Text = value.GetType().Name;
                }
                else
                {
                    this.TheSelectedObjects = null;
                    OnSelectionChangedMethod.Invoke(Designer.PropertyInspectorView, new object[] { null });
                    this.SelectionTypeLabel.Text = string.Empty;
                }

                ChangeHelpText(string.Empty, string.Empty);
            }
        }
        /// <summary>Get or sets the selected object collection. Returns empty array by default.</summary>
        public object[] SelectedObjects
        {
            get
            {
                if (this.TheSelectedObjects == null || this.TheSelectedObjects.Length == 0)
                    return new object[] { };
                else
                    return this.TheSelectedObjects.Clone() as object[];
            }
            set
            {
                this.TheSelectedObjects = value;

                if (TheSelectedObjects == null || TheSelectedObjects.Length == 0)
                {
                    this.SelectedObject = null;
                }
                else if (TheSelectedObjects.Length == 1)
                {
                    this.SelectedObject = TheSelectedObjects[0];
                }
                else
                {
                    //object[] array = value as object[];
                    bool same = true;
                    Type first = null;

                    var context = new EditingContext();
                    var mtm = new ModelTreeManager(context);
                    Selection selection = null;

                    for (int i = 0; i < TheSelectedObjects.Length; i++)
                    {
                        mtm.Load(TheSelectedObjects[i]);
                        if (i == 0)
                        {
                            selection = Selection.Select(context, mtm.Root);
                            first = this.TheSelectedObjects[0].GetType();
                        }
                        else
                        {
                            selection = Selection.Union(context, mtm.Root);
                            if (!this.TheSelectedObjects[i].GetType().Equals(first))
                                same = false;
                        }
                    }

                    OnSelectionChangedMethod.Invoke(Designer.PropertyInspectorView, new object[] { selection });
                    ChangeHelpText(string.Empty, string.Empty);

                    this.SelectionTypeLabel.Text = same ? first.Name + " <multiple>" : "Object <multiple>";
                }
            }
        }
        /// <summary>XAML information with PropertyGrid's font and color information</summary>
        /// <seealso>Documentation for WorkflowDesigner.PropertyInspectorFontAndColorData</seealso>
        public string FontAndColorData
        {
            set
            {
                Designer.PropertyInspectorFontAndColorData = value;
            }
        }
        /// <summary>Shows the description area on the top of the control</summary>
        public bool HelpVisible
        {
            get
            {
                return this.ShowDescription;
            }
            set
            {
                if (value && !this.ShowDescription)
                {
                    this.RowDefinitions[1].Height = new GridLength(5);
                    this.RowDefinitions[2].Height = new GridLength(this.HelpTextHeight);
                }
                else if (!value && this.ShowDescription)
                {
                    this.HelpTextHeight = this.RowDefinitions[2].Height.Value;
                    this.RowDefinitions[1].Height = new GridLength(0);
                    this.RowDefinitions[2].Height = new GridLength(0);
                }
                this.ShowDescription = value;
            }
        }
        #endregion

        /// <summary>Default constructor, creates the UIElements including a PropertyInspector</summary>
        public WpfPropertyGrid()
        {
            this.ColumnDefinitions.Add(new ColumnDefinition());
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });

            this.Designer = new WorkflowDesigner();
            TextBlock title = new TextBlock()
            {
                Visibility = Windows.Visibility.Visible,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = FontWeights.Bold
            };
            TextBlock descrip = new TextBlock()
            {
                Visibility = Windows.Visibility.Visible,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            DockPanel dock = new DockPanel()
            {
                Visibility = Windows.Visibility.Visible,
                LastChildFill = true,
                Margin = new Thickness(3, 0, 3, 0)
            };

            title.SetValue(DockPanel.DockProperty, Dock.Top);
            dock.Children.Add(title);
            dock.Children.Add(descrip);
            this.HelpText = new Border()
            {
                Visibility = Windows.Visibility.Visible,
                BorderBrush = SystemColors.ActiveBorderBrush,
                Background = SystemColors.ControlBrush,
                BorderThickness = new Thickness(1),
                Child = dock
            };
            this.Splitter = new GridSplitter()
            {
                Visibility = Windows.Visibility.Visible,
                ResizeDirection = GridResizeDirection.Rows,
                Height = 5,
                HorizontalAlignment = Windows.HorizontalAlignment.Stretch
            };

            var inspector = Designer.PropertyInspectorView;
            inspector.Visibility = Visibility.Visible;
            inspector.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);

            this.Splitter.SetValue(Grid.RowProperty, 1);
            this.Splitter.SetValue(Grid.ColumnProperty, 0);

            this.HelpText.SetValue(Grid.RowProperty, 2);
            this.HelpText.SetValue(Grid.ColumnProperty, 0);

            Binding binding = new Binding("Parent.Background");
            title.SetBinding(BackgroundProperty, binding);
            descrip.SetBinding(BackgroundProperty, binding);

            this.Children.Add(inspector);
            this.Children.Add(this.Splitter);
            this.Children.Add(this.HelpText);

            Type inspectorType = inspector.GetType();
            var props = inspectorType.GetProperties(Reflection.BindingFlags.Public | Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance |
                Reflection.BindingFlags.DeclaredOnly);
            this.RefreshMethod = inspectorType.GetMethod("RefreshPropertyList",
                Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance | Reflection.BindingFlags.DeclaredOnly);
            this.OnSelectionChangedMethod = inspectorType.GetMethod("OnSelectionChanged",
                Reflection.BindingFlags.Public | Reflection.BindingFlags.Instance | Reflection.BindingFlags.DeclaredOnly);
            this.SelectionTypeLabel = inspectorType.GetMethod("get_SelectionTypeLabel",
                Reflection.BindingFlags.Public | Reflection.BindingFlags.NonPublic | Reflection.BindingFlags.Instance |
                Reflection.BindingFlags.DeclaredOnly).Invoke(inspector, new object[0]) as TextBlock;
            inspectorType.GetEvent("GotFocus").AddEventHandler(this,
                Delegate.CreateDelegate(typeof(RoutedEventHandler), this, "GotFocusHandler", false));

            this.SelectionTypeLabel.Text = string.Empty;
        }

        /// <summary>Updates the PropertyGrid's properties</summary>
        public void RefreshPropertyList()
        {
            RefreshMethod.Invoke(Designer.PropertyInspectorView, new object[] { false });
        }

        /// <summary>Traps the change of focused property and updates the help text</summary>
        /// <param name="sender">Not used</param>
        /// <param name="args">Points to the source control containing the selected property</param>
        private void GotFocusHandler(object sender, RoutedEventArgs args)
        {
            //if (args.OriginalSource is TextBlock)
            {
                string title = string.Empty;
                string descrip = string.Empty;
                if (this.TheSelectedObjects != null && this.TheSelectedObjects.Length > 0)
                {
                    Type first = this.TheSelectedObjects[0].GetType();
                    for (int i = 1; i < this.TheSelectedObjects.Length; i++)
                    {
                        if (!this.TheSelectedObjects[i].GetType().Equals(first))
                        {
                            ChangeHelpText(title, descrip);
                            return;
                        }
                    }

                    object data = (args.OriginalSource as FrameworkElement).DataContext;
                    PropertyInfo propEntry = data.GetType().GetProperty("PropertyEntry");
                    if (propEntry == null)
                    {
                        propEntry = data.GetType().GetProperty("ParentProperty");
                    }

                    if (propEntry != null)
                    {
                        object propEntryValue = propEntry.GetValue(data, null);
                        string propName = propEntryValue.GetType().GetProperty("PropertyName").GetValue(propEntryValue, null) as string;
                        title = propEntryValue.GetType().GetProperty("DisplayName").GetValue(propEntryValue, null) as string;
                        var property = TypeDescriptor.GetProperties(TheSelectedObjects[0]).Cast<PropertyDescriptor>().First(p => p.Name == propName);
                        var attrs = property.Attributes.OfType<DescriptionAttribute>();
                        if (attrs.Any()) descrip = attrs.First().Description;
                    }
                    ChangeHelpText(title, descrip);
                }
            }
        }

        /// <summary>Changes the text help area contents</summary>
        /// <param name="title">Title in bold</param>
        /// <param name="descrip">Description with ellipsis</param>
        private void ChangeHelpText(string title, string descrip)
        {
            DockPanel dock = this.HelpText.Child as DockPanel;
            (dock.Children[0] as TextBlock).Text = title;
            (dock.Children[1] as TextBlock).Text = descrip;
        }
    }
}
