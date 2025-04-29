using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fuser;

// This is very rudimentary, nothing substantially is implemented yet.
// It's just to kick off the project and get something working.
public class MergeAssembliesTask : Task
{
    [Required]
    public string MainAssemblyPath { get; set; } = "";

    [Required]
    public ITaskItem[] AssembliesToMerge { get; set; } = Array.Empty<ITaskItem>();

    public bool DeleteMergedFiles { get; set; } = true;

    public override bool Execute()
    {
        try
        {
            Log.LogMessage(MessageImportance.High, $"Fuser: Merging assemblies into {MainAssemblyPath}");

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(MainAssemblyPath)!);

            var readerParams = new ReaderParameters { AssemblyResolver = resolver };
            var mainAssembly = AssemblyDefinition.ReadAssembly(MainAssemblyPath, readerParams);

            var assembliesToMergeDefs = new List<AssemblyDefinition>();
            var mergedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var assemblyItem in AssembliesToMerge)
            {
                var path = assemblyItem.ItemSpec;
                var assembly = AssemblyDefinition.ReadAssembly(path, readerParams);
                assembliesToMergeDefs.Add(assembly);
                mergedAssemblyNames.Add(assembly.Name.Name);
            }

            foreach (var assembly in assembliesToMergeDefs)
            {
                foreach (var module in assembly.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        if (type.IsSpecialName || type.Name == "<Module>")
                            continue;

                        // Make types internal
                        if (type.IsPublic)
                            type.IsPublic = false;

                        var importedType = mainAssembly.MainModule.ImportReference(type);
                        var clonedType = new TypeDefinition(
                            type.Namespace,
                            type.Name,
                            type.Attributes,
                            type.BaseType is not null ? mainAssembly.MainModule.ImportReference(type.BaseType) : null
                        );

                        foreach (var field in type.Fields)
                        {
                            var clonedField = new FieldDefinition(
                                field.Name,
                                field.Attributes,
                                mainAssembly.MainModule.ImportReference(field.FieldType));

                            clonedType.Fields.Add(clonedField);
                        }

                        foreach (var method in type.Methods)
                        {
                            var clonedMethod = new MethodDefinition(
                                method.Name,
                                method.Attributes,
                                method.ReturnType is not null ? mainAssembly.MainModule.ImportReference(method.ReturnType) : null);

                            foreach (var parameter in method.Parameters)
                            {
                                clonedMethod.Parameters.Add(new ParameterDefinition(
                                    parameter.Name,
                                    parameter.Attributes,
                                    mainAssembly.MainModule.ImportReference(parameter.ParameterType)));
                            }

                            clonedType.Methods.Add(clonedMethod);
                        }

                        mainAssembly.MainModule.Types.Add(clonedType);
                    }
                }
            }

            // Remove merged assembly references
            var referencesToRemove = mainAssembly.MainModule.AssemblyReferences
                .Where(r => mergedAssemblyNames.Contains(r.Name))
                .ToList();

            foreach (var reference in referencesToRemove)
            {
                mainAssembly.MainModule.AssemblyReferences.Remove(reference);
                Log.LogMessage(MessageImportance.Low, $"Fuser: Removed reference to {reference.Name}");
            }

            mainAssembly.Write(MainAssemblyPath);

            // Delete merged files if requested
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

            Log.LogMessage(MessageImportance.High, $"Fuser: Merging completed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }
}
