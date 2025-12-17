# ClrMD Migration Guide: 0.9.2 to 1.1.2

## Overview
This document provides a comprehensive guide for the migration of Microsoft.Diagnostics.Runtime (ClrMD) from version 0.9.2 to 1.1.2 in the DebugDiag.DotNet project.

## Project References Updated

### Before (0.9.2)
```xml
<Reference Include="Microsoft.Diagnostics.Runtime">
  <HintPath>Microsoft.Diagnostics.Runtime.dll</HintPath>
</Reference>
```

### After (1.1.2)
```xml
<Reference Include="Microsoft.Diagnostics.Runtime">
  <HintPath>..\Microsoft.Diagnostics.Runtime_1.1.2\bin\Debug\net45\Microsoft.Diagnostics.Runtime.dll</HintPath>
</Reference>
<Reference Include="System.Xaml" />
```

## API Changes Made

### 1. GetRuntime() → Runtime Property

**Before:**
```csharp
heapType.Heap.GetRuntime().ReadPointer(value, out value);
```

**After:**
```csharp
heapType.Heap.Runtime.ReadPointer(value, out value);
```

**Files Updated:**
- `DebugDiag.DotNet/ClrHelper.cs` (1 instance)
- `ClrMemDiagExt/ClrObject.cs` (2 instances)
- `ClrMemDiagExt/StaticVariableValueWrapper.cs` (1 instance)
- `DebugDiag.AnalysisRules/HeapCache.cs` (1 instance)
- `DebugDiag.AnalysisRules/DotNetMemoryAnalysis.cs` (1 instance)
- `DebugDiag.AnalysisRules/SharePointAnalysis.cs` (1 instance)

### 2. ArrayComponentType → ComponentType

**Before:**
```csharp
ClrType arrayComponentType = type.ArrayComponentType;
ClrType keyType = ((ClrType)val).ArrayComponentType.GetFieldByName("key").Type;
```

**After:**
```csharp
ClrType arrayComponentType = type.ComponentType;
ClrType keyType = ((ClrType)val).ComponentType.GetFieldByName("key").Type;
```

**Files Updated:**
- `ClrMemDiagExt/ClrObject.cs` (4 instances)
- `ClrMemDiagExt/MDType.cs` (2 instances)
- `DebugDiag.AnalysisRules/DotNetMemoryAnalysis.cs` (4 instances)

### 3. EnumerateObjects() → EnumerateObjectAddresses()

**Before:**
```csharp
foreach (ulong item in ClrHeap.EnumerateObjects())
{
    ClrType objectType = ClrHeap.GetObjectType(item);
    // ...
}
```

**After:**
```csharp
foreach (ulong item in ClrHeap.EnumerateObjectAddresses())
{
    ClrType objectType = ClrHeap.GetObjectType(item);
    // ...
}
```

**Files Updated:**
- `DebugDiag.DotNet/NetDbgObj.cs` (1 instance)
- `DebugDiag.AnalysisRules/HeapCache.cs` (1 instance)

### 4. EnumerateFinalizerQueue() → EnumerateFinalizerQueueObjectAddresses()

**Before:**
```csharp
foreach (ulong objAddr in runtime.EnumerateFinalizerQueue())
{
    // Process finalizer queue objects
}
```

**After:**
```csharp
foreach (ulong objAddr in runtime.EnumerateFinalizerQueueObjectAddresses())
{
    // Process finalizer queue objects
}
```

**Compatibility Extension:**
A compatibility extension method has been added to provide backward compatibility:
```csharp
// ClrMDCompatibility.cs provides:
public static IEnumerable<ulong> EnumerateFinalizerQueue(this ClrRuntime runtime)
{
    return runtime.EnumerateFinalizerQueueObjectAddresses();
}
```

### 5. GetThreadPool() → ThreadPool Property

**Before:**
```csharp
ClrThreadPool threadPool = runtime.GetThreadPool();
if (threadPool != null)
{
    // Process thread pool information
}
```

**After:**
```csharp
ClrThreadPool threadPool = runtime.ThreadPool;
if (threadPool != null)
{
    // Process thread pool information
}
```

**Compatibility Extension:**
A compatibility extension method has been added to provide backward compatibility:
```csharp
// ClrMDCompatibility.cs provides:
public static ClrThreadPool GetThreadPool(this ClrRuntime runtime)
{
    // The base ClrRuntime.ThreadPool throws NotImplementedException
    // We need to use reflection to access the ThreadPool property from concrete implementations
    try
    {
        // Try to get the ThreadPool property using reflection
        var threadPoolProperty = runtime.GetType().GetProperty("ThreadPool", BindingFlags.Public | BindingFlags.Instance);
        if (threadPoolProperty != null && threadPoolProperty.CanRead)
        {
            var threadPool = threadPoolProperty.GetValue(runtime) as ClrThreadPool;
            if (threadPool != null)
            {
                return threadPool;
            }
        }
    }
    catch
    {
        // If reflection fails, fall back to the base implementation
    }
    
    // Fallback to the base implementation (will throw NotImplementedException)
    return runtime.ThreadPool;
}
```

**Note:** The base `ClrRuntime.ThreadPool` property throws `NotImplementedException`, so the compatibility method uses reflection to access the actual implementation from concrete runtime types (since `DesktopRuntimeBase` is internal).

### 6. GetHeap() → Heap Property

**Before:**
```csharp
ClrHeap heap = runtime.GetHeap();
```

**After:**
```csharp
ClrHeap heap = runtime.Heap;
```

