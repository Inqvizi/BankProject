using BankShared.Enums;
using System;

namespace BankShared.DTOs
{
    [Serializable]
    public class BaseRequest
    {
        public RequestType RequestType { get; set; }
        public string JsonPayload { get; set; }
    }
}