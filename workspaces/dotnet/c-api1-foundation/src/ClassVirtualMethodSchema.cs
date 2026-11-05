using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public sealed class ClassVirtualMethodSchema : IClassVirtualMethodSchema
{
    public required uint NativeVtableIndex { get; set; }

    public required string Name { get; set; }

    public required bool IsStatic { get; set; }

    public required ITypeSchema? ReturnType { get; set; }

    public required List<ClassMethodArgSchema> Args { get; set; }

    public required bool IsNativeReturnValueByParamOptimizationEnabled { get; set; }
}