using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public interface IClassSchema
{
    public string? Namespace { get; set; }

    public string Name { get; set; }

    public ClassRefSchema? ParentClassRef { get; set; }

    public uint NativeDataSize { get; set; }

    public List<ClassFieldSchema>? Fields { get; set; }

    public List<IClassMethodSchema> Methods { get; set; }
}