using System.Collections.Generic;
using System.Reflection;

namespace NuGet.Extensions.FeedAudit
{
    public class FeedAuditResult
    {
        public IPackage Package;
        public List<AssemblyName> ResolvedAssemblyReferences = new List<AssemblyName>();
        public List<AssemblyName> UnresolvedAssemblyReferences = new List<AssemblyName>();
        public List<string> UnloadablePackageFiles = new List<string>();
        public List<IPackage> UsedPackageDependencies = new List<IPackage>();
        public List<IPackage> UnusedPackageDependencies = new List<IPackage>(); 
        public List<PackageDependency> UnresolvedDependencies = new List<PackageDependency>();
        public List<AssemblyName> FeedResolvableReferences = new List<AssemblyName>();
        public List<AssemblyName> FeedUnresolvableReferences = new List<AssemblyName>();
        public List<AssemblyName> GacResolvableReferences = new List<AssemblyName>();
    }
}