namespace OMP.LSWTSS;

// Builds everything `build-all` builds except `c-api1`. `c-api1` is the only
// component that needs the game installed (it scrapes each version's native API
// at build time), so this target can run with only the Steam version - or no
// game at all - which is useful for producing a release that reuses an
// upstream-built `c-api1`.
public static class BuildAllNoCApi1
{
    public static void Execute()
    {
        // .NET-only components first, so they still build even if the toolchain
        // for the later components is missing.
        BuildCFuncHook1.Execute();
        BuildInputHook1.Execute();
        BuildOverlay1.Execute();
        BuildV1.Execute();
        BuildDebugTools.Execute();
        BuildTestCefMod.Execute();

        // These need extra toolchains: galaxy-unleashed builds a JS overlay
        // (Node/Yarn) and bundle builds the dinput8 driver (Rust/cargo). Kept
        // last so a missing toolchain doesn't block the components above.
        BuildGalaxyUnleashed.Execute();
        BuildBundle.Execute();
    }
}
