﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class BaseResultStatus : ResultStatus
    {
        public static readonly BaseResultStatus OK = new BaseResultStatus(0, "OK", true);

        public static readonly BaseResultStatus INVALID_PARAMETER = new BaseResultStatus(-400, "Invalid or illegal parameter");
        public static readonly BaseResultStatus ACCESS_DENIED = new BaseResultStatus(-403, "Access denied");

        public static readonly BaseResultStatus INTERNAL_SERVER_ERROR = new BaseResultStatus(-500, "Internal server error");

        private BaseResultStatus(int code, string message, bool success = false)
        {
            Code = code;
            Message = message;
            Success = success;
        }
    }
}