using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// A <see cref="ResultStatus"/> that wraps another <see cref="ResultStatus"/> while supplying a custom error message.
    /// </summary>
    public class MessageResultStatus : ResultStatus
    {
        public MessageResultStatus(ResultStatus status, string message)
        {
            Code = status.Code;
            Message = message;
            Success = status.Success;
        }
    }
}