**Files Updated:**
- `DebugDiag.AnalysisRules/SharePointAnalysis.cs` (1 instance)
- `DebugDiag.AnalysisRules/HeapCache.cs` (1 instance)
- `DebugDiag.AnalysisRules/DotNetMemoryAnalysis.cs` (1 instance)

### 7. Additional API Changes in ClrMD 1.1.2

**Method to Property Changes:**
```csharp
// Before
field.IsObjectReference()  →  field.IsObjectReference
field.IsPrimitive()        →  field.IsPrimitive  
field.IsValueClass()       →  field.IsValueType
runtime.GetHeap()          →  runtime.Heap
```

**Files Updated:**
- `DebugDiag.AnalysisRules/AnalyzeManagedImpl.cs` (1 instance of IsValueType)
- `DebugDiag.AnalysisRules/DotNetMemoryAnalysis.cs` (6 instances: IsValueType, IsObjectReference)
- `DebugDiag.AnalysisRules/SharePointAnalysis.cs` (1 instance of IsObjectReference)
- `DebugDiag.AnalysisRules/HeapCache.cs` (1 instance of IsObjectReference)

**API Method Changes:**
```csharp
// Before
clrInfo.TryGetDacLocation()     →  clrInfo.DacInfo.LocalDacPath
target.CreateRuntime(dacPath)   →  clrInfo.CreateRuntime(dacPath)
runtime.EnumerateFinalizerQueue() → runtime.EnumerateFinalizerQueueObjectAddresses()
runtime.GetThreadPool()        →  runtime.ThreadPool
```

**Removed APIs:**
```csharp
// ClrDiagnosticsException.HR enum was removed
// Before: Enum.GetName(typeof(ClrDiagnosticsException.HR), ex.HResult)
// After:  String.Format("0x{0:X8}", ex.HResult)
```

## ClrMemDiagExt Integration

The ClrMemDiagExt functionality has been preserved and continues to work with ClrMD 1.1.2:

- **Dynamic Object Access**: ClrMemDiagExt.ClrObject continues to provide dynamic object access
- **Array/Collection Indexing**: Dictionary and List indexing functionality maintained
- **Type Caching**: Existing caching mechanisms preserved
- **COM Interface Extensions**: All COM-related functionality unchanged

## Compilation Fixes Applied

### 1. ElfCoreFile.cs Type Inference Issue
**Problem:** Switch expression type inference failed in LINQ query
**Solution:** Added explicit cast to `IElfPRStatus`

### 2. DacDataTargetWrapper.cs Unsafe Context
**Problem:** Unsafe delegate used without unsafe context
**Solution:** Added `unsafe` keyword to class declaration

### 3. ClrObject Namespace Conflict
**Problem:** ClrMD 1.1.2 introduced a new `ClrObject` struct that conflicts with ClrMemDiagExt's `ClrObject` class
**Solution:** Used namespace aliases to resolve the conflict

**Before:**
```csharp
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.RuntimeExt;
```

**After:**
```csharp
using Microsoft.Diagnostics.Runtime;
using ClrObject = Microsoft.Diagnostics.RuntimeExt.ClrObject;
using Microsoft.Diagnostics.RuntimeExt; // For ClrPrimitiveValue, ClrNullValue, etc.
```

**Files Updated:**
- `DebugDiag.DotNet/ClrHelper.cs`
- `DebugDiag.DotNet/NetDbgThread.cs`
- `DebugDiag.DotNet/NetDbgObj.cs`

## Testing and Validation

### Compilation Status
- ✅ All projects compile successfully
- ✅ No deprecated API warnings
- ✅ All references correctly updated

### Functionality Verification
- ✅ ClrMemDiagExt dynamic objects work correctly
- ✅ Object enumeration patterns function properly
- ✅ Heap analysis capabilities maintained
- ✅ Thread stack analysis unchanged

## Troubleshooting

### Common Issues

1. **Missing Assembly Reference**
   - Ensure ClrMD 1.1.2 DLL is in the correct path
   - Verify project references point to the new location

2. **Compilation Errors**
   - Check for any remaining `GetRuntime()` calls
   - Verify `ArrayComponentType` has been replaced with `ComponentType`
   - Ensure `EnumerateObjects()` is replaced with `EnumerateObjectAddresses()` where returning addresses
   - Resolve `ClrObject` namespace conflicts using aliases: `using ClrObject = Microsoft.Diagnostics.RuntimeExt.ClrObject;`

3. **Runtime Issues**
   - Verify ClrMD 1.1.2 DLL is deployed with the application
   - Check that all dependent assemblies are compatible

### Performance Considerations

ClrMD 1.1.2 includes several performance improvements:
- Built-in caching mechanisms (`CacheHeap()`, `CacheRoots()`)
- Enhanced type system performance
- Improved object reference enumeration

Consider leveraging these features in future optimizations.

## Future Enhancements

### Potential Optimizations
1. **Implement Built-in Caching**: Replace custom caching with ClrMD 1.1.2's `CacheHeap()` and `CacheRoots()`
2. **Use New ClrObject Struct**: Consider using `heap.GetObject(ulong)` for object creation
3. **Enhanced Type System**: Leverage improved method table handling

### ClrMemDiagExt Consolidation
Future work could include:
- Evaluating which ClrMemDiagExt features can be replaced with native ClrMD 1.1.2 functionality
- Creating compatibility layers for seamless transition
- Optimizing performance by using native implementations where possible

## Conclusion

The migration from ClrMD 0.9.2 to 1.1.2 has been successfully completed with minimal code changes required. All deprecated APIs have been updated, and the existing functionality has been preserved through the ClrMemDiagExt compatibility layer.

The upgrade provides access to improved performance, bug fixes, and new features available in ClrMD 1.1.2 while maintaining backward compatibility for existing analysis workflows.