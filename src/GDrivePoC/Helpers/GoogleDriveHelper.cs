using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GDrivePoC.Helpers
{
    public class GoogleDriveHelper
    {
        public static string[] Scopes = { DriveService.Scope.Drive };
        public static string ApplicationName = "Scain PoC Quickstart";

        private DriveService Service => GetService();

        /// <summary>
        /// Create the service for accessing Google Drive
        /// </summary>
        private DriveService GetService()
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "Scain.Intranet",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Return Drive API service.
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        /// <summary>
        /// Builds a Google File object for uploading content to Google Drive
        /// </summary>
        private Google.Apis.Drive.v3.Data.File BuildDriveFile(string filePath, string parents)
        {
            return new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(filePath),
                MimeType = MimeTypes.GetMimeType(filePath),
                Parents = new[] { parents }
            };
        }

        /// <summary>
        /// File Upload to the Google Drive root folder.
        /// </summary>
        public async Task<string> UploadFileAsync(string file, IProgress<long> progress)
        {
            var destination = @"Temp/Propuestas/SCAIN Consultoría/PPC-025-2021";
            var parentId = await CheckFolderStructureOnDrive(destination);

            IUploadProgress result = null;
            FilesResource.CreateMediaUpload request;

            var gFile = BuildDriveFile(file, parentId);
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                request = Service.Files.Create(gFile, stream, gFile.MimeType);
                request.ChunkSize = ResumableUpload.MinimumChunkSize * 4;
                request.ProgressChanged += (p) => progress.Report(p.BytesSent * 100 / stream.Length);
                request.Fields = "id, webContentLink, name";
                result = request.Upload();

                // Handling uploading:
                if (result.Status != UploadStatus.Completed)
                {
                    var rdn = new Random();
                    var waitTime = 0;
                    var count = 0;
                    do
                    {
                        waitTime = (Convert.ToInt32(Math.Pow(2, count)) * 1000) + rdn.Next(0, 1000);
                        Thread.Sleep(waitTime);
                        result = request.Upload();
                        count++;
                    } while (count < 5 && (result.Status != UploadStatus.Completed));
                }
            }
            if (result.Status != UploadStatus.Completed)
                throw result.Exception;

            var fileUploaded = request.ResponseBody;
            return fileUploaded.WebContentLink;

        }

        /// <summary>
        /// Checks the folder structure on google drive and retrives the id for the lower folder on the structure where the file is going to be updated
        /// </summary>
        /// <returns>string</returns>
        public async Task<string> CheckFolderStructureOnDrive(string filePath)
        {
            var elements = filePath.Split(@"/");
            var folders = new List<DriveFolder>();

            foreach (var element in elements)
            {
                string parent = null;
                try
                {
                    var index = elements.GetIndexOf(element);

                    if (folders.Any())
                        parent = folders[index - 1].Id;

                    var folderId = await SearchFolderOnDrive(element, parent);
                    folders.Add(new DriveFolder { Id = folderId, Name = element });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return folders.ElementAt(folders.Count - 1).Id;
        }

        /// <summary>
        /// Allows to find the folder on the drive and retrieves its Id
        /// </summary>
        /// <returns>string</returns>
        private async Task<string> SearchFolderOnDrive(string folderName, string parent = null)
        {
            var request = Service.Files.List();
            request.PageSize = 10;
            request.Q = $"{Constants.FolderMediaTypeSearch} and name = '{folderName}'";
            if (!string.IsNullOrEmpty(parent))
            {
                request.Q += $" and '{parent}' in parents";
            }
            request.Q += $" and trashed = false";
            request.Fields = Constants.SearchRequestFields;

            var files = (await request.ExecuteAsync()).Files;
            if (files.Any())
                return files.FirstOrDefault().Id;
            else
                return await CreateFolder(folderName, parent);
        }

        /// <summary>
        /// Creates a folder once its non existence has been verified
        /// </summary>
        /// <returns>string</returns>
        private async Task<string> CreateFolder(string folderName, string parent)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = Constants.GoogleMimeType,
                Parents = new[] { parent }
            };
            var request = Service.Files.Create(fileMetadata);
            request.Fields = Constants.FolderFields;
            var folder = await request.ExecuteAsync();
            return folder.Id;
        }
    }
}
