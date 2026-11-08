# typed

## Generic Editor Contracts

TypeD provides component selection, clipboard, camera and history services,
shared command data, icon/drag helpers, and component-tree move validation.
Primitive bool/string/int/double/enum property definitions are registered by
TypeD initialization. Numeric text controls live in `TypeD.View.PropertyTemplates`
and can be reused by both generic and module-specific templates.

Engine-specific creation dialogs are supplied through
`CreateComponentDialogHook` and `AddCustomPropertyDialogHook`. The editor knows
only their TypeD data contracts; loaded modules own the windows and engine
semantics.

## File format versions and module conversions

Project (`.typeo`) and component (`.component`) JSON documents contain independent
integer format versions, not editor or module package versions:

```json
{
  "FormatVersions": {
    "TypeD": 1,
    "ExampleModule": 2
  }
}
```

Missing metadata, or a missing module entry, means version **0**. TypeD's first
version is 1; its 0-to-1 conversion only adds version metadata. Each module opts
in separately for project files, component files, or both. Component version
metadata belongs to the entire definition, including nested children and
property overrides, not to individual child instances.

### Registering conversions

Override `TypeDModuleInitializer.FormatVersions` in the editor-side module:

```csharp
using System.Text.Json.Nodes;
using TypeD;
using TypeD.Models.Data;

public class ExampleInitializer : TypeDModuleInitializer
{
    public override IEnumerable<FormatVersionDefinition> FormatVersions =>
        new[]
        {
            new FormatVersionDefinition(
                FormatDocumentKind.Component,
                currentVersion: 2,
                new Dictionary<int, Action<JsonObject>>
                {
                    [0] = json =>
                    {
                        // Adopt the module's first format without changing old data.
                    },
                    [1] = json =>
                    {
                        if (json.Remove("OldModuleOptions", out var options))
                            json["ModuleOptions"] = options;
                    }
                })
        };

    public override void Initializer(Project project) { }
    public override void Uninitializer() { }
}
```

The module name is the stable metadata id; do not include its package version.
Definitions are registered before `Initializer` and disposed automatically on
module unload, including hot reload. A module may define one registration per
document kind. For non-module integrations and tests,
`IFormatVersionModel.Register(moduleId, definition)` returns an `IDisposable`
registration.

Each dictionary entry converts version **n** to **n + 1**. Supply every step
needed for the old versions you support, including version 0 for existing
unversioned projects. To release a new format, increment `CurrentVersion` and
add the old current version's conversion. Missing steps are errors, never
implicit no-ops.

Conversions receive a copy of the complete raw JSON **before DTO
deserialization**, so they can rename fields, update nested children and
property values, and add module-owned data. Preserve unrelated data and do not
edit `FormatVersions`; the editor owns the bookkeeping. Converters should be
deterministic and only change the supplied JSON, with no external side effects.
They run TypeD-first, then by module id in ordinal order, with steps in ascending
version order. Do not depend on other modules' conversion order.

Root module-owned JSON fields are preserved through ordinary saves using DTO
extension data. Unknown fields inside child/property DTOs are not extension
points; use the existing child/property structures or a module-owned root field.

### Opening and saving

1. The project header is read using only TypeD conversions, without changing
   files, so the editor can locate and load module dependencies.
2. After modules register their definitions, project and component conversions
   are all preflighted before any upgraded document is saved.
3. Each changed file is atomically replaced, with the original bytes retained
   beside it as `<original-name>.<unique-id>.bak`. Backup locations are logged.
   Current files are not rewritten or backed up again.
4. Component code and the program entry point are regenerated after any format
   upgrade, even if ordinary regeneration was disabled.

Module project converters cannot change `ProjectName`, `CSSolutionPath`, or
`CSProjName`, since those fields locate the dependencies that supply the
converters. They may change other project fields such as `StartScene` and
module-owned extension fields.

Malformed versions, missing converters, missing module format handlers, and
unsupported future versions stop loading without rewriting the documents.
Conversion or deserialization failures in a batch leave that batch untouched.
I/O failures during replacement are reported; files already replaced retain
their backups. Backups are never removed automatically.

After changing format definitions during development, reopen the project to run
its conversions. Saving an already-loaded document with older stored versions
is rejected rather than stamping a new version without conversion.

### Generated code

Every auto-generated Codalyzer file includes a comment such as:

```csharp
// TypeO format versions: {"TypeD":1,"ExampleModule":2}
```

Component generators use component format versions; other generators use
project format versions. User-maintained C# files are neither stamped nor
converted. Generated code is derived output: converters edit JSON, and the
editor regenerates C# instead of migrating generated text.

Run format regression tests with:

```text
dotnet test TypeDTest\TypeDTest.csproj --filter FullyQualifiedName~FormatVersionTests
```