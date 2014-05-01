using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using ININ.Alliances.RecordingExportExample.Annotations;
using ININ.IceLib;

namespace ININ.Alliances.RecordingExportExample.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected SynchronizationContext Context;

        public CommandBindingCollection CommandBindings { get; set; }

        public ViewModelBase()
        {
            // Get UI thread context; assumes all view models are instantiated on the UI thread
            Context = SynchronizationContext.Current;
            CommandBindings = new CommandBindingCollection();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Context.Send(s =>
            {
                try
                {
                    PropertyChangedEventHandler handler = PropertyChanged;
                    if (handler != null)
                    {
                        handler(this, new PropertyChangedEventArgs(propertyName));
                    }
                }
                catch (Exception ex)
                {
                    Tracing.TraceException(ex, ex.Message);
                }
            }, null);
        }

        public virtual void Dispose()
        {
            
        }
    }
}
