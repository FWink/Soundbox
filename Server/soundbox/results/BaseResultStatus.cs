using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class BaseResultStatus : ResultStatus
    {
        public static readonly BaseResultStatus OK = new BaseResultStatus(0, "OK", true);
        /// <summary>
        /// Server elected not to perform a certain change (thus broadcasting that change to clients) because effectively there was no modification anyway.
        /// </summary>
        public static readonly BaseResultStatus OK_NO_CHANGE = new BaseResultStatus(-1, "OK but no change has been performed: before and after are the same", true);

        public static readonly BaseResultStatus INVALID_PARAMETER = new BaseResultStatus(-400, "Invalid or illegal parameter");
        /// <summary>
        /// Access to the server is denied: user is not logged in.
        /// </summary>
        public static readonly BaseResultStatus ACCESS_DENIED = new BaseResultStatus(-403, "Access denied: not logged in");
        /// <summary>
        /// Access to a specific function was denied (e.g. deleting sounds) because the user is lacking permission (to the entire feature).
        /// </summary>
        /// <seealso cref="RESSOURCE_DENIED"/>
        public static readonly BaseResultStatus PERMISSION_DENIED = new BaseResultStatus(-461, "Permission denied: no permission for function");
        /// <summary>
        /// Access to a specific ressource was denied (e.g. deleting a sound in a restricted directory) because the user is lacking permission on the ressource.
        /// </summary>
        /// <seealso cref="PERMISSION_DENIED"/>
        public static readonly BaseResultStatus RESSOURCE_DENIED = new BaseResultStatus(-462, "Ressource denied: no permission for function on ressource");

        public static readonly BaseResultStatus INTERNAL_SERVER_ERROR = new BaseResultStatus(-500, "Internal server error");

        private BaseResultStatus(int code, string message, bool success = false)
        {
            Code = code;
            Message = message;
            Success = success;
        }
    }
}
