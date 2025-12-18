using RFIDInventario.Server.Services;

namespace RFIDInventario.Server.Models
{
    public class FileParams
    {
        public class FileParam
        {
            public required string ColumnName { get; set; }
            public int Position { get; set; }
        }

        public class FileSetting
        {
            public required string FileName { get; set; }
            public required string FileSeparator { get; set; }
            public required List<FileParam> FileParams { get; set; }
            public ICarga? Service { get; set; }
        }

        public class FileParamsSettings
        {
            public required string DirectoryPath { get; set; }
            public required List<FileSetting> Files { get; set; }
        }
    }
}
