using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ININ.Alliances.RecordingExportExample.ViewModel
{
    public static class UiCommands
    {
        public static RoutedUICommand LogInCommand = new RoutedUICommand("Log In", "LogInCommad", typeof(UiCommands));
        public static RoutedUICommand ExportCommand = new RoutedUICommand("Export", "ExportCommand", typeof(UiCommands));
        public static RoutedUICommand ExportTestCommand = new RoutedUICommand("Export Test", "ExportTestCommand", typeof(UiCommands));
    }
}
