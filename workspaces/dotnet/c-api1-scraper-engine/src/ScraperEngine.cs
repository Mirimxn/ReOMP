using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

namespace OMP.LSWTSS.CApi1;

public static class ScraperEngine
{
    static readonly List<ApiClass.NativeHandle> _apiClasses = [];

    static readonly Dictionary<string, string> _apiClassesFormattedNameMap = [];

    public static string GetApiClassFormattedName(string apiClassName)
    {
        string result = "";

        var apiClassNameGenericCharIndex = apiClassName.IndexOf('<');

        var apiClassNameMainPart = (
            apiClassNameGenericCharIndex == -1 ? apiClassName : apiClassName[..apiClassNameGenericCharIndex]
        ).Replace("::", "#");

        var apiClassNameGenericPart = (
            apiClassNameGenericCharIndex == -1 ? null : apiClassName[(apiClassNameGenericCharIndex + 1)..^1]
        )?.Replace("::", "@");

        bool makeNextUpper = true;

        var apiClassNamePreprocessed = apiClassNameMainPart + (apiClassNameGenericPart == null ? "" : $"<{apiClassNameGenericPart}>");

        for (int i = 0; i < apiClassNamePreprocessed.Length; i++)
        {
            var apiClassNamePreprocessedChar = apiClassNamePreprocessed[i];

            if (
                apiClassNamePreprocessedChar == ' '
                ||
                apiClassNamePreprocessedChar == '-'
                ||
                apiClassNamePreprocessedChar == '_'
                ||
                apiClassNamePreprocessedChar == '.'
            )
            {
                makeNextUpper = true;
            }
            else
            {
                if (makeNextUpper)
                {
                    result += char.ToUpper(apiClassNamePreprocessedChar);

                    makeNextUpper = false;
                }
                else
                {
                    result += apiClassNamePreprocessedChar;
                }
            }
        }

        return result
            .Replace("*", "Ptr")
            .Replace("&", "Ref")
            .Replace("#", ".")
            .Replace("<", "__")
            .Replace(",", "__")
            .Replace("@", "_")
            .Replace(">", "");
    }

    static readonly CFuncHook1<ApiRegistry.RegisterApiClassMethod.NativeDelegate> _apiRegistryRegisterApiClassMethodHook = new(
        ApiRegistry.RegisterApiClassMethod.Info.NativePtr,
        (
            nint nativeDataRawPtr,
            nint apiClassNameNativeDataRawPtr,
            nint apiClassInterfaceNativeDataRawPtr,
            nint param3,
            nint edFileDescNativeDataRawPtr,
            nint param5,
            float param6,
            nint param7,
            int edClassSerializationTarget
        ) =>
        {
            var apiClassNativeDataRawPtr = _apiRegistryRegisterApiClassMethodHook!.Trampoline!(
                nativeDataRawPtr,
                apiClassNameNativeDataRawPtr,
                apiClassInterfaceNativeDataRawPtr,
                param3,
                edFileDescNativeDataRawPtr,
                param5,
                param6,
                param7,
                edClassSerializationTarget
            );

            var apiClass = (ApiClass.NativeHandle)apiClassNativeDataRawPtr;

            _apiClasses.Add(apiClass);

            var apiClassName = apiClass.AsApiType().GetName()!;

            _apiClassesFormattedNameMap[apiClassName] = GetApiClassFormattedName(apiClassName);

            return apiClassNativeDataRawPtr;
        }
    );

    public static string GetClassFullName(string apiClassName)
    {
        if (!_apiClassesFormattedNameMap.TryGetValue(apiClassName, out var apiClassFormattedName))
        {
            return GetApiClassFormattedName(apiClassName);
        }

        var apiClassFormattedOverloads = _apiClassesFormattedNameMap
            .Where(x => x.Value == apiClassFormattedName)
            .Select(x => x.Key)
            .ToArray();

        return apiClassFormattedName + (apiClassFormattedOverloads.Length > 1 ? Array.IndexOf(apiClassFormattedOverloads, apiClassName) + 1 : "");
    }

