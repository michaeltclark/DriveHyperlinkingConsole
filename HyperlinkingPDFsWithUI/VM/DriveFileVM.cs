using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HyperlinkingPDFsWithUI
{
    public class DriveFileVM : BaseVM
    {
        private File _file;

        /// <summary>
        /// The prefix that Google Drive uses with PDF files to share a link. Final URL: {pdfPrefix}{file id}
        /// </summary>
        private const string _pdfPrefix = @"https://drive.google.com/file/d/";

        /// <summary>
        /// Google drive unique identifier for the file
        /// </summary>
        public string FileId { get { return _file.Id; } }

        /// <summary>
        /// Name of the file on Drive
        /// </summary>
        public string FileName { get { return _file.Title; } }

        /// <summary>
        /// Original File Name on Drive.
        /// </summary>
        public string OriginalFileName { get { return _file.OriginalFilename; } }

        /// <summary>
        /// Command to open the file url.
        /// </summary>
        public ICommand OpenSheetUrl { get; set; }

        /// <summary>
        /// The filetype determined from the MIME Type. 
        /// </summary>
        public DriveFileType DriveFileType
        {
            get
            {
                switch (_file.MimeType)
                {
                    case "application/pdf":
                        return DriveFileType.PDF;
                    case "application/vnd.google-apps.spreadsheet":
                        return DriveFileType.GSheet;
                    case "application/vnd.google-apps.folder":
                        return DriveFileType.GFolder;
                    default:
                        return DriveFileType.Other;
                }
            }
        }

        /// <summary>
        /// Returns the drive link dependent on the type of file
        /// </summary>
        public string DriveLink
        {
            get
            {
                switch (DriveFileType)
                {
                    case DriveFileType.PDF:
                        return $"{_pdfPrefix}{FileId}";
                    default:
                        return "Not a PDF";
                }
            }
            set { }
        }

        /// <summary>
        /// Determines if the hyperlink should be clickable.
        /// </summary>
        public bool IsLinkEnabled{ get { return DriveFileType == DriveFileType.PDF ? true : false; } }

        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="thisChild"></param>
        public DriveFileVM(ChildReference thisChild, DriveService driveService)
        {
            OpenSheetUrl = new RelayCommand(OpenSheetUrlMethod);

            // Get more information about the file using the child Id
            try
            {
                _file = driveService.Files.Get(thisChild.Id).Execute();
            }
            catch (Exception)
            {
                _file = null;
            }
        }

        /// <summary>
        /// Opens the drive link of the file.
        /// </summary>
        private void OpenSheetUrlMethod()
        {
            System.Diagnostics.Process.Start(DriveLink);
        }
    }
}
