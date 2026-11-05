using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public sealed class ClassRegisteredMethodSchema : IClassRegisteredMethodSchema
{
    public required string ApiFunctionName { get; set; }

    public uint? NativeDataRawPtrOffset { get; set; }

    public required uint? NativeSteamRuntimeOffset { get; set; }

    public required uint? NativeEGSRuntimeOffset { get; set; }

    public required string Name { get; set; }

    public required bool IsStatic { get; set; }

    public required ITypeSchema? ReturnType { get; set; }

    public required List<ClassMethodArgSchema> Args { get; set; }

    public required bool IsNativeReturnValueByParamOptimizationEnabled { get; set; }
}