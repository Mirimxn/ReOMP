using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public interface IRegisteredClassSchema : IClassSchema
{
    public string ApiClassName { get; set; }

    public List<ClassRegisteredPropertySchema> Properties { get; set; }
}