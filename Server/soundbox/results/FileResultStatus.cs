using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class FileResultStatus : ResultStatus
    {
        public static readonly FileResultStatus INVALID_DISPLAY_NAME_EXISTS = new FileResultStatus(1400, "Display name already exists in that directory");
        public static readonly FileResultStatus ILLEGAL_FILE_TYPE = new FileResultStatus(1401, "File type is not supported");
        public static readonly FileResultStatus FILE_DOES_NOT_EXIST = new FileResultStatus(1402, "File or directory does not exist");
        public static readonly FileResultStatus INVALID_FILE_NAME = new FileResultStatus(1403, "Given name for file or directory is invalid");

        public static readonly FileResultStatus IO_ERROR = new FileResultStatus(1500, "Error while writing a file");

        private FileResultStatus(int code, string message, bool success = false)
        {
            Code = code;
            Message = message;
            Success = success;
        }
    }
}
