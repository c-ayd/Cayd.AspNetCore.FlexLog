using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.StressTest
{
    public class RequestModel
    {
        public int Id { get; set; }
        public string Str1 { get; set; } = null!;
        public string Str2 { get; set; } = null!;
        public string Str3 { get; set; } = null!;
        public string Str4 { get; set; } = null!;
        public string Str5 { get; set; } = null!;
        public int Int1 { get; set; }
        public int Int2 { get; set; }
        public int Int3 { get; set; }
        public int Int4 { get; set; }
        public int Int5 { get; set; }
        public NestedValue Nested { get; set; } = new NestedValue();

        public class NestedValue
        {
            public List<string> Strs { get; set; } = null!;
            public List<int> Ints { get; set; } = null!;
        }
    }
}
