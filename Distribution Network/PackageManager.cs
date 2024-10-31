using System.IO.Compression;
using Microsoft.CodeAnalysis;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Doorfail.Distribution.Network
{
    public class PackageManager
    {
        private static string _nugetCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        public static async Task<MetadataReference> LoadNuGetAssembly(PackageInfo package)
        {
            // Path to the package directory in the cache
            var packageDirectory = Path.Combine(_nugetCache, package.Name.ToLower());
            string? dllPath = null;

            // If version is "*", get the latest version available
            if (package.Version == "*")
            {
                var availableVersions = Directory.EnumerateDirectories(packageDirectory)
                                                  .Select(Path.GetFileName)
                                                  .OrderByDescending(v => v)
                                                  .ToList();
                package.Version = availableVersions.FirstOrDefault(); // Pick the latest version
            }

            // Check if a specific version folder exists
            var packagePath = Path.Combine(packageDirectory, package.Version);
            if (Directory.Exists(packagePath))
            {
                // Look for the main DLL file in this version's directory
                dllPath = Directory.GetFiles(packagePath, "*.dll", SearchOption.AllDirectories).FirstOrDefault();
                if (dllPath != null)
                {
                    Console.WriteLine($"Loading cached assembly from: {dllPath}");
                    return MetadataReference.CreateFromFile(dllPath);
                }
            }
        
            // If not found in cache, download the package
            if (dllPath == null)
            {
                dllPath = await DownloadAndExtractNuGetPackage(package.Name, package.Version);
            }

            // Load the DLL if it exists
            if (dllPath != null)
            {
                var assembly = MetadataReference.CreateFromFile(dllPath);
                Console.WriteLine($"Loaded assembly: {assembly.Display}");

                return assembly;
            }
            else
            {
                Console.WriteLine("Package DLL not found.");
            }

            return null;
        }

        public static async Task<string> DownloadAndExtractNuGetPackage(string packageName, string version = "**")
        {
            string packageVersion = version;
            string nupkgPath = Path.Combine(Path.GetTempPath(), $"{packageName}.{packageVersion}.nupkg");
            string extractPath = Path.Combine(Path.GetTempPath(), $"{packageName}.{packageVersion}");

            // Set up the NuGet source repository
            var sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

            // If version is "**", get the latest version from NuGet
            if (version.StartsWith( "*"))
            {
                var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
                var latestPackageMetadata = await packageMetadataResource.GetMetadataAsync(packageName, includePrerelease: false, includeUnlisted: false, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);

                // Find the latest version
                var latestVersionMetadata = latestPackageMetadata?.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
                if (latestVersionMetadata == null)
                {
                    Console.WriteLine($"Failed to find the latest version of {packageName}");
                    return null;
                }

                // Set the version to the latest version found
                packageVersion = latestVersionMetadata.Identity.Version.ToString();
            }

            // Proceed with downloading the package
            var packageIdentity = new PackageIdentity(packageName, new NuGetVersion(packageVersion));
            var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>();
            var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                packageIdentity,
                new PackageDownloadContext(new SourceCacheContext()),
                nupkgPath,
                NullLogger.Instance,
                CancellationToken.None);

            // Check the download result and extract the package
            if (downloadResult.Status == DownloadResourceResultStatus.Available)
            {
                ZipFile.ExtractToDirectory(((FileStream)downloadResult.PackageStream).Name, extractPath, overwriteFiles: true);

                // Find the main DLL in the extracted package
                var dllPath = Directory.GetFiles(extractPath, "*.dll", SearchOption.AllDirectories).FirstOrDefault();
                Console.WriteLine($"Extracted DLL path: {dllPath}");
                return dllPath;
            }
            else
            {
                Console.WriteLine($"Failed to download {packageName} {version}. Status: {downloadResult.Status}");
                return null;
            }
        }

    }
}
