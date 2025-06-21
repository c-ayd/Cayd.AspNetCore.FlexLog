namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.RequestBody
{
    public class RequestModel
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public NestedValue Nested { get; set; } = null!;

        public class NestedValue
        {
            public string MyString { get; set; } = null!;
        }
    }
}
