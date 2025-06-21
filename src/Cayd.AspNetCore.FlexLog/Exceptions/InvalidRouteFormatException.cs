using System;

namespace Cayd.AspNetCore.FlexLog.Exceptions
{
    public class InvalidRouteFormatException : Exception
    {
        public InvalidRouteFormatException(string option, string routeValue) :
            base($"Route value, {routeValue}, in {option} does not start with '/'.")
        { }
    }
}
