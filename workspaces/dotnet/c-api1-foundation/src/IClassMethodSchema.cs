using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public interface IClassMethodSchema
{
    public string Name { get; set; }

    public bool IsStatic { get; set; }

    public ITypeSchema? ReturnType { get; set; }

    public List<ClassMethodArgSchema> Args { get; set; }

    public bool IsNativeReturnValueByParamOptimizationEnabled { get; set; }
}