    public static string GetClassPropertyOrMethodName(string apiClassFieldOrFunctionName)
    {
        string result = "";

        bool makeNextUpper = true;

        for (int i = 0; i < apiClassFieldOrFunctionName.Length; i++)
        {
            var apiClassFieldOrFunctionNameChar = apiClassFieldOrFunctionName[i];

            if (i == 0 && char.IsDigit(apiClassFieldOrFunctionNameChar))
            {
                result += "N";
            }

            if (
                apiClassFieldOrFunctionNameChar == ' '
                ||
                apiClassFieldOrFunctionNameChar == '_'
                ||
                apiClassFieldOrFunctionNameChar == '-'
                ||
                apiClassFieldOrFunctionNameChar == '('
                ||
                apiClassFieldOrFunctionNameChar == ')'
                ||
                apiClassFieldOrFunctionNameChar == '['
                ||
                apiClassFieldOrFunctionNameChar == ']'
                ||
                apiClassFieldOrFunctionNameChar == '!'
                ||
                apiClassFieldOrFunctionNameChar == '/'
                ||
                apiClassFieldOrFunctionNameChar == '<'
                ||
                apiClassFieldOrFunctionNameChar == '>'
                ||
                apiClassFieldOrFunctionNameChar == '='
                ||
                apiClassFieldOrFunctionNameChar == '.'
                ||
                apiClassFieldOrFunctionNameChar == ':'
                ||
                apiClassFieldOrFunctionNameChar == '?'
                ||
                apiClassFieldOrFunctionNameChar == '+'
                ||
                apiClassFieldOrFunctionNameChar == '#'
                ||
                apiClassFieldOrFunctionNameChar == '	'
                ||
                apiClassFieldOrFunctionNameChar == '&'
                ||
                apiClassFieldOrFunctionNameChar == '*'
                ||
                apiClassFieldOrFunctionNameChar == ','
            )
            {
                makeNextUpper = true;
            }
            else
            {
                if (makeNextUpper)
                {
                    result += char.ToUpper(apiClassFieldOrFunctionNameChar);

                    makeNextUpper = false;
                }
                else
                {
                    result += apiClassFieldOrFunctionNameChar;
                }
            }
        }

        return result.Replace("'s", "").Replace("'", "").Replace("%", "Percentage");
    }

