using System;

namespace SharpMessaging.BenchmarkApp
{
    public class CommandLineArgumentAttribute : Attribute
    {
        public char ShortName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MetaValue { get; set; }
        public string DefaultValue { get; set; }
    }

}