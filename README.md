# Fuser

**Fuser** is an MSBuild task that merges selected referenced assemblies directly into your project's output during build.

## Why?

The main motivation is to avoid dependency conflicts and version mismatches in shared hosting and plugin environments. 

I still have no clear plan; the project is still in its inception. A colleague in the community brought up the idea, and I'm open to any suggestions to shape the library's goals. Tell me about your specific scenarios and the pain points you're facing.

### Initial idea

Mark any package you want to be merged into your output as follows.

```xml
<ItemGroup>
  <PackageReference Include="Some.Library" Version="1.2.3">
    <Merge>true</Merge>
  </PackageReference>

  <PackageReference Include="Fuser" Version="1.0.0" PrivateAssets="All" />
</ItemGroup>
```

✅ No need to manually merge or pack.  
✅ Works during normal `dotnet build` and `dotnet test`.  
✅ Simple configuration with a `<Merge>true</Merge>` property on any `PackageReference`.