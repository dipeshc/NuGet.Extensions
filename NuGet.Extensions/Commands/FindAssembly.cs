﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Commands;
using System.ComponentModel.Composition;
using NuGet.Common;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using NuGet.Extras.Repositories;
using NuGet.Extras.ExtensionMethods;

namespace NuGet.Extensions.GetLatest.Commands
{
    [Command("findassembly", "Finds which package an assembly file exists within, can be run on a single file or on a full directory." +
                             " Optionally provides the ability to output a packages.config file that satisfies the directory of dll's", MinArgs = 1)]
    public class FindAssembly : Command
    {
        private readonly List<string> _sources = new List<string>();
        public IPackageRepositoryFactory RepositoryFactory { get; set; }
        public IPackageSourceProvider SourceProvider { get; set; }
        private RepositoryAssemblyResolver resolver;
        private IFileSystem fileSystem;

        [Option("A list of sources to search")]
        public ICollection<string> Source
        {
            get { return _sources; }
        }

        [Option("Exhaustive search, otherwise we stop on first occurence")]
        public bool Exhaustive { get; set; }

        [Option("Check all versions, by default only the latest will be searched")]
        public bool AllVersions { get; set; }

        [Option("Output results to a packages.config file", AltName = "f")]
        public bool OutputPackageConfig { get; set; }

        [ImportingConstructor]
        public FindAssembly(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
        {
            Contract.Assert(packageRepositoryFactory != null);
            Contract.Assert(sourceProvider != null);

            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
        }

        public override void ExecuteCommand()
        {
            if (!string.IsNullOrEmpty(Arguments[0]))
            {
                List<string> assemblies = new List<string>();
                if (!Arguments[0].EndsWith(".dll"))
                {
                    fileSystem = CreateFileSystem(Directory.GetCurrentDirectory());
                    assemblies.AddRange(GetAssemblyListFromDirectory());
                }
                else
                {
                    assemblies.Add(Arguments[0]);
                }


                Stopwatch sw = new Stopwatch();
                sw.Start();
                var repository = GetRepository();
                IQueryable<IPackage> packageSource = GetPackageList(repository);
                resolver = new RepositoryAssemblyResolver(assemblies, packageSource, fileSystem, Console);

                Dictionary<string, List<IPackage>> assemblyLocations = new Dictionary<string, List<IPackage>>();
                if (assemblies.Count > 0)
                {
                    assemblyLocations = resolver.ResolveAssemblies(Exhaustive);
                }

                Console.WriteLine();
                sw.Stop();

                if (!OutputPackageConfig)
                {
                    OutputResultsToConsole(sw, assemblyLocations);
                }
                else
                {
                    resolver.OutputPackageConfigFile();
                    OutputErrors(assemblyLocations);
                }

                Environment.Exit(0);

            }
        }

        private void OutputErrors(Dictionary<string, List<IPackage>> assemblyLocations)
        {
            var notFound = assemblyLocations.Where(l => l.Value.Count == 0);
            if (notFound.Count() > 0)
            {
                Console.WriteLine("Could not find the following assemblies in any packages on the provided source:");
                foreach (var a in notFound)
                {
                    Console.WriteLine("{0}".PadLeft(15), a.Key);
                }
            }
        }


        private void OutputResultsToConsole(Stopwatch sw, Dictionary<string, List<IPackage>> assemblyLocations)
        {
            foreach (var a in assemblyLocations)
            {
                Console.WriteLine("Search results for : {0}", a.Key);
                if (a.Value.Count > 0)
                {
                    LogFoundPackages(a.Value);
                }
                else
                {
                    Console.WriteLine("Could not find assembly : {0}", a.Key);
                }
            }
            OutputElapsedTime(sw);
        }

        private IEnumerable<string> GetAssemblyListFromDirectory()
        {
            var assemblies = new List<string>();
            var fqfn = fileSystem.GetFiles(Arguments[0], "*.dll");
            var filenames = fqfn.Select(f =>
            {
                var sfn = new FileInfo(f);
                return sfn.Name;
            });
            return filenames;

        }

        private IQueryable<IPackage> GetPackageList(IPackageRepository repository)
        {
            IQueryable<IPackage> packages;
            if (AllVersions)
            {
                packages = repository.GetPackages();
            }
            else
            {
                packages = repository.GetPackages().Where(p => p.IsLatestVersion);
            }
            return packages;
        }

        private void OutputElapsedTime(Stopwatch sw)
        {
            Console.WriteLine("Completed search in {0} seconds", sw.Elapsed.TotalSeconds);
        }

        private void LogFoundPackages(List<IPackage> found)
        {
            foreach (var p in found)
            {
                Console.WriteLine("Found in : ".PadLeft(15) + "{0} {1}", p.Id, p.Version);
            }
        }

        private IPackageRepository GetRepository()
        {
            var repository = AggregateRepositoryHelper.CreateAggregateRepositoryFromSources(RepositoryFactory, SourceProvider, Source);
            repository.Logger = Console;
            return repository;
        }

        protected virtual IFileSystem CreateFileSystem(string root)
        {
            return new PhysicalFileSystem(root);
        }
    }
}
