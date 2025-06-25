namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class TestService
    {
        private readonly IFlexLogger<TestService> _flexLogger;

        public TestService(IFlexLogger<TestService> flexLogger)
        {
            _flexLogger = flexLogger;
        }

        public void Log()
        {
            _flexLogger.LogInformation("Test info");
        }
    }
}
