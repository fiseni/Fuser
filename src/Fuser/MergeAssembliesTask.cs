using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Fuser;

public class MergeAssembliesTask : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    [Required]
    public bool DeleteMergedFiles { get; set; } = false;

    [Required]
    public ITaskItem[] ReferencesToMerge { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        //Debugger.Launch();

        try
        {
            Log.LogMessage(MessageImportance.High, $"Fuser: Merging assemblies into {MainAssemblyPath}");

            var assembliesToMerge = new[] { MainAssemblyPath }
                .Concat(ReferencesToMerge.Select(x => x.ItemSpec))
                .Distinct()
                .ToArray();

            var repackOptions = new RepackOptions
            {
                OutputFile = MainAssemblyPath,
                InputAssemblies = assembliesToMerge,
                DebugInfo = true,
                Internalize = true,
                Parallel = false,
                SearchDirectories = new[] { Path.GetDirectoryName(MainAssemblyPath)! },
                TargetKind = ILRepack.Kind.SameAsPrimaryAssembly,
            };

            var repack = new ILRepack(repackOptions);
            repack.Repack();

            Log.LogMessage(MessageImportance.High, "Fuser: Merging completed successfully.");

            if (DeleteMergedFiles)
            {
                var filesToDelete = GetFilesToDelete(MainAssemblyPath, assembliesToMerge);
                foreach (var filePath in filesToDelete)
                {
                    try
                    {
                        File.Delete(filePath);
                        Log.LogMessage(MessageImportance.Low, $"Fuser: Deleted merged file {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Fuser: Failed to delete merged file '{filePath}': {ex.Message}");
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private static string[] GetFilesToDelete(string mainAssemblyPath, string[] assemblies)
    {
        var mainAssemblyDir = Path.GetDirectoryName(mainAssemblyPath)!;
        var mainAssemblyBaseName = Path.GetFileNameWithoutExtension(mainAssemblyPath);

        return assemblies
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.Equals(name, mainAssemblyBaseName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(baseName => Directory.GetFiles(mainAssemblyDir, baseName + ".*"))
            .ToArray();
    }
}
