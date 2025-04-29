using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ILRepacking;
using System;
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
    public ITaskItem[] AssembliesToMerge { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.High, $"Fuser: Merging assemblies into {MainAssemblyPath}");

            var allAssemblies = new[] { MainAssemblyPath }
                .Concat(AssembliesToMerge.Select(a => a.ItemSpec))
                .Distinct()
                .ToArray();

            var repackOptions = new RepackOptions
            {
                OutputFile = MainAssemblyPath,
                InputAssemblies = allAssemblies,
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
                foreach (var assemblyItem in AssembliesToMerge)
                {
                    try
                    {
                        var path = assemblyItem.ItemSpec;
                        File.Delete(path);
                        Log.LogMessage(MessageImportance.Low, $"Fuser: Deleted merged file {path}");
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Fuser: Failed to delete merged file '{assemblyItem.ItemSpec}': {ex.Message}");
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
}
