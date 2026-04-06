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

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on contributing to this project.
