using System.Windows;
using System.Windows.Input;

namespace ININ.Alliances.RecordingExportExample.View.Supporting
{
    public class CommandBindingHelpers : DependencyObject
    {
        #region RegisterCommandBindings Attached Property
        /// <summary>
        /// Any CommandBinding objects set on this Attached Property will be added to the UIElement object's CommandBindings property. Existing 
        /// CommandBinding objects (not part of this object) in the UIElement object's CommandBindings property will be preserved on any changes 
        /// to this object.
        /// </summary>
        public static DependencyProperty RegisterCommandBindingsProperty =
            DependencyProperty.RegisterAttached("RegisterCommandBindings", typeof(CommandBindingCollection), typeof(CommandBindingHelpers),
            new PropertyMetadata(null, OnRegisterCommandBindingChanged));

        public static void SetRegisterCommandBindings(UIElement element, CommandBindingCollection value)
        {
            if (element != null)
                element.SetValue(RegisterCommandBindingsProperty, value);
        }

        public static CommandBindingCollection GetRegisterCommandBindings(UIElement element)
        {
            return (element != null ? (CommandBindingCollection)element.GetValue(RegisterCommandBindingsProperty) : null);
        }

        /// <summary>
        /// Called when the Attached Property RegisterCommandBindings is changed. Its operation is to remove the old CommandBinding objects from the 
        /// UIElement and add the new ones to it while preserving any CommandBinding objects that may have been set on the UIElement from another source.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnRegisterCommandBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = sender as UIElement;
            if (element == null) return;

            var newBindings = (e.NewValue as CommandBindingCollection);
            var oldBindings = (e.OldValue as CommandBindingCollection);
            if (newBindings != null)
            {
                // Remove the old bindings
                if (oldBindings != null)
                {
                    foreach (CommandBinding commandBinding in oldBindings)
                    {
                        if (element.CommandBindings.Contains(commandBinding))
                        { element.CommandBindings.Remove(commandBinding); }
                    }
                }

                // Add the new bindings
                element.CommandBindings.AddRange(newBindings);
            }
        }
        #endregion



        #region BaseBindings Attached Property
        /// <summary>
        /// An Attached Property for storing a CommandBindingCollection on an object aside from CommandBindings
        /// </summary>
        public static DependencyProperty BaseBindingsProperty =
            DependencyProperty.RegisterAttached("BaseBindings", typeof(CommandBindingCollection), typeof(CommandBindingHelpers), new PropertyMetadata(new CommandBindingCollection()));

        public static CommandBindingCollection GetBaseBindings(DependencyObject element)
        {
            var val = element.GetValue(BaseBindingsProperty);
            return val as CommandBindingCollection;
        }
        public static void SetBaseBindings(DependencyObject element, CommandBindingCollection value)
        {
            element.SetValue(BaseBindingsProperty, value);
        }
        #endregion

    }
}