using FsCheck;
using JsonToolkit.STJ.Tests.Properties;

namespace JsonToolkit.STJ.Tests
{
    /// <summary>
    /// Test setup and configuration for JsonToolkit.STJ tests.
    /// </summary>
    public static class TestSetup
    {
        static TestSetup()
        {
            // Register custom generators for property-based testing
            Arb.Register<JsonObjectGen>();
        }
    }
}