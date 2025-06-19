using System.Text.Json;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class ResponseModel
    {
        public JsonDocument Value { get; set; } = null!;
        public NestedResponseModel Nested { get; set; } = null!;

        public class NestedResponseModel
        {
            public JsonDocument Secret { get; set; } = null!;
        }
    }
}
