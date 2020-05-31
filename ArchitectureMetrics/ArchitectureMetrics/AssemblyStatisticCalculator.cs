using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;

namespace ArchitectureMetrics
{
	//var path = Path.Combine(Directory.GetCurrentDirectory(), @"bin\Debug\netcoreapp2.1");
	//new AssemblyStatisticCalculator().Calculate(path, "TCO.TEARECS", x => false);

	//var path = Path.Combine(Directory.GetCurrentDirectory(), @"bin\Debug\netcoreapp3.0");
	//Func<Type, bool> dtoCheck = (x) =>
	//{
	//if (AssemblyStatisticCalculator.IsAnonymousType(x)) return false;
	//if (!x.FullName.Contains("Cqrs")) return false;
	//if (x.Name.EndsWith("ViewModel") || x.Name.EndsWith("Query") || x.Name.EndsWith("Command")) return true;
	//return AssemblyStatisticCalculator.IsPocoType(x);
	//};
	//new AssemblyStatisticCalculator().Calculate(path, "ContestantRegister", dtoCheck);
    
    //var path = Path.Combine(Directory.GetCurrentDirectory(), @"bin\Debug\netcoreapp3.1");
    //new AssemblyStatisticCalculator().Calculate(path, "Max", x => x.Name.EndsWith("Dto"));
	
	//var path = @"c:\Users\dtjd\Desktop\PravoToDenis_no_git_hub\Sites\CaseMap\bin\";
	//new AssemblyStatisticCalculator().Calculate(path, new []{ "CaseMap", "CaseDotStar" }, null, @"c:\Users\dtjd\Desktop\Stat.csv");

	public class AssemblyStatisticCalculator
	{
		private static readonly HashSet<string> BaseMethods = new HashSet<string> { "ToString", "Equals", "GetHashCode", "GetType" };

        public void Calculate(string path, string assemblyNamePrefix, Func<Type, bool> dtoCheckerFunc = null, string outputFilePath = "AssemblyStatistic.csv")
        {
            Calculate(path, new[] {assemblyNamePrefix}, dtoCheckerFunc, outputFilePath);
        }

		public void Calculate(string path, string[] assemblyNamePrefixes, Func<Type, bool> dtoCheckerFunc = null, string outputFilePath = "AssemblyStatistic.csv")
        {
            var assemblies = assemblyNamePrefixes.SelectMany(x => Directory.EnumerateFiles(path, $"{x}*dll"))
                .Select(Assembly.LoadFile);
			
            var stats = new List<AssemblyStat>();
			var referencedCount = new Dictionary<string, int>();
			foreach (var assembly in assemblies)
			{
				var assemblyStat = new AssemblyStat { Name = assembly.GetName().Name };
                
                var references = assembly.GetReferencedAssemblies()
                    .Where(x => assemblyNamePrefixes.Any(prefix => x.Name.StartsWith(prefix)))
                    .ToList();
				assemblyStat.References = references.Count;
				
                foreach (var reference in references)
				{
					if (referencedCount.ContainsKey(reference.Name))
					{
						referencedCount[reference.Name] = referencedCount[reference.Name] + 1;
					}
                    else
                    {
                        referencedCount[reference.Name] = 1;
                    }
				}

				var types = assembly.GetTypes();
				assemblyStat.Interfaces = types.Count(x => x.IsInterface);
				assemblyStat.AbstractClasses = types.Count(x => x.IsClass && x.IsAbstract && !x.IsSealed);
				//https://stackoverflow.com/questions/2639418/use-reflection-to-get-a-list-of-static-classes
				assemblyStat.StaticClassesWithoutMethods = types.Count(x => x.IsClass && x.IsAbstract && x.IsSealed &&
																	x.GetMethods().All(m => BaseMethods.Contains(m.Name)));
				assemblyStat.StaticClassesWithMethods = types.Count(x => x.IsClass && x.IsAbstract && x.IsSealed &&
																	x.GetMethods().Any(m => !BaseMethods.Contains(m.Name)));
				assemblyStat.Structs = types.Count(x => x.IsValueType && !x.IsEnum);
				assemblyStat.Enums = types.Count(x => x.IsEnum);
				assemblyStat.Exceptions = types.Count(x => typeof(Exception).IsAssignableFrom(x));
				assemblyStat.Events = types.Count(x => typeof(Delegate).IsAssignableFrom(x));
				assemblyStat.EventArgs = types.Count(x => typeof(EventArgs).IsAssignableFrom(x));

                if (dtoCheckerFunc != null)
                {
                    assemblyStat.DTOs = types.Count(dtoCheckerFunc);
				}

				//Not anonymous 
				assemblyStat.TotalClasses = types.Count(x => !IsAnonymousType(x));

				stats.Add(assemblyStat);
			}

			foreach (var p in referencedCount)
			{
				var stat = stats.First(x => x.Name == p.Key);
				stat.Referenced = p.Value;
			}

			using (var writer = new StreamWriter(outputFilePath))
			using (var csv = new CsvWriter(writer, false))
			{
				csv.WriteRecords(stats);
			}
		}

        public static bool IsAnonymousType(Type type)
        {
            return type.Name.Contains('_') || type.Name.Contains('<') || type.Name.Contains('>');
        }

		public static bool IsPocoType(Type type)
        {
			if (!type.IsClass || type.IsAbstract) return false;
			if (type.GetFields().Any() ||
				type.GetMethods().Any(x => !x.Name.StartsWith("get_") && !x.Name.StartsWith("set_") && !BaseMethods.Contains(x.Name)) ||
				type.GetEvents().Any() ||
				type.GetConstructors().Length != 1)
				return false;

			return type.GetProperties().All(x => x.CanRead && x.CanWrite);
		}
	}
}
