using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Input;
using ININ.Alliances.RecordingExportExample.Model;
using ININ.Alliances.RecordingExportExample.Properties;
using ININ.IceLib.Connection;
using ININ.IceLib.QualityManagement;

namespace ININ.Alliances.RecordingExportExample.ViewModel
{
    public class ExportViewModel : ViewModelBase
    {

        private readonly Session _session;
        private readonly BackgroundWorker _exportWorker = new BackgroundWorker();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private DateTime _startDate = DateTime.Now.Date.AddDays(-1);
        private DateTime _endDate = DateTime.Now.Date;
        private bool _mediaCalls;
        private bool _mediaChats;
        private bool _mediaEmails;
        private string _exportDirectory;
        private int _progressMinimum = 0;
        private int _progressMaximum = 100;
        private int _progressValue = -1;
        private string _progressText = "";
        private bool _exportUseOriginalFileNames;


        public DateTime StartDate
        {
            get { return _startDate; }
            set
            {
                _startDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndDate
        {
            get { return _endDate; }
            set
            {
                _endDate = value;
                OnPropertyChanged();
            }
        }

        public bool MediaCalls
        {
            get { return _mediaCalls; }
            set
            {
                _mediaCalls = value;
                OnPropertyChanged();
            }
        }

        public bool MediaChats
        {
            get { return _mediaChats; }
            set
            {
                _mediaChats = value;
                OnPropertyChanged();
            }
        }

        public bool MediaEmails
        {
            get { return _mediaEmails; }
            set
            {
                _mediaEmails = value;
                OnPropertyChanged();
            }
        }

        public string EnabledMediaQueryPart
        {
            get
            {
                // 1=call; 2=chat; 3=email
                List<string> values = new List<string>();
                if (MediaCalls) values.Add("1");
                if (MediaChats) values.Add("2");
                if (MediaEmails) values.Add("3");

                if (values.Count > 0)
                    return "AND rm.MediaType IN (" + values.Aggregate((i, j) => i + "," + j) + ") ";
                else
                    return "";
            }
        }

        public string ExportDirectory
        {
            get { return _exportDirectory; }
            set
            {
                _exportDirectory = value;
                OnPropertyChanged();
            }
        }

        public bool ExportUseOriginalFileNames
        {
            get { return _exportUseOriginalFileNames; }
            set
            {
                _exportUseOriginalFileNames = value;
                OnPropertyChanged();
            }
        }

        public int ProgressMinimum
        {
            get { return _progressMinimum; }
            set
            {
                _progressMinimum = value;
                OnPropertyChanged();
            }
        }

        public int ProgressMaximum
        {
            get { return _progressMaximum; }
            set
            {
                _progressMaximum = value;
                OnPropertyChanged();
            }
        }

        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged();
                OnPropertyChanged("HasProgress");
            }
        }

        public string ProgressText
        {
            get { return _progressText; }
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }

        public bool HasProgress { get { return ProgressValue >= 0; } }


        public ExportViewModel(Session session)
        {
            _session = session;

            // Register command bindings for this view model
            CommandBindings.Add(new CommandBinding(UiCommands.ExportCommand, Export_Executed, Export_CanExecute));
            CommandBindings.Add(new CommandBinding(UiCommands.ExportTestCommand, ExportTest_Executed, ExportTest_CanExecute));

            _exportWorker.DoWork += ExportWorkerOnDoWork;
            _exportWorker.RunWorkerCompleted += ExportWorkerOnRunWorkerCompleted;

            // Load settings
            MediaCalls = Settings.Default.ExportMediaCalls;
            MediaChats = Settings.Default.ExportMediaChats;
            MediaEmails = Settings.Default.ExportMediaEmails;
            ExportDirectory = Settings.Default.ExportDirectory;
            ExportUseOriginalFileNames = Settings.Default.ExportUseOriginalFileNames;
        }



        #region Commanding

