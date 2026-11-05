namespace OMP.LSWTSS;

public static partial class RuntimeEngine
{
    static void LoadModIfUnloaded(ModState modState)
    {
        if (modState.IsLoaded)
        {
            return;
        }

        modState.IsBeingLoaded = true;

        if (modState.Info.Dependencies != null)
        {
            foreach (var modDependencyInfo in modState.Info.Dependencies)
            {
                var modDependencyState = _modsState.Find(modState => modState.Id == modDependencyInfo.Id);

                if (modDependencyState == null)
                {
                    throw Crash($"Cannot find mod dependency {modDependencyInfo.Id} for {modState.Id}");
                }

                if (modDependencyState.IsBeingLoaded)
                {
                    throw Crash($"Circular mod dependency detected for {modDependencyState.Id} when loading {modState.Id}");
                }

                LoadModIfUnloaded(modDependencyState);
            }
        
        }

        foreach (var modActionInfo in modState.Info.Actions)
        {
            ExecuteModAction(modState.Id, modState.DirPath, modActionInfo);
        }

        modState.IsBeingLoaded = false;

        modState.IsLoaded = true;
    }
}