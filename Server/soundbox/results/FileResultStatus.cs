using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class FileResultStatus : ResultStatus
    {
        protected const ResultStatusSegment Segment = ResultStatusSegment.File;

        public static readonly FileResultStatus UPLOAD_ABORTED = new FileResultStatus(1000, "Upload aborted by user");
        public static readonly FileResultStatus INVALID_DISPLAY_NAME_EXISTS = new FileResultStatus(1400, "Display name already exists in that directory");
        public static readonly FileResultStatus ILLEGAL_FILE_TYPE = new FileResultStatus(1401, "File type is not supported");
        public static readonly FileResultStatus FILE_DOES_NOT_EXIST = new FileResultStatus(1402, "File or directory does not exist");
        public static readonly FileResultStatus INVALID_FILE_NAME = new FileResultStatus(1403, "Given name for file or directory is invalid");
        public static readonly FileResultStatus ILLEGAL_FILE_EDIT_DENIED = new FileResultStatus(1404, "Given file may not be modified");
        public static readonly FileResultStatus ILLEGAL_FILE_EDIT_DENIED_ROOT = new FileResultStatus(1405, "Given file is the root directory. It may not be modified");
        public static readonly FileResultStatus MOVE_TARGET_INVALID = new FileResultStatus(1406, "Cannot move file to target: file and target directory are the same");

        public static readonly FileResultStatus IO_ERROR = new FileResultStatus(1500, "Error while writing a file");

        private FileResultStatus(int code, string message, bool success = false)
        {
            Code = code;
            Message = message;
            Success = success;
        }
    }
}