    static ITypeSchema GetTypeSchema(string apiTypeName, bool isApiTypePtr, bool isApiTypeRef)
    {
        return (isApiTypePtr || isApiTypeRef) switch
        {
            true => apiTypeName switch
            {
                "Char" or "char" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "string",
                    IsPrimitiveNativeDataPtr = false
                },
                "UChar" or "uchar" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "byte",
                    IsPrimitiveNativeDataPtr = true
                },
                "Short" or "short" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "short",
                    IsPrimitiveNativeDataPtr = true
                },
                "Int" or "int" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "int",
                    IsPrimitiveNativeDataPtr = true
                },
                "Int64" or "int64" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "long",
                    IsPrimitiveNativeDataPtr = true
                },
                "Half" or "half" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "System.Half",
                    IsPrimitiveNativeDataPtr = true
                },
                "Float" or "float" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "float",
                    IsPrimitiveNativeDataPtr = true
                },
                "String" or "string" => throw new System.InvalidOperationException(),
                "Ptr" or "ptr" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "nint",
                    IsPrimitiveNativeDataPtr = false
                },
                "unknown" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "nint",
                    IsPrimitiveNativeDataPtr = false
                },
                _ => new ClassTypeSchema
                {
                    ClassFullName = GetClassFullName(apiTypeName),
                    ClassGenerics = null,
                    IsClassNativeHandle = true
                }
            },
            false => apiTypeName switch
            {
                "Bool" or "bool"=> new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "bool",
                    IsPrimitiveNativeDataPtr = false
                },
                "Char" or "char" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "bool",
                    IsPrimitiveNativeDataPtr = false
                },
                "UChar" or "uchar" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "byte",
                    IsPrimitiveNativeDataPtr = false
                },
                "Short" or "short" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "short",
                    IsPrimitiveNativeDataPtr = false
                },
                "Int" or "int" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "int",
                    IsPrimitiveNativeDataPtr = false
                },
                "Int64" or "int64" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "long",
                    IsPrimitiveNativeDataPtr = false
                },
                "Half" or "half" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "System.Half",
                    IsPrimitiveNativeDataPtr = false
                },
                "Float" or "float" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "float",
                    IsPrimitiveNativeDataPtr = false
                },
                "String" or "String*" or "string" or "string*" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "string",
                    IsPrimitiveNativeDataPtr = false
                },
                "Ptr" or "ptr" => new PrimitiveTypeSchema
                {
                    PrimitiveFullName = "nint",
                    IsPrimitiveNativeDataPtr = false
                },
                "VuMtx" => new ClassTypeSchema
                {
                    ClassFullName = "NuMtx",
                    ClassGenerics = null,
                    IsClassNativeHandle = false
                },
                "VuVec" => new ClassTypeSchema
                {
                    ClassFullName = "Vec4",
                    ClassGenerics = null,
                    IsClassNativeHandle = false
                },
                "unknown" => throw new InvalidOperationException(),
                _ => new ClassTypeSchema
                {
                    ClassFullName = GetClassFullName(apiTypeName),
                    ClassGenerics = null,
                    IsClassNativeHandle = false
                }
            }
        };
    }

    static bool GetIsApiClassSkipped(string apiClassName)
    {
        return (
            apiClassName == "Char"
            ||
            apiClassName == "UChar"
            ||
            apiClassName == "Short"
            ||
            apiClassName == "Int"
            ||
            apiClassName == "Int64"
            ||
            apiClassName == "Float"
            ||
            apiClassName == "unknown"
            ||
            apiClassName.EndsWith("_epic")
            ||
            apiClassName.EndsWith("_steam")
        );
    }

    public static unsafe void Scrape()
    {
        var schema = new Schema
        {
            Classes = [],
        };

        foreach (var apiClass in _apiClasses)
        {
            if (!apiClass.AsApiType().IsApiClass())
            {
                continue;
            }

            var apiClassName = apiClass.AsApiType().GetName()!;

            if (GetIsApiClassSkipped(apiClassName))
            {
                continue;
            }

            var classFullName = GetClassFullName(apiClassName);

            var classFullNameParts = classFullName.Split(".");

            var apiClassBaseClassField = apiClass.NativeDataPtr->BaseClassField;

            ClassRefSchema? classParentClassRefSchema = null;

            if (apiClassBaseClassField != nint.Zero)
            {
                var apiClassBaseClassFieldTypeName = apiClassBaseClassField.GetTypeName()!;

                var apiClassBaseClassFieldTrueClass = apiClassBaseClassField.GetTrueClass();

                if (apiClassBaseClassFieldTrueClass != nint.Zero)
                {
                    apiClassBaseClassFieldTypeName = apiClassBaseClassFieldTrueClass.AsApiType().GetName()!;
                }

                if (!GetIsApiClassSkipped(apiClassBaseClassFieldTypeName))
                {
                    classParentClassRefSchema = new ClassRefSchema
                    {
                        ClassFullName = GetClassFullName(apiClassBaseClassFieldTypeName)
                    };
                }
            }

            var apiClassSize = apiClass.AsApiType().GetSize();

            var classSchema = new RegisteredClassSchema
            {
                ApiClassName = apiClassName,
                Properties = [],
                Namespace = classFullNameParts.Length > 1 ? string.Join(".", classFullNameParts[..^1]) : null,
                Name = classFullNameParts.Last(),
                ParentClassRef = classParentClassRefSchema,
                NativeDataSize = (uint)apiClassSize,
                Fields = null,
                Methods = [],
            };

            var classPropertyNameCounter = new Dictionary<string, int>();

            var apiClassFields = apiClass.GetFields();

            foreach (var apiClassField in apiClassFields)
            {
                var apiClassFieldName = apiClassField.GetName()!;

                var apiClassFieldTypeName = apiClassField.GetTypeName()!;

                var apiClassFieldTrueClass = apiClassField.GetTrueClass();

                if (apiClassFieldTrueClass != nint.Zero)
                {
                    apiClassFieldTypeName = apiClassFieldTrueClass.AsApiType().GetName()!;
                }

                var classPropertyName = GetClassPropertyOrMethodName(apiClassFieldName);

                if (classPropertyName == "")
                {
                    continue;
                }

                var isClassPropertyOverloaded = apiClassFields.Count(x => GetClassPropertyOrMethodName(x.GetName()!) == classPropertyName) > 1;
                var classPropertyNameIndex = classPropertyNameCounter.GetValueOrDefault(classPropertyName);

                var classPropertyTypeSchema = GetTypeSchema(
                    apiClassFieldTypeName.Replace("[]", "").Replace("(256)", ""),
                    false,
                    false
                );

                classSchema.Properties.Add(
                    new ClassRegisteredPropertySchema
                    {
                        ApiClassFieldName = apiClassFieldName,
                        Name = classPropertyName + (isClassPropertyOverloaded ? (classPropertyNameIndex + 1).ToString() : ""),
                        Type = classPropertyTypeSchema,
                        IsArray = apiClassField.GetIsArray()
                    }
                );

                classPropertyNameCounter[classPropertyName] = classPropertyNameIndex + 1;
            }

            var classMethodNameCounter = new Dictionary<string, int>();

            var apiClassFunctions = apiClass.GetFunctions();

            foreach (var apiClassFunction in apiClassFunctions)
            {
                var apiClassFunctionName = apiClassFunction.GetName()!;

                var apiClassFunctionPrototype = apiClassFunction.NativeDataPtr->Prototype;

                var classMethodNativeOffset = Native.GetOffsetFromPtr(apiClassFunction.NativeDataPtr->ExecuteNativePtr);

                uint? classMethodNativeSteamRuntimeOffset = null;
                uint? classMethodNativeEGSRuntimeOffset = null;

                switch (Native.GetCurrentRuntimeKind())
                {
                    case NativeRuntimeKind.SteamRuntime:
                        classMethodNativeSteamRuntimeOffset = classMethodNativeOffset;
                        break;
                    case NativeRuntimeKind.EGSRuntime:
                        classMethodNativeEGSRuntimeOffset = classMethodNativeOffset;
                        break;
                }

                var apiClassFunctionPrototypeReturnTypeName = apiClassFunctionPrototype.NativeDataPtr->ReturnType.GetName()!;

                var isApiClassFunctionPrototypeReturnTypePtr = apiClassFunctionPrototype.GetIsReturnTypePtr();

                var isApiClassFunctionPrototypeReturnTypeRef = apiClassFunctionPrototype.GetIsReturnTypeRef();

                ITypeSchema? classMethodReturnTypeSchema = null;

                if (
                    apiClassFunctionPrototypeReturnTypeName != "unknown"
                    ||
                    isApiClassFunctionPrototypeReturnTypePtr
                    ||
                    isApiClassFunctionPrototypeReturnTypeRef
                )
                {
                    classMethodReturnTypeSchema = GetTypeSchema(
                        apiClassFunctionPrototypeReturnTypeName,
                        isApiClassFunctionPrototypeReturnTypePtr,
                        isApiClassFunctionPrototypeReturnTypeRef
                    );
                }

                var classMethodName = GetClassPropertyOrMethodName(apiClassFunctionName);

                var isClassMethodOverloaded = apiClassFunctions.Count(x => GetClassPropertyOrMethodName(x.GetName()!) == classMethodName) > 1;
                var classMethodNameIndex = classMethodNameCounter.GetValueOrDefault(classMethodName);

                var classMethodSchema = new ClassRegisteredMethodSchema
                {
                    ApiFunctionName = apiClassFunctionName,
                    NativeSteamRuntimeOffset = classMethodNativeSteamRuntimeOffset,
                    NativeEGSRuntimeOffset = classMethodNativeEGSRuntimeOffset,
                    Name = classMethodName + (isClassMethodOverloaded ? (classMethodNameIndex + 1).ToString() : ""),
                    IsStatic = !apiClassFunction.GetIsMethod(),
                    ReturnType = classMethodReturnTypeSchema,
                    Args = [],
                    IsNativeReturnValueByParamOptimizationEnabled = (
                        classMethodReturnTypeSchema is ClassTypeSchema classMethodReturnClassTypeSchema
                        &&
                        !classMethodReturnClassTypeSchema.IsClassNativeHandle
                    ),
                    NativeDataRawPtrOffset = apiClassFunction.GetNativeDataRawPtrOffset()
                };

                classMethodNameCounter[classMethodName] = classMethodNameIndex + 1;

                var apiClassFunctionPrototypeArgTypes = apiClassFunctionPrototype.GetArgTypes();

                for (var apiClassFunctionPrototypeArgTypeIndex = apiClassFunction.GetIsMethod() ? (byte)1 : (byte)0; apiClassFunctionPrototypeArgTypeIndex < apiClassFunctionPrototypeArgTypes.Length; apiClassFunctionPrototypeArgTypeIndex++)
                {
                    var apiClassFunctionArgType = apiClassFunctionPrototypeArgTypes[apiClassFunctionPrototypeArgTypeIndex];

                    var apiClassFunctionArgTypeName = apiClassFunctionArgType.GetName()!;

                    var isApiClassFunctionArgTypePtr = apiClassFunctionPrototype.GetIsArgTypePtr(apiClassFunctionPrototypeArgTypeIndex);

                    var isApiClassFunctionArgTypeRef = apiClassFunctionPrototype.GetIsArgTypeRef(apiClassFunctionPrototypeArgTypeIndex);

                    classMethodSchema.Args.Add(
                        new ClassMethodArgSchema
                        {
                            Name = $"arg{apiClassFunctionPrototypeArgTypeIndex}",
                            Type = GetTypeSchema(
                                apiClassFunctionArgTypeName,
                                isApiClassFunctionArgTypePtr,
                                isApiClassFunctionArgTypeRef
                            )
                        }
                    );
                }

                classSchema.Methods.Add(classMethodSchema);
            }

            schema.Classes.Add(classSchema);
        }

        var schemaAsJson = JsonConvert.SerializeObject(
            schema,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            }
        );

        var schemaFileDirPath = Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "..",
                "..",
                "..",
                "..",
                "c-api1-main"
            )
        );

        var schemaFileName = Native.GetCurrentRuntimeValue(
            steamRuntimeValue: "SteamSchema.json",
            egsRuntimeValue: "EGSSchema.json"
        );

        var schemaFilePath = Path.Combine(schemaFileDirPath, "src", schemaFileName);

        Console.WriteLine(schemaFilePath);

        File.WriteAllText(schemaFilePath, schemaAsJson);
    }

    public static int Init(IntPtr args, int sizeBytes)
    {
        _apiRegistryRegisterApiClassMethodHook.Enable();

        ThreadPool.QueueUserWorkItem(
            _ =>
            {
                try
                {
                    Thread.Sleep(5000);

                    Console.WriteLine("Scraping...");

                    Scrape();

                    Process.GetCurrentProcess().Kill();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        );

        return 0;
    }
}