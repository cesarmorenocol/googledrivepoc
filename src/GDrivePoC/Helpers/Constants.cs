namespace GDrivePoC.Helpers
{
    public static class Constants
    {
        public const string GoogleMimeType = "application/vnd.google-apps.folder";
        public static string FolderMediaTypeSearch => $"mimeType = '{GoogleMimeType}'";
        public const string SearchRequestFields = "nextPageToken, files(id, name, parents, version, driveId)";
        public const string FolderFields = "id";
    }
}