        private void ExportTest_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                // Always true; this is controlled via properties and style triggers
                e.CanExecute = true;
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }
        }

        private void ExportTest_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _exportWorker.RunWorkerAsync("test");
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }
        }

        private void Export_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            try
            {
                // Always true; this is controlled via properties and style triggers
                e.CanExecute = true;
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }
        }

        private void Export_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _exportWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }
        }

        #endregion



        #region PrivateMethods
        
        private void ExportWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs runWorkerCompletedEventArgs)
        {
            try
            {
                _stopwatch.Stop();
                if (_stopwatch.Elapsed.TotalSeconds > 60)
                    MainViewModel.Instance.LogMessage(string.Format("Export completed in {0} minutes and {1} seconds",
                        _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds));
                else
                    MainViewModel.Instance.LogMessage(string.Format("Export completed in {0} seconds",
                        _stopwatch.Elapsed.TotalSeconds));
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }
        }

        private void ExportWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                _stopwatch.Reset();
                _stopwatch.Start();

                var rm = QualityManagementManager.GetInstance(_session).RecordingsManager;
                List<DbDataRecord> records;
                // Make progress show
                ProgressValue = 0;
                ProgressText = "Querying...";

                // Query records
                //Instantiate the connection with the database 
                using (var myConnection = new SqlConnection(MainViewModel.Instance.ConnectionString))
                {
                    myConnection.Open();
                    var query =
                        "SELECT isum.InteractionIDKey, rm.MediaType, rm.CallType, isum.RemoteID, isum.RemoteNumberCallId, rm.RecordingId, " +
                        "CONVERT(datetime, SWITCHOFFSET(CONVERT(datetimeoffset, isum.StartDateTimeUTC), DATENAME(TzOffset, SYSDATETIMEOFFSET()))) AS LocalTime, " +
                        "rm.NumAttachments " +
                        "FROM InteractionSummary as isum, IR_RecordingMedia as rm " +
                        "WHERE isum.InteractionIDKey = rm.QueueObjectIdKey AND " +
                        "StartDateTimeUTC BETWEEN @StartTime AND @EndTime " + EnabledMediaQueryPart;
                    SqlCommand comm = new SqlCommand(query, myConnection);
                    //Add parameters to the query
                    comm.Parameters.AddWithValue("StartTime", StartDate.ToUniversalTime());
                    comm.Parameters.AddWithValue("EndTime", EndDate.ToUniversalTime());
                    //Execute the query
                    var reader = comm.ExecuteReader();

                    records = reader.Cast<DbDataRecord>().ToList();
                    reader.Close();
                }

                // Only continue of not testing
                if ("test".Equals(e.Argument))
                {
                    MainViewModel.Instance.LogMessage("Filter test completed. Filter returns " + records.Count +
                                                      " results.");
                    return;
                }
                else
                {
                    MainViewModel.Instance.LogMessage("Query returned " + records.Count + " results");
                }

                // Set up progress bar
                ProgressMaximum = records.Count - 1;

                // Ensure export directory exists
                if (!Directory.Exists(ExportDirectory))
                    Directory.CreateDirectory(ExportDirectory);

                // Export recordings
                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    ProgressText = string.Format("Processing {0}/{1}...", i + 1, records.Count);
                    ProgressValue = i;
                    MainViewModel.Instance.LogMessage("Exporting recording(s) for " + record["InteractionIDKey"]);

                    // Create file name
                    var filename = MakeFileName(record["InteractionIDKey"].ToString(), record["RecordingId"].ToString(),
                        record["MediaType"].ToString(), record["LocalTime"].ToString(), -1);

                    // Download primary media
                    DownloadFile(
                        rm.GetExportUri(record["RecordingId"].ToString(), RecordingMediaType.PrimaryMedia, "", 0),
                        Path.Combine(ExportDirectory, filename));

                    // Download additional media
                    if (record["MediaType"].ToString() != "3") continue; // Emails only
                    int attachmentCounter = 0;
                    int.TryParse(record["NumAttachments"].ToString(), out attachmentCounter);
                    for (int attachmentNum = 1; attachmentNum <= attachmentCounter; attachmentNum++)
                    {
                        ProgressText = string.Format("Processing {0}/{1} (Attachment {2}/{3})...", i + 1, records.Count,
                            attachmentNum, attachmentCounter);

                        filename = MakeFileName(record["InteractionIDKey"].ToString(), record["RecordingId"].ToString(),
                            record["MediaType"].ToString(), record["LocalTime"].ToString(), attachmentNum);
                        DownloadFile(
                            rm.GetExportUri(record["RecordingId"].ToString(), RecordingMediaType.EmailAttachment, "", attachmentNum),
                            Path.Combine(ExportDirectory, filename));
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }
            finally
            {
                ProgressValue = -1;
                ProgressMinimum = 0;
                ProgressMaximum = 100;
                ProgressText = "";
            }
        }

        private void DownloadFile(Uri uri, string filename)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(uri, filename);

                    // Loop through headers to find file info
                    /* Example header output:
                     * ----------------------
                     * content-disposition: attachment; filename=1001365309_1001365308_19af4b1d7064436e2a7a4c18.wav
                     * Content-Length: 8120
                     * Content-Type: audio/wav
                     * Date: Thu, 01 May 2014 15:50:22 GMT
                     * Server: ININ-i3http/1.2
                     */
                    for (int i = 0; i < client.ResponseHeaders.Count; ++i)
                    {
                        string header = client.ResponseHeaders.GetKey(i);
                        if (header == "content-disposition")
                        {
                            // Get header value
                            var values = client.ResponseHeaders.GetValues(i);
                            if (values == null || values.Length == 0) continue;

                            var newFilename = filename;
                            if (ExportUseOriginalFileNames)
                            {
                                // This will replace the generated filename with the name from the response header
                                newFilename = Path.Combine(ExportDirectory, values[0].Substring(values[0].LastIndexOf('=') + 1));
                            }
                            else
                            {
                                // This will add ".wav", or the like, to the existing filename
                                newFilename = filename + values[0].Substring(values[0].LastIndexOf('.'));
                            }

                            // Check to make sure it's not the same somehow, don't want to lose files
                            if (newFilename == filename) continue;

                            // Remove duplicates first (copying doesn't overwrite)
                            if (File.Exists(newFilename)) File.Delete(newFilename);

                            // Copy file to give new name with extension
                            File.Copy(filename, newFilename);

                            // Delete old file without extension
                            File.Delete(filename);
                        }

                        foreach (string value in client.ResponseHeaders.GetValues(i))
                        {
                            Console.WriteLine("{0}: {1}", header, value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }
        }

        private string MakeFileName(string interactionIdKey, string recordingId, string mediaType, 
            string recordingTimestamp, int sequenceNum)
        {
            // Determine timestamp
            DateTime recordingDateTime;
            string timestamp;
            if (DateTime.TryParse(recordingTimestamp, out recordingDateTime))
                timestamp = recordingDateTime.ToString();
            else
                timestamp = "";

            // Format sequence number
            var sequenceString = "";
            if (sequenceNum > 0)
                sequenceString = "_" + sequenceNum;

            // Media type and extension
            var mediaTypeString = "unknown";
            var extension = "txt";
            if (mediaType == "1")
            {
                mediaTypeString = "call";
                extension = "wav";
            }
            else if (mediaType == "2")
            {
                mediaTypeString = "chat";
                extension = "xml";
            }
            else if (mediaType == "3")
            {
                mediaTypeString = "email";
                extension = "txt";
            }

            // Format filename
            // NOTE: The extension is intentionally left off as it will be added after the file is downloaded
            return HelperModel.MakeValidFileName(string.Format("{0} [{1}] {2} ({3}){4}",
                interactionIdKey, recordingId, timestamp, mediaTypeString, sequenceString, extension));
        }

        #endregion



        #region Public Methods

        public override void Dispose()
        {
            try
            {
                Settings.Default.ExportMediaCalls = MediaCalls;
                Settings.Default.ExportMediaChats = MediaChats;
                Settings.Default.ExportMediaEmails = MediaEmails;
                Settings.Default.ExportDirectory = ExportDirectory;
                Settings.Default.ExportUseOriginalFileNames = ExportUseOriginalFileNames;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MainViewModel.Instance.LogMessage(ex);
            }

            base.Dispose();
        }

        #endregion
    }
}
