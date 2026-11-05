using System.Collections.Generic;
using System.Linq;

namespace OMP.LSWTSS.CApi1;

public static class GetClassSrc
{
    public static string Execute(IClassSchema classSchema, IEnumerable<IClassSchema> classesSchema)
    {
        var classSrcBuilder = new SrcBuilder();

        var classNamespaceParts = classSchema.Namespace == null ? [] : classSchema.Namespace.Split('.');

        classSrcBuilder.Append($"namespace OMP.LSWTSS.CApi1;");
        classSrcBuilder.Append();

        foreach (var classNamespacePart in classNamespaceParts)
        {
            classSrcBuilder.Append($"public static partial class {classNamespacePart}");
            classSrcBuilder.Append("{");
            classSrcBuilder.Indent++;
        }

        classSrcBuilder.Append($"public unsafe static partial class {classSchema.Name}");
        classSrcBuilder.Append("{");
        classSrcBuilder.Indent++;

        {
            if (classSchema is IRegisteredVirtualClassSchema registeredVirtualClassSchema)
            {
                classSrcBuilder.Append("public static readonly RegisteredVirtualClassInfo Info = new RegisteredVirtualClassInfo(");
                classSrcBuilder.Indent++;
                classSrcBuilder.Append($"apiClassName: \"{registeredVirtualClassSchema.ApiClassName}\",");
                classSrcBuilder.Append($"nativeVtableSteamRuntimeOffset: 0x{registeredVirtualClassSchema.NativeVtableSteamRuntimeOffset:X},");
                classSrcBuilder.Append($"nativeVtableEGSRuntimeOffset: 0x{registeredVirtualClassSchema.NativeVtableEGSRuntimeOffset:X}");
                classSrcBuilder.Indent--;
                classSrcBuilder.Append(");");
            }
            else if (classSchema is IRegisteredClassSchema registeredClassSchema)
            {
                classSrcBuilder.Append("public static readonly IRegisteredClassInfo Info = new RegisteredClassInfo(");
                classSrcBuilder.Indent++;
                classSrcBuilder.Append($"apiClassName: \"{registeredClassSchema.ApiClassName}\"");
                classSrcBuilder.Indent--;
                classSrcBuilder.Append(");");
            }
            else if (classSchema is IVirtualClassSchema virtualClassSchema)
            {
                classSrcBuilder.Append("public static readonly VirtualClassInfo Info = new VirtualClassInfo(");
                classSrcBuilder.Indent++;
                classSrcBuilder.Append($"nativeVtableSteamRuntimeOffset: 0x{virtualClassSchema.NativeVtableSteamRuntimeOffset:X},");
                classSrcBuilder.Append($"nativeVtableEGSRuntimeOffset: 0x{virtualClassSchema.NativeVtableEGSRuntimeOffset:X}");
                classSrcBuilder.Indent--;
                classSrcBuilder.Append(");");
            }
            else if (classSchema is IRawClassSchema)
            {
                classSrcBuilder.Append("public static readonly IRawClassInfo Info = new RawClassInfo();");
            }
        }
        classSrcBuilder.Append();

        {
            if (classSchema is IRegisteredClassSchema registeredClassSchema)
            {
                foreach (var registeredClassPropertySchema in registeredClassSchema.Properties)
                {
                    classSrcBuilder.Append($"public static partial class {registeredClassPropertySchema.Name}Property");
                    classSrcBuilder.Append("{");
                    classSrcBuilder.Indent++;

                    classSrcBuilder.Append("public static readonly IClassRegisteredPropertyInfo Info = new ClassRegisteredPropertyInfo(");
                    classSrcBuilder.Indent++;
                    classSrcBuilder.Append($"class_: {classSchema.Name}.Info,");
                    classSrcBuilder.Append($"apiClassFieldName: \"{registeredClassPropertySchema.ApiClassFieldName}\"");
                    classSrcBuilder.Indent--;
                    classSrcBuilder.Append(");");

                    classSrcBuilder.Indent--;
                    classSrcBuilder.Append("}");
                }
            }
        }

        foreach (var classMethodSchema in classSchema.Methods)
        {
            classSrcBuilder.Append($"public static partial class {classMethodSchema.Name}Method");
            classSrcBuilder.Append("{");
            classSrcBuilder.Indent++;

            if (classMethodSchema is IClassRegisteredMethodSchema classRegisteredMethodSchema)
            {
                classSrcBuilder.Append("public static readonly IClassRegisteredMethodInfo Info = new ClassRegisteredMethodInfo(");
                classSrcBuilder.Indent++;
                classSrcBuilder.Append($"class_: {classSchema.Name}.Info,");
                classSrcBuilder.Append($"apiFunctionName: \"{classRegisteredMethodSchema.ApiFunctionName}\",");
                classSrcBuilder.Append($"nativeSteamRuntimeOffset: 0x{classRegisteredMethodSchema.NativeSteamRuntimeOffset:X},");
                classSrcBuilder.Append($"nativeEGSRuntimeOffset: 0x{classRegisteredMethodSchema.NativeEGSRuntimeOffset:X},");
                classSrcBuilder.Append($"isStatic: {classRegisteredMethodSchema.IsStatic.ToString().ToLowerInvariant()}");
                classSrcBuilder.Indent--;
                classSrcBuilder.Append(");");
            }
            else if (classMethodSchema is IClassVirtualMethodSchema classVirtualMethodSchema)
            {
                classSrcBuilder.Append("public static readonly ClassVirtualMethodInfo Info = new ClassVirtualMethodInfo(");
                classSrcBuilder.Indent++;
                classSrcBuilder.Append($"class_: {classSchema.Name}.Info,");
                classSrcBuilder.Append($"nativeVtableIndex: {classVirtualMethodSchema.NativeVtableIndex},");
                classSrcBuilder.Append($"isStatic: {classVirtualMethodSchema.IsStatic.ToString().ToLowerInvariant()}");
                classSrcBuilder.Indent--;
                classSrcBuilder.Append(");");
            }
            else if (classMethodSchema is IClassRawMethodSchema classRawMethodSchema)
            {
                classSrcBuilder.Append("public static readonly IClassRawMethodInfo Info = new ClassRawMethodInfo(");
                classSrcBuilder.Indent++;
                classSrcBuilder.Append($"nativeSteamRuntimeOffset: 0x{classRawMethodSchema.NativeSteamRuntimeOffset:X},");
                classSrcBuilder.Append($"nativeEGSRuntimeOffset: 0x{classRawMethodSchema.NativeEGSRuntimeOffset:X},");
                classSrcBuilder.Append($"isStatic: {classRawMethodSchema.IsStatic.ToString().ToLowerInvariant()}");
                classSrcBuilder.Indent--;
                classSrcBuilder.Append(");");
            }

            classSrcBuilder.Append();

            if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
            {
                classSrcBuilder.Append("public delegate nint NativeDelegate(");
            }
            else
            {
                if (classMethodSchema.ReturnType == null)
                {
                    classSrcBuilder.Append("public delegate void NativeDelegate(");
                }
                else
                {
                    classSrcBuilder.Append($"public delegate {GetTypeNativeSrc.Execute(classMethodSchema.ReturnType)} NativeDelegate(");
                }
            }
            classSrcBuilder.Indent++;

            if (!classMethodSchema.IsStatic)
            {
                classSrcBuilder.Append($"nint nativeDataRawPtr{(!classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled && classMethodSchema.Args.Count == 0 ? "" : ",")}");
            }

            if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
            {
                classSrcBuilder.Append($"nint returnValueNativeDataRawPtr{(classMethodSchema.Args.Count == 0 ? "" : ",")}");
            }

            foreach (var classMethodArgSchema in classMethodSchema.Args)
            {
                var isClassMethodArgLast = classMethodArgSchema == classMethodSchema.Args.Last();

                classSrcBuilder.Append($"{GetClassMethodArgNativeDefSrc.Execute(classMethodArgSchema)}{(isClassMethodArgLast ? "" : ",")}");
            }


            classSrcBuilder.Indent--;
            classSrcBuilder.Append(");");

            classSrcBuilder.Append();

            if (classMethodSchema.ReturnType == null)
            {
                classSrcBuilder.Append("public delegate void Delegate(");
            }
            else
            {
                classSrcBuilder.Append($"public delegate {GetTypeSrc.Execute(classMethodSchema.ReturnType)} Delegate(");
            }

            classSrcBuilder.Indent++;

            foreach (var classMethodArgSchema in classMethodSchema.Args)
            {
                var isClassMethodArgLast = classMethodArgSchema == classMethodSchema.Args.Last();

                classSrcBuilder.Append($"{GetClassMethodArgDefSrc.Execute(classMethodArgSchema)}{(isClassMethodArgLast ? "" : ",")}");
            }

            classSrcBuilder.Indent--;
            classSrcBuilder.Append(");");

            classSrcBuilder.Indent--;
            classSrcBuilder.Append("}");

            if (classMethodSchema.IsStatic)
            {
                classSrcBuilder.Append();

                if (classMethodSchema.ReturnType == null)
                {
                    classSrcBuilder.Append($"public static void {classMethodSchema.Name}(");
                }
                else
                {
                    classSrcBuilder.Append($"public static {GetTypeSrc.Execute(classMethodSchema.ReturnType)} {classMethodSchema.Name}(");
                }
                classSrcBuilder.Indent++;

                foreach (var classMethodArgSchema in classMethodSchema.Args)
                {
                    var isClassMethodArgLast = classMethodArgSchema == classMethodSchema.Args.Last();

                    classSrcBuilder.Append($"{GetClassMethodArgDefSrc.Execute(classMethodArgSchema)}{(isClassMethodArgLast ? "" : ",")}");
                }

                classSrcBuilder.Indent--;
                classSrcBuilder.Append(")");

                classSrcBuilder.Append("{");
                classSrcBuilder.Indent++;

                classSrcBuilder.Append($"var methodNativeDelegate = Native.GetMethodDelegate<{classMethodSchema.Name}Method.NativeDelegate>({classMethodSchema.Name}Method.Info.NativePtr);");

                foreach (var classMethodArgSchema in classMethodSchema.Args)
                {
                    if (
                        classMethodArgSchema.Type is PrimitiveTypeSchema classMethodArgPrimitiveTypeSchema
                        &&
                        classMethodArgPrimitiveTypeSchema.PrimitiveFullName == "string"
                    )
                    {
                        classSrcBuilder.Append($"var {classMethodArgSchema.Name}NativeDataRawPtr = global::System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8({classMethodArgSchema.Name});");
                    }
                }

                if (classMethodSchema.ReturnType == null)
                {
                    classSrcBuilder.Append($"methodNativeDelegate(");
                }
                else
                {
                    if (
                        classMethodSchema.ReturnType is PrimitiveTypeSchema classMethodReturnTypePrimitiveTypeSchema
                        &&
                        classMethodReturnTypePrimitiveTypeSchema.PrimitiveFullName == "string"
                    )
                    {
                        classSrcBuilder.Append($"var returnValueNativeDataRawPtr = methodNativeDelegate(");
                    }
                    else if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
                    {
                        classSrcBuilder.Append($"{GetTypeSrc.Execute(classMethodSchema.ReturnType)} returnValueNativeData;");
                        classSrcBuilder.Append($"methodNativeDelegate(");
                    }
                    else
                    {
                        classSrcBuilder.Append($"var returnValue = ({GetTypeSrc.Execute(classMethodSchema.ReturnType)})methodNativeDelegate(");
                    }
                }

                classSrcBuilder.Indent++;

                if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
                {
                    classSrcBuilder.Append($"(nint)(&returnValueNativeData){(classMethodSchema.Args.Count == 0 ? "" : ",")}");
                }

                foreach (var classMethodArgSchema in classMethodSchema.Args)
                {
                    var isClassMethodArgLast = classMethodArgSchema == classMethodSchema.Args.Last();

                    classSrcBuilder.Append($"{GetClassMethodArgCallSrc.Execute(classMethodArgSchema)}{(isClassMethodArgLast ? "" : ",")}");
                }

                classSrcBuilder.Indent--;
                classSrcBuilder.Append(");");

                foreach (var classMethodArgSchema in classMethodSchema.Args)
                {
                    if (
                        classMethodArgSchema.Type is PrimitiveTypeSchema classMethodArgPrimitiveTypeSchema
                        &&
                        classMethodArgPrimitiveTypeSchema.PrimitiveFullName == "string"
                    )
                    {
                        classSrcBuilder.Append($"global::System.Runtime.InteropServices.Marshal.FreeCoTaskMem({classMethodArgSchema.Name}NativeDataRawPtr);");
                    }
                }

                if (classMethodSchema.ReturnType != null)
                {
                    if (
                        classMethodSchema.ReturnType is PrimitiveTypeSchema classMethodReturnTypePrimitiveTypeSchema
                        &&
                        classMethodReturnTypePrimitiveTypeSchema.PrimitiveFullName == "string"
                    )
                    {
                        classSrcBuilder.Append("return global::System.Runtime.InteropServices.Marshal.PtrToStringUTF8(returnValueNativeDataRawPtr);");
                    }
                    else if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
                    {
                        classSrcBuilder.Append("return returnValueNativeData;");
                    }
                    else
                    {
                        classSrcBuilder.Append("return returnValue;");
                    }
                }

                classSrcBuilder.Indent--;
                classSrcBuilder.Append("}");
            }
        }

        classSrcBuilder.Append($"[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]");
        classSrcBuilder.Append($"public partial struct NativeData");
        classSrcBuilder.Append("{");
        classSrcBuilder.Indent++;

        if (classSchema.Fields != null)
        {
            foreach (var classFieldSchema in classSchema.Fields)
            {
                if (classFieldSchema.Comment != null)
                {
                    classSrcBuilder.Append($"// {classFieldSchema.Comment}");
                }
                classSrcBuilder.Append($"public {GetTypeSrc.Execute(classFieldSchema.Type)} {classFieldSchema.Name};");
            }
        }
        else
        {
            var classNativeDataRemainingSize = classSchema.NativeDataSize;

            while (classNativeDataRemainingSize >= 0x8)
            {
                var classFakeFieldOffset = classSchema.NativeDataSize - classNativeDataRemainingSize;

                classSrcBuilder.Append($"public nint UnknownField0x{classFakeFieldOffset:X};");

                classNativeDataRemainingSize -= 0x8;
            }

            while (classNativeDataRemainingSize >= 0x4)
            {
                var classFakeFieldOffset = classSchema.NativeDataSize - classNativeDataRemainingSize;

                classSrcBuilder.Append($"public uint UnknownField0x{classFakeFieldOffset:X};");

                classNativeDataRemainingSize -= 0x4;
            }

            while (classNativeDataRemainingSize >= 0x2)
            {
                var classFakeFieldOffset = classSchema.NativeDataSize - classNativeDataRemainingSize;

                classSrcBuilder.Append($"public ushort UnknownField0x{classFakeFieldOffset:X};");

                classNativeDataRemainingSize -= 0x2;
            }

            if (classNativeDataRemainingSize == 0x1)
            {
                var classFakeFieldOffset = classSchema.NativeDataSize - classNativeDataRemainingSize;

                classSrcBuilder.Append($"public byte UnknownField0x{classFakeFieldOffset:X};");
            }
        }

        classSrcBuilder.Indent--;
        classSrcBuilder.Append("}");

        classSrcBuilder.Append($"[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]");
        classSrcBuilder.Append($"public partial struct NativeHandle");
        classSrcBuilder.Append("{");
        classSrcBuilder.Indent++;

        classSrcBuilder.Append($"public NativeData* NativeDataPtr;");
        classSrcBuilder.Append($"public readonly nint NativeDataRawPtr => (nint)NativeDataPtr;");
        classSrcBuilder.Append($"public static implicit operator nint(NativeHandle @this) => @this.NativeDataRawPtr;");
        classSrcBuilder.Append($"public static implicit operator NativeData*(NativeHandle @this) => @this.NativeDataPtr;");
        classSrcBuilder.Append($"public static implicit operator NativeHandle(NativeData* nativeDataPtr) => new() {{ NativeDataPtr = nativeDataPtr }};");
        classSrcBuilder.Append($"public static explicit operator NativeHandle(nint nativeDataRawPtr) => new() {{ NativeDataPtr = (NativeData*)nativeDataRawPtr }};");

        var classOverloadedMethodsName = classSchema.Methods.Select(x => x.Name).ToList();
        var classOverloadedPropertiesName = new List<string>();

        if (classSchema is IRegisteredClassSchema classRegisteredClassSchema)
        {
            classOverloadedPropertiesName.AddRange(classRegisteredClassSchema.Properties.Select(x => x.Name));
        }

        if (classSchema.ParentClassRef != null)
        {
            string? classAncestorClassFullName = classSchema.ParentClassRef.ClassFullName;

            while (classAncestorClassFullName != null)
            {
                var classAncestorClassSchema = classesSchema.First(x => x.Namespace == null ? x.Name == classAncestorClassFullName : $"{x.Namespace}.{x.Name}" == classAncestorClassFullName);

                var classAncestorClassCastSrc = $"(({classAncestorClassFullName}.NativeHandle)(this))";

                classSrcBuilder.Append($"public static explicit operator NativeHandle({classAncestorClassFullName}.NativeHandle @this) => new() {{ NativeDataPtr = (NativeData*)@this.NativeDataPtr }};");
                classSrcBuilder.Append($"public static implicit operator {classAncestorClassFullName}.NativeHandle(NativeHandle @this) => new() {{ NativeDataPtr = ({classAncestorClassFullName}.NativeData*)@this.NativeDataPtr }};");
                classSrcBuilder.Append($"public readonly {classAncestorClassFullName}.NativeHandle As{classAncestorClassFullName.Replace(".", "")}() => this;");

                foreach (var classAncestorClassMethodSchema in classAncestorClassSchema.Methods)
                {
                    if (classAncestorClassMethodSchema.IsStatic || classOverloadedMethodsName.Contains(classAncestorClassMethodSchema.Name))
                    {
                        continue;
                    }

                    classOverloadedMethodsName.Add(classAncestorClassMethodSchema.Name);

                    classSrcBuilder.Append($"public readonly {classAncestorClassFullName}.{classAncestorClassMethodSchema.Name}Method.Delegate {classAncestorClassMethodSchema.Name} => {classAncestorClassCastSrc}.{classAncestorClassMethodSchema.Name};");
                }

                if (classAncestorClassSchema is IRegisteredClassSchema classAncestorRegisteredClassSchema)
                {
                    foreach (var classAncestorClassPropertySchema in classAncestorRegisteredClassSchema.Properties)
                    {
                        if (classOverloadedPropertiesName.Contains(classAncestorClassPropertySchema.Name))
                        {
                            continue;
                        }

                        classOverloadedPropertiesName.Add(classAncestorClassPropertySchema.Name);

                        var classAncestorClassPropertyNameSuffix = classAncestorRegisteredClassSchema.Methods.Any(x => x.Name == classAncestorClassPropertySchema.Name) ? "_" : "";

                        if (classAncestorClassPropertySchema.IsArray)
                        {
                            if (classAncestorClassPropertySchema.Type is PrimitiveTypeSchema classAncestorClassPropertyPrimitiveTypeSchema)
                            {
                                if (classAncestorClassPropertyPrimitiveTypeSchema.PrimitiveFullName == "string")
                                {
                                    classSrcBuilder.Append($"public ClassRegisteredPropertyElementsStringValueNativeDataAccessor {classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix} => {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix};");
                                }
                                else
                                {
                                    classSrcBuilder.Append($"public ClassRegisteredPropertyElementsGenericValueNativeDataAccessor<{GetTypeSrc.Execute(classAncestorClassPropertyPrimitiveTypeSchema)}> {classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix} => {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix};");
                                }
                            }
                            else if (classAncestorClassPropertySchema.Type is ClassTypeSchema classAncestorClassPropertyClassTypeSchema)
                            {
                                classSrcBuilder.Append($"public ClassRegisteredPropertyElementsGenericValueNativeDataAccessor<{classAncestorClassPropertyClassTypeSchema.ClassFullName}.NativeData> {classAncestorClassPropertySchema.Name}NativeData{classAncestorClassPropertyNameSuffix} => {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}NativeData{classAncestorClassPropertyNameSuffix};");
                                classSrcBuilder.Append($"public ClassRegisteredPropertyElementsGenericValueNativeHandleAccessor<{classAncestorClassPropertyClassTypeSchema.ClassFullName}.NativeHandle> {classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix} => {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix};");
                            }
                            else
                            {
                                throw new System.InvalidOperationException();
                            }
                        }
                        else
                        {
                            if (classAncestorClassPropertySchema.Type is PrimitiveTypeSchema)
                            {
                                classSrcBuilder.Append($"public {GetTypeSrc.Execute(classAncestorClassPropertySchema.Type)} {classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix}");
                            }
                            else if (classAncestorClassPropertySchema.Type is ClassTypeSchema)
                            {
                                classSrcBuilder.Append($"public {GetTypeSrc.Execute(classAncestorClassPropertySchema.Type)} {classAncestorClassPropertySchema.Name}NativeData{classAncestorClassPropertyNameSuffix}");
                            }
                            else
                            {
                                throw new System.InvalidOperationException();
                            }

                            classSrcBuilder.Append("{");
                            classSrcBuilder.Indent++;

                            classSrcBuilder.Append("get");
                            classSrcBuilder.Append("{");
                            classSrcBuilder.Indent++;

                            if (classAncestorClassPropertySchema.Type is PrimitiveTypeSchema)
                            {
                                classSrcBuilder.Append($"return {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix};");
                            }
                            else if (classAncestorClassPropertySchema.Type is ClassTypeSchema)
                            {
                                classSrcBuilder.Append($"return {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}NativeData{classAncestorClassPropertyNameSuffix};");
                            }
                            else
                            {
                                throw new System.InvalidOperationException();
                            }

                            classSrcBuilder.Indent--;
                            classSrcBuilder.Append("}");

                            classSrcBuilder.Append("set");
                            classSrcBuilder.Append("{");
                            classSrcBuilder.Indent++;

                            classSrcBuilder.Append($"var ancestorNativeHandle = {classAncestorClassCastSrc};");

                            if (classAncestorClassPropertySchema.Type is PrimitiveTypeSchema)
                            {
                                classSrcBuilder.Append($"ancestorNativeHandle.{classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix} = value;");
                            }
                            else if (classAncestorClassPropertySchema.Type is ClassTypeSchema)
                            {
                                classSrcBuilder.Append($"ancestorNativeHandle.{classAncestorClassPropertySchema.Name}NativeData{classAncestorClassPropertyNameSuffix} = value;");
                            }
                            else
                            {
                                throw new System.InvalidOperationException();
                            }

                            classSrcBuilder.Indent--;
                            classSrcBuilder.Append("}");

                            classSrcBuilder.Indent--;
                            classSrcBuilder.Append("}");

                            ITypeSchema classAncestorClassPropertyPtrTypeSchema;

                            {
                                if (classAncestorClassPropertySchema.Type is PrimitiveTypeSchema classAncestorClassPropertyPrimitiveTypeSchema)
                                {
                                    classAncestorClassPropertyPtrTypeSchema = new PrimitiveTypeSchema
                                    {
                                        PrimitiveFullName = classAncestorClassPropertyPrimitiveTypeSchema.PrimitiveFullName,
                                        IsPrimitiveNativeDataPtr = true
                                    };

                                    classSrcBuilder.Append($"public {GetTypeSrc.Execute(classAncestorClassPropertyPtrTypeSchema)} {classAncestorClassPropertySchema.Name}NativeDataPtr{classAncestorClassPropertyNameSuffix}");
                                }
                                else if (classAncestorClassPropertySchema.Type is ClassTypeSchema classAncestorClassPropertyClassTypeSchema)
                                {
                                    classAncestorClassPropertyPtrTypeSchema = new ClassTypeSchema
                                    {
                                        ClassFullName = classAncestorClassPropertyClassTypeSchema.ClassFullName,
                                        ClassGenerics = classAncestorClassPropertyClassTypeSchema.ClassGenerics,
                                        IsClassNativeHandle = true
                                    };

                                    classSrcBuilder.Append($"public {GetTypeSrc.Execute(classAncestorClassPropertyPtrTypeSchema)} {classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix}");
                                }
                                else
                                {
                                    throw new System.InvalidOperationException();
                                }
                            }

                            classSrcBuilder.Append("{");
                            classSrcBuilder.Indent++;

                            classSrcBuilder.Append("get");
                            classSrcBuilder.Append("{");
                            classSrcBuilder.Indent++;

                            if (classAncestorClassPropertySchema.Type is PrimitiveTypeSchema)
                            {
                                classSrcBuilder.Append($"return {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}NativeDataPtr{classAncestorClassPropertyNameSuffix};");
                            }
                            else if (classAncestorClassPropertySchema.Type is ClassTypeSchema)
                            {
                                classSrcBuilder.Append($"return {classAncestorClassCastSrc}.{classAncestorClassPropertySchema.Name}{classAncestorClassPropertyNameSuffix};");
                            }
                            else
                            {
                                throw new System.InvalidOperationException();
                            }

                            classSrcBuilder.Indent--;
                            classSrcBuilder.Append("}");

                            classSrcBuilder.Indent--;
                            classSrcBuilder.Append("}");
                        }
                    }
                }

                classAncestorClassFullName = classAncestorClassSchema.ParentClassRef?.ClassFullName;
            }
        }

        if (classSchema is IRegisteredClassSchema registeredClassSchema2)
        {
            foreach (var classPropertySchema in registeredClassSchema2.Properties)
            {
                var classPropertyNameSuffix = classOverloadedMethodsName.Contains(classPropertySchema.Name) ? "_" : "";

                if (classPropertySchema.IsArray)
                {
                    if (classPropertySchema.Type is PrimitiveTypeSchema classPropertyPrimitiveTypeSchema)
                    {
                        if (classPropertyPrimitiveTypeSchema.PrimitiveFullName == "string")
                        {
                            classSrcBuilder.Append($"public ClassRegisteredPropertyElementsStringValueNativeDataAccessor {classPropertySchema.Name}{classPropertyNameSuffix} => new(NativeDataRawPtr, {classPropertySchema.Name}Property.Info.GetApiClassField());");
                        }
                        else
                        {
                            classSrcBuilder.Append($"public ClassRegisteredPropertyElementsGenericValueNativeDataAccessor<{GetTypeSrc.Execute(classPropertyPrimitiveTypeSchema)}> {classPropertySchema.Name}{classPropertyNameSuffix} => new(NativeDataRawPtr, {classPropertySchema.Name}Property.Info.GetApiClassField());");
                        }
                    }
                    else if (classPropertySchema.Type is ClassTypeSchema classPropertyClassTypeSchema)
                    {
                        classSrcBuilder.Append($"public ClassRegisteredPropertyElementsGenericValueNativeDataAccessor<{classPropertyClassTypeSchema.ClassFullName}.NativeData> {classPropertySchema.Name}NativeData{classPropertyNameSuffix} => new(NativeDataRawPtr, {classPropertySchema.Name}Property.Info.GetApiClassField());");
                        classSrcBuilder.Append($"public ClassRegisteredPropertyElementsGenericValueNativeHandleAccessor<{classPropertyClassTypeSchema.ClassFullName}.NativeHandle> {classPropertySchema.Name}{classPropertyNameSuffix} => new(NativeDataRawPtr, Info.GetApiClass(),{classPropertySchema.Name}Property.Info.GetApiClassField());");
                    }
                    else
                    {
                        throw new System.InvalidOperationException();
                    }
                }
                else
                {
                    if (classPropertySchema.Type is PrimitiveTypeSchema)
                    {
                        classSrcBuilder.Append($"public {GetTypeSrc.Execute(classPropertySchema.Type)} {classPropertySchema.Name}{classPropertyNameSuffix}");
                    }
                    else if (classPropertySchema.Type is ClassTypeSchema)
                    {
                        classSrcBuilder.Append($"public {GetTypeSrc.Execute(classPropertySchema.Type)} {classPropertySchema.Name}NativeData{classPropertyNameSuffix}");
                    }
                    else
                    {
                        throw new System.InvalidOperationException();
                    }

                    classSrcBuilder.Append("{");
                    classSrcBuilder.Indent++;

                    classSrcBuilder.Append("get");
                    classSrcBuilder.Append("{");
                    classSrcBuilder.Indent++;

                    classSrcBuilder.Append($"var apiClassField = {classPropertySchema.Name}Property.Info.GetApiClassField();");

                    {
                        if (classPropertySchema.Type is PrimitiveTypeSchema classPropertyPrimitiveTypeSchema && classPropertyPrimitiveTypeSchema.PrimitiveFullName == "string")
                        {
                            classSrcBuilder.Append($"nint returnValueNativeDataRawPtr = global::System.Runtime.InteropServices.Marshal.AllocCoTaskMem(apiClassField.NativeDataPtr->StringMaxLength);");
                            classSrcBuilder.Append($"apiClassField.GetMemberData1(NativeDataRawPtr, 0, returnValueNativeDataRawPtr, apiClassField.NativeDataPtr->StringMaxLength, 0, 0);");
                            classSrcBuilder.Append("var returnValue = global::System.Runtime.InteropServices.Marshal.PtrToStringUTF8(returnValueNativeDataRawPtr);");
                            classSrcBuilder.Append("global::System.Runtime.InteropServices.Marshal.FreeCoTaskMem(returnValueNativeDataRawPtr);");
                        }
                        else
                        {
                            classSrcBuilder.Append($"{GetTypeSrc.Execute(classPropertySchema.Type)} returnValue;");
                            classSrcBuilder.Append($"apiClassField.GetMemberData1(NativeDataRawPtr, 0, (nint)(&returnValue), sizeof({GetTypeNativeSrc.Execute(classPropertySchema.Type)}), 0, 0);");
                        }
                    }

                    classSrcBuilder.Append("return returnValue;");

                    classSrcBuilder.Indent--;
                    classSrcBuilder.Append("}");

                    classSrcBuilder.Append("set");
                    classSrcBuilder.Append("{");
                    classSrcBuilder.Indent++;

                    classSrcBuilder.Append($"var apiClassField = {classPropertySchema.Name}Property.Info.GetApiClassField();");

                    {
                        if (classPropertySchema.Type is PrimitiveTypeSchema classPropertyPrimitiveTypeSchema && classPropertyPrimitiveTypeSchema.PrimitiveFullName == "string")
                        {
                            classSrcBuilder.Append($"var valueNativeDataRawPtr = global::System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8(value);");
                            classSrcBuilder.Append($"apiClassField.SetMemberData1(NativeDataRawPtr, 0, valueNativeDataRawPtr, value == null ? 0 : value.Length + 1, 0, 0);");
                            classSrcBuilder.Append("global::System.Runtime.InteropServices.Marshal.FreeCoTaskMem(valueNativeDataRawPtr);");
                        }
                        else
                        {
                            classSrcBuilder.Append($"apiClassField.SetMemberData1(NativeDataRawPtr, 0, (nint)(&value), sizeof({GetTypeSrc.Execute(classPropertySchema.Type)}), 0, 0);");
                        }
                    }

                    classSrcBuilder.Indent--;
                    classSrcBuilder.Append("}");

                    classSrcBuilder.Indent--;
                    classSrcBuilder.Append("}");

                    ITypeSchema classPropertyPtrTypeSchema;

                    {
                        if (classPropertySchema.Type is PrimitiveTypeSchema classPropertyPrimitiveTypeSchema)
                        {
                            classPropertyPtrTypeSchema = new PrimitiveTypeSchema
                            {
                                PrimitiveFullName = classPropertyPrimitiveTypeSchema.PrimitiveFullName,
                                IsPrimitiveNativeDataPtr = true
                            };

                            classSrcBuilder.Append($"public {GetTypeSrc.Execute(classPropertyPtrTypeSchema)} {classPropertySchema.Name}NativeDataPtr{classPropertyNameSuffix}");
                        }
                        else if (classPropertySchema.Type is ClassTypeSchema classPropertyClassTypeSchema)
                        {
                            classPropertyPtrTypeSchema = new ClassTypeSchema
                            {
                                ClassFullName = classPropertyClassTypeSchema.ClassFullName,
                                ClassGenerics = classPropertyClassTypeSchema.ClassGenerics,
                                IsClassNativeHandle = true
                            };

                            classSrcBuilder.Append($"public {GetTypeSrc.Execute(classPropertyPtrTypeSchema)} {classPropertySchema.Name}{classPropertyNameSuffix}");
                        }
                        else
                        {
                            throw new System.InvalidOperationException();
                        }
                    }

                    classSrcBuilder.Append("{");
                    classSrcBuilder.Indent++;

                    classSrcBuilder.Append("get");
                    classSrcBuilder.Append("{");
                    classSrcBuilder.Indent++;

                    classSrcBuilder.Append($"var apiClassField = {classPropertySchema.Name}Property.Info.GetApiClassField();");

                    classSrcBuilder.Append($"return ({GetTypeSrc.Execute(classPropertyPtrTypeSchema)})(apiClassField.GetMemberDataPtr2(NativeDataRawPtr));");

                    classSrcBuilder.Indent--;
                    classSrcBuilder.Append("}");

                    classSrcBuilder.Indent--;
                    classSrcBuilder.Append("}");
                }
            }
        }


        foreach (var classMethodSchema in classSchema.Methods)
        {
            if (classMethodSchema.IsStatic)
            {
                continue;
            }

            classSrcBuilder.Append();

            if (classMethodSchema.ReturnType == null)
            {
                classSrcBuilder.Append($"public readonly void {classMethodSchema.Name}(");
            }
            else
            {
                classSrcBuilder.Append($"public readonly {GetTypeSrc.Execute(classMethodSchema.ReturnType)} {classMethodSchema.Name}(");
            }
            classSrcBuilder.Indent++;

            foreach (var classMethodArgSchema in classMethodSchema.Args)
            {
                var isClassMethodArgLast = classMethodArgSchema == classMethodSchema.Args.Last();

                classSrcBuilder.Append($"{GetClassMethodArgDefSrc.Execute(classMethodArgSchema)}{(isClassMethodArgLast ? "" : ",")}");
            }

            classSrcBuilder.Indent--;
            classSrcBuilder.Append(")");

            classSrcBuilder.Append("{");
            classSrcBuilder.Indent++;

            if (classMethodSchema is IClassVirtualMethodSchema)
            {
                classSrcBuilder.Append($"var methodNativeDelegate = Native.GetObjectVtableMethodDelegate<{classMethodSchema.Name}Method.NativeDelegate>(NativeDataRawPtr, {classMethodSchema.Name}Method.Info.NativeVtableIndex);");
            }
            else
            {
                classSrcBuilder.Append($"var methodNativeDelegate = Native.GetMethodDelegate<{classMethodSchema.Name}Method.NativeDelegate>({classMethodSchema.Name}Method.Info.NativePtr);");
            }

            foreach (var classMethodArgSchema in classMethodSchema.Args)
            {
                if (
                    classMethodArgSchema.Type is PrimitiveTypeSchema classMethodArgPrimitiveTypeSchema
                    &&
                    classMethodArgPrimitiveTypeSchema.PrimitiveFullName == "string"
                )
                {
                    classSrcBuilder.Append($"var {classMethodArgSchema.Name}NativeDataRawPtr = global::System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8({classMethodArgSchema.Name});");
                }
            }

            if (classMethodSchema.ReturnType == null)
            {
                classSrcBuilder.Append($"methodNativeDelegate(");
            }
            else
            {
                if (
                    classMethodSchema.ReturnType is PrimitiveTypeSchema classMethodReturnTypePrimitiveTypeSchema
                    &&
                    classMethodReturnTypePrimitiveTypeSchema.PrimitiveFullName == "string"
                )
                {
                    classSrcBuilder.Append($"var returnValueNativeDataRawPtr = methodNativeDelegate(");
                }
                else if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
                {
                    classSrcBuilder.Append($"{GetTypeSrc.Execute(classMethodSchema.ReturnType)} returnValueNativeData;");
                    classSrcBuilder.Append($"methodNativeDelegate(");
                }
                else
                {
                    classSrcBuilder.Append($"var returnValue = ({GetTypeSrc.Execute(classMethodSchema.ReturnType)})methodNativeDelegate(");
                }
            }

            classSrcBuilder.Indent++;

            if (classMethodSchema is IClassRegisteredMethodSchema classRegisteredMethodSchema && classRegisteredMethodSchema.NativeDataRawPtrOffset != null)
            {
                classSrcBuilder.Append($"NativeDataRawPtr + 0x{classRegisteredMethodSchema.NativeDataRawPtrOffset.Value:X}{(!classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled && classMethodSchema.Args.Count == 0 ? "" : ",")}");
            }
            else
            {
                classSrcBuilder.Append($"NativeDataRawPtr{(!classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled && classMethodSchema.Args.Count == 0 ? "" : ",")}");
            }

            if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
            {
                classSrcBuilder.Append($"(nint)(&returnValueNativeData){(classMethodSchema.Args.Count == 0 ? "" : ",")}");
            }

            foreach (var classMethodArgSchema in classMethodSchema.Args)
            {
                var isClassMethodArgLast = classMethodArgSchema == classMethodSchema.Args.Last();

                classSrcBuilder.Append($"{GetClassMethodArgCallSrc.Execute(classMethodArgSchema)}{(isClassMethodArgLast ? "" : ",")}");
            }

            classSrcBuilder.Indent--;
            classSrcBuilder.Append(");");

            foreach (var classMethodArgSchema in classMethodSchema.Args)
            {
                if (
                    classMethodArgSchema.Type is PrimitiveTypeSchema classMethodArgPrimitiveTypeSchema
                    &&
                    classMethodArgPrimitiveTypeSchema.PrimitiveFullName == "string"
                )
                {
                    classSrcBuilder.Append($"global::System.Runtime.InteropServices.Marshal.FreeCoTaskMem({classMethodArgSchema.Name}NativeDataRawPtr);");
                }
            }

            if (classMethodSchema.ReturnType != null)
            {
                if (
                    classMethodSchema.ReturnType is PrimitiveTypeSchema classMethodReturnTypePrimitiveTypeSchema
                    &&
                    classMethodReturnTypePrimitiveTypeSchema.PrimitiveFullName == "string"
                )
                {
                    classSrcBuilder.Append("return global::System.Runtime.InteropServices.Marshal.PtrToStringUTF8(returnValueNativeDataRawPtr);");
                }
                else if (classMethodSchema.IsNativeReturnValueByParamOptimizationEnabled)
                {
                    classSrcBuilder.Append("return returnValueNativeData;");
                }
                else
                {
                    classSrcBuilder.Append("return returnValue;");
                }
            }

            classSrcBuilder.Indent--;
            classSrcBuilder.Append("}");
        }

        classSrcBuilder.Indent--;
        classSrcBuilder.Append("}");

        classSrcBuilder.Indent--;
        classSrcBuilder.Append("}");

        foreach (var _ in classNamespaceParts)
        {
            classSrcBuilder.Indent--;
            classSrcBuilder.Append("}");
        }

        return classSrcBuilder.ToString().TrimEnd();
    }
}