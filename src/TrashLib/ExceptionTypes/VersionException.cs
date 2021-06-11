using System;

namespace TrashLib.ExceptionTypes
{
    public class VersionException : Exception
    {
        public VersionException(string msg)
            : base(msg)
        {
        }
    }
}
