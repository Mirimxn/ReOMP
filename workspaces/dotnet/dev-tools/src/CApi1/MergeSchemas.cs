using System.Linq;

namespace OMP.LSWTSS.CApi1;

public static class MergeSchemas
{
    public static void Execute(Schema finalSchema, Schema schema, bool additive)
    {
        foreach (var classSchema in schema.Classes)
        {
            var classFinalSchema = finalSchema.Classes.FirstOrDefault(x => x.Namespace == classSchema.Namespace && x.Name == classSchema.Name);

            if (classFinalSchema == null)
            {
                if (additive)
                {
                    finalSchema.Classes.Add(classSchema);
                }
                continue;
            }

            if (
                classFinalSchema is IVirtualClassSchema == false
                &&
                classSchema is IVirtualClassSchema
            )
            {
                finalSchema.Classes.Remove(classFinalSchema);
                if (classFinalSchema is RawClassSchema rawClassFinalSchema)
                {
                    classFinalSchema = new VirtualClassSchema
                    {
                        NativeVtableSteamRuntimeOffset = null,
                        NativeVtableEGSRuntimeOffset = null,
                        Namespace = rawClassFinalSchema.Namespace,
                        Name = rawClassFinalSchema.Name,
                        ParentClassRef = rawClassFinalSchema.ParentClassRef,
                        NativeDataSize = rawClassFinalSchema.NativeDataSize,
                        Fields = rawClassFinalSchema.Fields,
                        Methods = rawClassFinalSchema.Methods,
                    };
                }
                else if (classFinalSchema is RegisteredClassSchema registeredClassFinalSchema)
                {
                    classFinalSchema = new RegisteredVirtualClassSchema
                    {
                        ApiClassName = registeredClassFinalSchema.ApiClassName,
                        Properties = registeredClassFinalSchema.Properties,
                        NativeVtableSteamRuntimeOffset = null,
                        NativeVtableEGSRuntimeOffset = null,
                        Namespace = registeredClassFinalSchema.Namespace,
                        Name = registeredClassFinalSchema.Name,
                        ParentClassRef = registeredClassFinalSchema.ParentClassRef,
                        NativeDataSize = registeredClassFinalSchema.NativeDataSize,
                        Fields = registeredClassFinalSchema.Fields,
                        Methods = registeredClassFinalSchema.Methods,
                    };
                }
                finalSchema.Classes.Add(classFinalSchema);
            }

            if (
                classFinalSchema is IVirtualClassSchema virtualClassFinalSchema
                &&
                classSchema is IVirtualClassSchema virtualClassSchema
            )
            {
                if (virtualClassFinalSchema.NativeVtableSteamRuntimeOffset == null && virtualClassSchema.NativeVtableSteamRuntimeOffset != null)
                {
                    virtualClassFinalSchema.NativeVtableSteamRuntimeOffset = virtualClassSchema.NativeVtableSteamRuntimeOffset;
                }

                if (virtualClassFinalSchema.NativeVtableEGSRuntimeOffset == null && virtualClassSchema.NativeVtableEGSRuntimeOffset != null)
                {
                    virtualClassFinalSchema.NativeVtableEGSRuntimeOffset = virtualClassSchema.NativeVtableEGSRuntimeOffset;
                }
            }

            if (classFinalSchema.Fields == null && classSchema.Fields != null)
            {
                classFinalSchema.Fields = classSchema.Fields;
            }

            foreach (var classMethodSchema in classSchema.Methods)
            {
                var classMethodFinalSchema = classFinalSchema.Methods.FirstOrDefault(x => x.Name == classMethodSchema.Name);

                if (classMethodFinalSchema == null)
                {
                    if (additive)
                    {
                        classFinalSchema.Methods.Add(classMethodSchema);
                    }
                    continue;
                }

                if (
                    classMethodFinalSchema is IClassRawMethodSchema classRawMethodFinalSchema
                    &&
                    classMethodSchema is IClassRawMethodSchema classRawMethodSchema
                )
                {
                    if (classRawMethodFinalSchema.NativeSteamRuntimeOffset == null && classRawMethodSchema.NativeSteamRuntimeOffset != null)
                    {
                        classRawMethodFinalSchema.NativeSteamRuntimeOffset = classRawMethodSchema.NativeSteamRuntimeOffset;
                    }

                    if (classRawMethodFinalSchema.NativeEGSRuntimeOffset == null && classRawMethodSchema.NativeEGSRuntimeOffset != null)
                    {
                        classRawMethodFinalSchema.NativeEGSRuntimeOffset = classRawMethodSchema.NativeEGSRuntimeOffset;
                    }
                }

                if (!classMethodFinalSchema.IsNativeReturnValueByParamOptimizationEnabled && classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
                {
                    classMethodFinalSchema.IsNativeReturnValueByParamOptimizationEnabled = true;
                }
            }
        }
    }
}