using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Generic result base class for client->server operations (e.g. uploading a file, creating a directory, playing a sound...)<br/>
    /// Contains at the very least:<list type="bullet">
    /// <item><see cref="Success"/>: whether the operation succeeded or not</item>
    /// <item><see cref="ResultStatus.Code"/>: a machine-readable result code (e.g. "access denied", "IO error", "file not found"...)</item>
    /// </list>
    /// Optionally contains <see cref="ResultStatus.Message"/>: a human-readable result message that explains the <see cref="ResultStatus.Code"/>.<br/>
    /// Furthermore may contain any operation-specific data in specialized classes of this type.
    /// </summary>
    public class ServerResult
    {
        public bool Success
        {
            get;
        }
        public ResultStatus Status
        {
            get;
        }

        public ServerResult(ResultStatus status)
        {
            Status = status;
            Success = status.Success;
        }

        public ServerResult(ResultStatus status, string message) : this(new MessageResultStatus(status, message)) { }
    }
}
