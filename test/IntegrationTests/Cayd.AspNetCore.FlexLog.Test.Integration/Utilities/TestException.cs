using System;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class TestException : Exception
    {
        public TestException(string message) : base(message) { }
    }
}
