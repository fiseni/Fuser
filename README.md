# Fuser

**Fuser** is an MSBuild task that merges selected referenced assemblies directly into your project's output during build.

## Why?

The main motivation is to avoid dependency conflicts and version mismatches in shared hosting and plugin environments. 

The project is still in its infancy, I haven't clearly defined the objectives yet. Tell me about your specific scenarios and the pain points you're facing.

### Initial idea

Mark any package you want to be merged into your output as follows.

```xml
<ItemGroup>
  <PackageReference Include="Some.Library" Version="1.2.3">
    <Merge>true</Merge>
  </PackageReference>

  <PackageReference Include="Fuser" Version="0.0.1-alpha1" PrivateAssets="All" />
</ItemGroup>
```

✅ No need to manually merge or pack.  
✅ Works during normal `dotnet build` and `dotnet test`.  
✅ Simple configuration with a `<Merge>true</Merge>` property on any `PackageReference`.
