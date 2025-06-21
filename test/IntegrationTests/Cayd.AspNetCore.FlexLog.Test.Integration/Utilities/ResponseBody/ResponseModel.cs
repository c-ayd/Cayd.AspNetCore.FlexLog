namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.ResponseBody
{
    public class ResponseModel
    {
        public string MyString { get; set; } = null!;
        public int MyInt { get; set; }
        public NestedValue Nested { get; set; } = null!;
        
        public class NestedValue
        {
            public string MyString { get; set; } = null!;
        }
    }
}
