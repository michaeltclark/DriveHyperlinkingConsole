using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HyperlinkingPDFsWithUI
{
    public class MainVM : BaseVM
    {
        private List<DriveFileVM> driveFileVMs;

        // App Name. Displayed to the user when authenticating. 
        static string _applicationName = "Bay View PDF Manipulation";

        // Sheet range to append values, should match the object size.
        private static readonly string _sheetRange = $"DONOTRENAME!A:E";

        /// <summary>
        /// Google Auth Credential.
        /// </summary>
        public UserCredential GoogleCredentials { get; set; }

        // If modifying these scopes, delete your previously saved credentials at ~/.credentials/drive-dotnet-quickstart.json
        static string[] Scopes = { DriveService.Scope.Drive, SheetsService.Scope.Spreadsheets };

        #region GoogleDriveService
        private static DriveService _driveService;

        /// <summary>
        /// Returns the drive service used to read/write information to the drive that's provisioned. 
        /// </summary>
        public DriveService DriveService
        {
            get
            {
                return _driveService ??=
                    new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = GoogleCredentials,
                        ApplicationName = _applicationName
                    });
            }
        }
        #endregion


        #region GoogleSheetsService
        private static SheetsService _sheetsService;

        /// <summary>
        /// Returns the SheestService used to read/write to the google sheet. 
        /// </summary>
        public SheetsService SheetsService
        {
            get
            {
                return _sheetsService ??=
                    new SheetsService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = GoogleCredentials,
                        ApplicationName = _applicationName
                    });
            }
        }
        #endregion

        /// <summary>
        /// The directory of the credentials.json file
        /// </summary>
        public string CredentialDirectory { get; set; }

        /// <summary>
        /// Spreadsheet ID copied from the sheets URL.
        /// </summary>
        public string SpreadsheetId { get; set; }

        /// <summary>
        /// Drive folder Id copied from the URL.
        /// </summary>
        public string DriveFolderId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ProgressValue { get; set; }

        #region AsyncCommands
        /// <summary>
        /// Command bound to the GetFilesFromDrive Method.
        /// </summary>
        public IAsyncCommand GetFilesFromDrive { get; set; }

        /// <summary>
        /// Command bound to the TryGetSpreadsheetById Method.
        /// </summary>
        public IAsyncCommand TryGetSpreadsheetById { get; set; }


        /// <summary>
        /// Command bound to the SetCredentialsLocation Method.
        /// </summary>
        public IAsyncCommand SetCredentialsLocation { get; set; }

        //public ICommand TryGetFolderById { get; set; }
        public IAsyncCommand TryGetFolderById { get; set; }

        #endregion

        /// <summary>
        /// Determines tha authorization state of the user.
        /// </summary>
        public bool IsAuthorized { get { return GoogleCredentials == null ? false : true; } }

        public bool IsSpreadSheetSelected { get; set; }

        public bool IsFolderSelected { get; set; }

        public bool IsProgressEnabled { get; set; }

        /// <summary>
        /// Collection of the Drive files in memory.
        /// </summary>
        public ObservableCollection<DriveFileVM> DriveFiles { get; set; }

        /// <summary>
        /// Base Constructor
        /// </summary>
        public MainVM()
        {
            // Register Commands
            SetCredentialsLocation = AsyncCommand.Create(token => SetCredentailLocationMethod(token));
            GetFilesFromDrive = AsyncCommand.Create(token => GetFilesFromDriveAsync(token));
            TryGetSpreadsheetById = AsyncCommand.Create(token => TryGetSpreadsheetByIdAsync(token));
            TryGetFolderById = AsyncCommand.Create(token => TryGetFolderByIdAsync(token));


            // Set the credential filepath to the sotred settings on start. 
            CredentialDirectory = Properties.Settings.Default.CredentialFilepath;

            // Set the spreadsheet Id to the stored value
            SpreadsheetId = Properties.Settings.Default.SpreadsheetId;

            // Set the folder Id to the stored value
            DriveFolderId = Properties.Settings.Default.FolderId;

            // Try to use the stored settings to authorize the user and find the folder and spreadsheet. 
            try
            {
                using (FileStream stream = new FileStream(CredentialDirectory, FileMode.Open))
                {
                    WebAuthorizeGoogleDrive(stream);
                }

                TryGetSpreadsheetByIdMethod();
                TryGetFolderByIdMethod();
            }
            catch (Exception)
            {

            }

            IsProgressEnabled = false;
        }


        #region CredentailsAndOAuth

        private async Task SetCredentailLocationMethod(object token)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.InitialDirectory = Properties.Settings.Default.CredentialFilepath;
            dialog.ShowDialog();

            try
            {
                using (FileStream stream = (FileStream)dialog.OpenFile())
                {
                    // The file token.json stores the user's access and refresh tokens, and is created automatically when the authorization flow completes for the first time.
                    await Task.Factory.StartNew(() => WebAuthorizeGoogleDrive(stream));
                }
            }
            catch (Exception)
            {
                CredentialDirectory = "Error parsing file for credentials, please choose a credentials.json file.";
            }
        }

        /// <summary>
        /// Uses the provided credentials to authorize the end user.
        /// </summary>
        private void WebAuthorizeGoogleDrive(FileStream stream)
        {
            try
            {
                string credPath = "token.json";
                GoogleCredentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;

                // Show the name in the credentials browser.
                CredentialDirectory = stream.Name;
            }
            catch (Exception ex)
            {
                CredentialDirectory = ex.Message;
            }
        }
        #endregion

        #region SpreadsheetId

        /// <summary>
        /// Async helper for the get method
        /// </summary>
        private async Task TryGetSpreadsheetByIdAsync(object token)
        {
            IsSpreadSheetSelected = false;
            await Task.Factory.StartNew(() => TryGetSpreadsheetByIdMethod());
        }

        /// <summary>
        /// Tests if the user specified Spreadsheet is valid
        /// </summary>
        private void TryGetSpreadsheetByIdMethod()
        {
            try
            {
                Google.Apis.Drive.v2.Data.File file = DriveService.Files.Get(SpreadsheetId).Execute();

                // Save this setting for future use.
                Properties.Settings.Default.SpreadsheetId = file.Id;
                Properties.Settings.Default.Save();

                IsSpreadSheetSelected = true;
            }
            catch (Exception)
            {
                SpreadsheetId = "Incorrect spreadsheet Id specified";
            }
        }
        #endregion

        #region DriveFolderId
        /// <summary>
        /// Async helper for the get method
        /// </summary>
        private async Task TryGetFolderByIdAsync(object token)
        {
            IsFolderSelected = false;
            await Task.Factory.StartNew(() => TryGetFolderByIdMethod());
        }

        /// <summary>
        /// Tests if the user specified Spreadsheet is valid
        /// </summary>
        private void TryGetFolderByIdMethod()
        {
            try
            {
                Google.Apis.Drive.v2.Data.File file = DriveService.Files.Get(DriveFolderId).Execute();

                // If the file is a Drive Folder
                if (DetermineDriveFileType.DetermineFiletype(file) == DriveFileType.GFolder)
                {
                    // Update the UI
                    IsFolderSelected = true;

                    // Save this setting for future use.
                    Properties.Settings.Default.FolderId = file.Id;
                    Properties.Settings.Default.Save();
                }
                else // The user entered a valid Id that is not a folder.
                {
                    IsFolderSelected = false;
                    DriveFolderId = $"Id {file.Id} is not a folder";
                }
            } // User entered invalid or empty Id
            catch (Exception ex)
            {
                DriveFolderId = "Incorret Folder Id Specified";
            }
        }

        #endregion


        private async Task GetFilesFromDriveAsync(object token)
        {
            IsProgressEnabled = true;

            // Clear the colleciton.
            DriveFiles = new ObservableCollection<DriveFileVM>();

            // Clear the helper collection. 
            driveFileVMs = new List<DriveFileVM>();

            // Gets a VM representation for each file in the specified drive folder.
            await Task.Factory.StartNew(() => GetFilesFromDriveMethod());

            // Add the temp list items to the main list.
            foreach (var item in driveFileVMs)
            {
                DriveFiles.Add(item);
            }

            // Puts the drive files into the GSheet if they are not there already (by Id). 
            await Task.Factory.StartNew(() => PutDriveFilesInGSheet());

            IsProgressEnabled = false;
        }

        // Get files from the specified drive folder.
        private void GetFilesFromDriveMethod()
        {
            // Request all the children of the specified folder.
            ChildrenResource.ListRequest listRequest = DriveService.Children.List(DriveFolderId);

            do
            {
                try
                {
                    // Execute the child list request.
                    ChildList children = listRequest.Execute();

                    foreach (ChildReference child in children.Items)
                    {
                        driveFileVMs.Add(new DriveFileVM(child, DriveService));
                    }

                    // set the token for the next page of the request. 
                    listRequest.PageToken = children.NextPageToken;
                }
                catch (Exception ex)
                {
                    listRequest.PageToken = null;
                }
            } while (!String.IsNullOrEmpty(listRequest.PageToken));
        }


        private void PutDriveFilesInGSheet()
        {
            // Get the sheets currently in the GSheet.

            // Define request parameters.
            SpreadsheetsResource.ValuesResource.GetRequest request = SheetsService.Spreadsheets.Values.Get(SpreadsheetId, _sheetRange);

            // Temp list of each row in the spreadsheet
            IList<IList<Object>> gSheetRowsExisting = request.Execute().Values;

            // Temp list of each new row to add to the spreadsheet.
            IList<IList<Object>> gSheetRowsNew = new List<IList<object>>();

            // If the existing response returns with values.
            if (gSheetRowsExisting != null && gSheetRowsExisting.Count > 0)
            {
                foreach (DriveFileVM file in DriveFiles)
                {
                    // If no row exists with the specified Id, add a new row to the spreadsheet. 
                    if (gSheetRowsExisting.Where(row => row[0].ToString() == file.FileId).Count()==0)
                    {
                        gSheetRowsNew.Add(new List<object>() { file.FileId, file.FileName, file.DriveFileType, file.DriveLink });
                    }
                    else if (gSheetRowsExisting.Where(row => row[0].ToString() == file.FileId).Count() == 1) // If a sheet already exists
                    {

                        // Get the spreadsheet version of the file to be renamed.
                        var gSheetDriveFile = gSheetRowsExisting.Where(row => row[0].ToString() == file.FileId).First();

                        // Get the google drive file corresponding to the file in the sheet.
                        Google.Apis.Drive.v2.Data.File driveFileToBeRenamed = DriveService.Files.Get(gSheetDriveFile[0].ToString()).Execute();

                        // Set this to null as it can't be overwritten. 
                        driveFileToBeRenamed.Id = null;

                        string newName = gSheetDriveFile[1].ToString();

                        // Only rename the file if it has a new name.
                        if (driveFileToBeRenamed.Title != newName)
                        {
                            driveFileToBeRenamed.Title = newName;
                            FilesResource.UpdateRequest updateRequest = DriveService.Files.Update(driveFileToBeRenamed, gSheetDriveFile[0].ToString());
                            updateRequest.Execute();
                        }
                    }
                }
            }
            else // Sheet is empty
            {
                foreach (DriveFileVM file in DriveFiles)
                {
                    gSheetRowsNew.Add(new List<object>() { file.FileId, file.FileName, file.DriveFileType, file.DriveLink, file.OriginalFileName });
                }
            }

            // The values to append into the range. 
            var valueRange = new ValueRange
            {
                Values = gSheetRowsNew
            };

            // Create a request to append the values in the spreadsheet, adds new values as new lines. 
            var appendRequest = SheetsService.Spreadsheets.Values.Append(valueRange, SpreadsheetId, _sheetRange);

            // The data should be interpreted as user entered.
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            // Execute the request
            var appendResponse = appendRequest.Execute();
        }

        private void RenameDriveFilesFromSheet()
        {

        }
    }
}
