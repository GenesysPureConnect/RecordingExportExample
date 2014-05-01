using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using ININ.Alliances.RecordingExportExample.Properties;
using ININ.Alliances.RecordingExportExample.ViewModel;

namespace ININ.Alliances.RecordingExportExample.Model
{
    public static class HelperModel
    {
        // This string of numbers is arbitrary; it only adds entropy to the encryption
        private static readonly byte[] SAditionalEntropy = { 1, 5, 7, 2, 8, 4, 9, 3 };

        // Keys for dynamic settings
        public static readonly string SettingKeyMediaCalls = "Media_Calls";
        public static readonly string SettingKeyMediaChats = "Media_Chats";
        public static readonly string SettingKeyMediaEmails = "Media_Emails";
        public static readonly string SettingKeyExportDirectory = "ExportDirectory";


        public static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        public static SecureString ConvertToSecureString(string strPassword)
        {
            var secureStr = new SecureString();
            if (strPassword.Length > 0)
            {
                foreach (var c in strPassword.ToCharArray()) secureStr.AppendChar(c);
            }
            return secureStr;
        }

        public static void LoadSettings(MainViewModel main)
        {
            try
            {
                if (Settings.Default.UpgradeRequired)
                {
                    // The default value is true, which will mean that a config file needs to be upgraded (or does not exist)
                    // so we should try to upgrade to get previous values.
                    Settings.Default.Upgrade();
                    // Set to false so that we know we have data
                    Settings.Default.UpgradeRequired = false;
                    // Save the settings
                    Settings.Default.Save();
                }

                // Set current values to Settings values
                main.CicUsername = Settings.Default.CicUsername;
                main.CicPassword = UnprotectPassword(Settings.Default.CicPassword);
                main.CicServer = Settings.Default.CicServer;
                main.DbUsername = Settings.Default.DbUsername;
                main.DbPassword = UnprotectPassword(Settings.Default.DbPassword);
                main.DbServer = Settings.Default.DbServer;
                main.DbName = Settings.Default.DbName;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void SaveSettings(MainViewModel main)
        {
            try
            {
                // Set Settings to current values
                Settings.Default.CicUsername = main.CicUsername;
                Settings.Default.CicPassword = ProtectPassword(main.CicPassword);
                Settings.Default.CicServer = main.CicServer;
                Settings.Default.DbUsername = main.DbUsername;
                Settings.Default.DbPassword = ProtectPassword(main.DbPassword);
                Settings.Default.DbServer = main.DbServer;
                Settings.Default.DbName = main.DbName;

                // Save Settings
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string ProtectPassword(SecureString password)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted 
                // only by the same current user. 
                return
                    Convert.ToBase64String(
                        ProtectedData.Protect(
                            GetBytes(Marshal.PtrToStringUni(Marshal.SecureStringToGlobalAllocUnicode(password))),
                            SAditionalEntropy,
                            DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not encrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(IntPtr.Zero);
            }
        }

        private static SecureString UnprotectPassword(string encryptedPassword)
        {
            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser. 
                return
                    ConvertToSecureString(
                        GetString(ProtectedData.Unprotect(Convert.FromBase64String(encryptedPassword),
                            SAditionalEntropy, DataProtectionScope.CurrentUser)));
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not decrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}
