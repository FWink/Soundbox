using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Base class for the result status of client->server operations.<br/>
    /// Implementing class MUST make sure to have globally unique <see cref="Code"/>s
    /// </summary>
    public abstract class ResultStatus
    {
        /// <summary>
        /// A machine-readable result code (e.g. "access denied", "IO error", "file not found"...)
        /// </summary>
        public int Code
        {
            get;
            protected set;
        }

        /// <summary>
        /// Optional: A human-readable result message that explains the <see cref="Code"/>
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message
        {
            get;
            protected set;
        }

        /// <summary>
        /// Whether this result represents a success.
        /// </summary>
        [JsonIgnore]
        public bool Success
        {
            get;
            protected set;
        }
    }
}
