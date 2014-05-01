using System;
using System.Windows.Data;
using System.Windows.Input;
using ININ.IceLib;

namespace ININ.Alliances.RecordingExportExample.View.Supporting
{
    public class CommandBindingCollectionConverter : IMultiValueConverter
    {
        /// <summary>
        /// Aggregates source CommandBindingCollection objects to a single CommandBindingCollection for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding"/> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue"/> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// An aggregated CommandBindingCollection object. An empty collection will be returned if the source values are empty or null.
        /// </returns>
        public object Convert(object[] values, Type targetType, object parameter,
                              System.Globalization.CultureInfo culture)
        {
            try
            {
                var commandBindingCollection = new CommandBindingCollection();
                foreach (object obj in values)
                {
                    if (obj is CommandBindingCollection)
                    {
                        commandBindingCollection.AddRange(obj as CommandBindingCollection);
                    }
                }
                return commandBindingCollection;
            }
            catch (Exception ex)
            {
                Tracing.TraceException(ex, ex.Message);
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
                                    System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}