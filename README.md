# Bielu.StaticCode.Analyzers

[![NuGet](https://img.shields.io/nuget/v/Bielu.StaticCode.Analyzers.svg)](https://www.nuget.org/packages/Bielu.StaticCode.Analyzers)

Roslyn-based static code analyzers that enforce coding conventions used across the bielu ecosystem.

## Installation

```xml
<PackageReference Include="Bielu.StaticCode.Analyzers">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

## Analyzers

| Rule ID | Category | Severity | Title |
|---------|----------|----------|-------|
| BIELU001 | Naming | Warning | [Decorator naming convention](#bielu001--decorator-naming-convention) |
| BIELU002 | Style | Warning | [Use primary constructor](#bielu002--use-primary-constructor) |
| BIELU003 | Usage | Warning | [Use IOptionsMonitor\<T\>](#bielu003--use-ioptionsmonitort) |
| BIELU004 | Usage | Warning | [Use ILogger\<T\>](#bielu004--use-iloggert) |
| BIELU005 | Naming | Warning | [Async method naming](#bielu005--async-method-naming) |
| BIELU006 | Naming | Warning | [IServiceCollection extension class naming](#bielu006--iservicecollection-extension-class-naming) |
| BIELU007 | Style | Warning | [Use ArgumentNullException.ThrowIfNull()](#bielu007--use-argumentnullexceptionthrowifnull) |
| BIELU008 | Usage | Warning | [Use ConfigureAwait(false)](#bielu008--use-configureawaitfalse) |
| BIELU009 | Design | Info | [Seal internal classes](#bielu009--seal-internal-classes) |

---

### BIELU001 – Decorator Naming Convention

Decorator classes must follow the naming pattern `{Modifier}{InterfaceName}Decorator`.

| ❌ Incorrect | ✅ Correct |
|---|---|
| `ApiServiceCache : IApiService` | `CachedApiServiceDecorator : IApiService` |
| `LoggingUserRepo : IUserRepository` | `LoggedUserRepositoryDecorator : IUserRepository` |

A class is considered a **decorator** when it:
1. Implements an interface that starts with `I` (e.g. `IApiService`)
2. Has a constructor parameter of the same interface type

The class name must end with `{InterfaceNameWithoutI}Decorator` (e.g. `ApiServiceDecorator`), optionally prefixed by a modifier (e.g. `Cached`, `Logged`, `Resilient`).

### BIELU002 – Use Primary Constructor

Classes with a single constructor whose body only contains simple field or property assignments should use the [primary constructor](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#primary-constructors) syntax introduced in C# 12.

```csharp
// ❌ Traditional constructor - can be simplified
public class MyService
{
    private readonly ILogger _logger;
    public MyService(ILogger logger) { _logger = logger; }
}

// ✅ Primary constructor
public class MyService(ILogger logger)
{
    private readonly ILogger _logger = logger;
}
```

Conditions under which the rule **does not** apply:
- The class already uses a primary constructor
- The class is `abstract` or `static`
- The class has more than one constructor
- The constructor chains to another constructor (`base(...)` / `this(...)`)
- The constructor body contains logic beyond simple assignments

### BIELU003 – Use IOptionsMonitor\<T\>

Options should be injected using `IOptionsMonitor<T>` rather than `IOptions<T>` or `IOptionsSnapshot<T>`.

`IOptionsMonitor<T>` provides the current configuration value and supports **change notifications**, making it the preferred choice for hot-reload scenarios.

| ❌ Incorrect | ✅ Correct |
|---|---|
| `IOptions<MyOptions>` | `IOptionsMonitor<MyOptions>` |
| `IOptionsSnapshot<MyOptions>` | `IOptionsMonitor<MyOptions>` |

A code fix is provided to automatically replace `IOptions<T>` / `IOptionsSnapshot<T>` with `IOptionsMonitor<T>`.

### BIELU004 – Use ILogger\<T\>

Constructor parameters should use the typed `ILogger<T>` (where T is the containing class) instead of the untyped `ILogger` interface. Typed loggers enable proper log categorization and filtering.

```csharp
// ❌ Untyped logger
public class MyService(ILogger logger) { }

// ✅ Typed logger
public class MyService(ILogger<MyService> logger) { }
```

### BIELU005 – Async Method Naming

Methods returning `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>` should have their name suffixed with `Async`.

```csharp
// ❌ Missing Async suffix
public Task GetData() => Task.CompletedTask;

// ✅ Correct suffix
public Task GetDataAsync() => Task.CompletedTask;
```

Exceptions:
- Overridden methods (naming is dictated by the base class)
- Interface implementations (naming is dictated by the interface)
- Test methods (marked with `[Fact]`, `[Theory]`, `[Test]`, or `[TestMethod]`)

### BIELU006 – IServiceCollection Extension Class Naming

Static classes containing extension methods on `IServiceCollection` should be named `{Feature}ServiceCollectionExtensions`.

```csharp
// ❌ Wrong name
public static class MyFeatureExtensions
{
    public static IServiceCollection AddMyFeature(this IServiceCollection services) => services;
}

// ✅ Correct name
public static class MyFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddMyFeature(this IServiceCollection services) => services;
}
```

### BIELU007 – Use ArgumentNullException.ThrowIfNull()

Null guard clauses should use the modern `ArgumentNullException.ThrowIfNull()` pattern instead of manual `if (param == null) throw new ArgumentNullException(...)`.

```csharp
// ❌ Manual null check
if (name == null)
    throw new ArgumentNullException(nameof(name));

// ✅ Modern guard clause
ArgumentNullException.ThrowIfNull(name);
```

### BIELU008 – Use ConfigureAwait(false)

In library code, awaited tasks should call `.ConfigureAwait(false)` to avoid capturing the synchronization context, preventing potential deadlocks and improving performance.

```csharp
// ❌ Missing ConfigureAwait
await Task.Delay(100);

// ✅ Explicit ConfigureAwait
await Task.Delay(100).ConfigureAwait(false);
```

### BIELU009 – Seal Internal Classes

Internal classes that are not designed for inheritance should be marked as `sealed`. This prevents accidental inheritance, makes intent clear, and enables JIT devirtualization optimizations. This rule is reported as **Info** severity.

```csharp
// ⚠️ Could be sealed
internal class MyHelper { }

// ✅ Sealed
internal sealed class MyHelper { }
```

Exceptions:
- Classes that are already `sealed`, `abstract`, or `static`
- Classes with `virtual` or `abstract` members
- Classes that have derived classes in the same file

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on contributing to this project.
