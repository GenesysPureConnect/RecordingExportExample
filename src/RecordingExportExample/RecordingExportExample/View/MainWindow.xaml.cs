using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using ININ.Alliances.RecordingExportExample.ViewModel;

namespace ININ.Alliances.RecordingExportExample.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = MainViewModel.Instance;

            InitializeComponent();

            CicPasswordBox.Password = Marshal.PtrToStringUni(Marshal.SecureStringToGlobalAllocUnicode(MainViewModel.Instance.CicPassword));
            DbPasswordBox.Password = Marshal.PtrToStringUni(Marshal.SecureStringToGlobalAllocUnicode(MainViewModel.Instance.DbPassword));
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            MainViewModel.Instance.Disconnect();
        }

        private void CicPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.CicPassword = CicPasswordBox.SecurePassword;
        }

        private void DbPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            MainViewModel.Instance.DbPassword = DbPasswordBox.SecurePassword;
        }
    }
}
