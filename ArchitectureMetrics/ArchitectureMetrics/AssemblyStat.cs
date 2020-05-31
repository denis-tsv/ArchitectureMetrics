using System;

namespace ArchitectureMetrics
{
    public class AssemblyStat
    {
        public string Name { get; set; }

        public double Instability => 1.0 * References / (References + Referenced);
        public double Abstractness => 1.0 * (Interfaces + AbstractClasses + Exceptions + DTOs) / TotalClasses;
        public double Distance => Math.Abs(Instability + Abstractness - 1);

        public int References { get; set; }
        public int Referenced { get; set; }
        
        public int Interfaces { get; set; }
        public int AbstractClasses { get; set; }
        public int StaticClassesWithoutMethods { get; set; }
        public int StaticClassesWithMethods { get; set; }
        public int Structs { get; set; }
        public int Events { get; set; }
        public int Exceptions { get; set; }
        public int DTOs { get; set; }
        public int Enums { get; set; }
        public int EventArgs { get; set; }
        public int TotalClasses { get; set; }

        
    }
}
