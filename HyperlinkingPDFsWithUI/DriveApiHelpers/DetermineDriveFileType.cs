using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperlinkingPDFsWithUI
{
    internal static class DetermineDriveFileType
    {
        /// <summary>
        /// Determines the filetype based on the input file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static DriveFileType DetermineFiletype(Google.Apis.Drive.v2.Data.File file)
        {
            switch (file.MimeType)
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
}
