using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OMP.LSWTSS;

public static partial class V1
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool CreateHardLink(
        string lpFileName,
        string lpExistingFileName,
        IntPtr lpSecurityAttributes
    );

    static void LinkCustomResource(CustomResourceInfo customResourceInfo)
    {
        var currentDirectoryPath = Environment.CurrentDirectory;

        var customResourceAbsolutePath = Path.Combine(currentDirectoryPath, customResourceInfo.Path);

        var customResourceDirAbsolutePath = Path.GetDirectoryName(customResourceAbsolutePath) ?? throw new InvalidOperationException();

        Directory.CreateDirectory(customResourceDirAbsolutePath);

        if (File.Exists(customResourceAbsolutePath))
        {
            File.Delete(customResourceAbsolutePath);
        }

        CreateHardLink(customResourceAbsolutePath, customResourceInfo.SrcPath, IntPtr.Zero);

        _diskResourcesInfo.Add(
            new DiskResourceInfo
            {
                CanonPath = GetResourceCanonPath(customResourceInfo.Path)
            }
        );

        Console.WriteLine($"Linked custom resource: {customResourceInfo.Path}");
    }
}