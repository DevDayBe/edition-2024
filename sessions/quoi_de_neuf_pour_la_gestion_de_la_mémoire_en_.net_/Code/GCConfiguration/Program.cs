using System;

namespace GCConfiguration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // get part of the GC configuration
            //
            Console.WriteLine("GC configuration");
            Console.WriteLine("----------------------------------");
            foreach (var variable in GC.GetConfigurationVariables())
            {
                Console.WriteLine($"{variable.Key} = {variable.Value}");
            }
        }
    }
}
