using System;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed : IDisposable
{
    public static bool InputHookClientHandler(in InputHook1.NativeMessage inputHookClientNativeMessage)
    {
        if (_instance == null)
        {
            return false;
        }

        if (_instance._modeState is PlayModeState)
        {
            if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F1)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    var closestNpcSpawnerInPlayerEntityRange = _instance!._closestNpcSpawnerInPlayerEntityRange;

                    if (closestNpcSpawnerInPlayerEntityRange != null)
                    {
                        _instance!._modeState = new MenuModeState
                        {
                            Config = new()
                            {
                                NavigateParams = new()
                                {
                                    To = $"/menu-mode/menu/npcs/manage-spawners/{closestNpcSpawnerInPlayerEntityRange.State.Id}",
                                    Search = [],
                                }
                            }
                        };
                    }
                    else
                    {
                        _instance!._modeState = new MenuModeState
                        {
                            Config = new()
                            {
                                NavigateParams = new()
                                {
                                    To = "/menu-mode/menu",
                                    Search = [],
                                }
                            }
                        };
                    }
                }

                return true;
            }
        }
        else if (_instance._modeState is MenuModeState)
        {
            if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_ESCAPE)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    _instance!._modeState = new PlayModeState
                    {
                    };
                }

                return true;
            }
        }
        else if (_instance._modeState is QuickSpawnNpcsModeState quickSpawnNpcsModeState)
        {
            if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F1)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    _instance!._modeState = new MenuModeState
                    {
                        Config = new()
                        {
                            NavigateParams = new()
                            {
                                To = "/menu-mode/menu/npcs/quick-spawn",
                                Search = [],
                            }
                        }
                    };
                }

                return true;
            }
            else if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F2)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    if (_instance._playerEntityLastPosition != null)
                    {
                        var _ = new SpawnNpcTask(
                            quickSpawnNpcsModeState.Config.NpcPreset,
                            quickSpawnNpcsModeState.Config.IsNpcBattleParticipant,
                            _instance._playerEntityLastPosition.Value,
                            isGlobal: true
                        );
                    }
                }

                return true;
            }
        }
        else if (_instance._modeState is CreateNpcSpawnersModeState createNpcSpawnersModeState)
        {
            if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F1)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    _instance!._modeState = new MenuModeState
                    {
                        Config = new()
                        {
                            NavigateParams = new()
                            {
                                To = "/menu-mode/menu/npcs/create-spawners",
                                Search = [],
                            }
                        }
                    };
                }

                return true;
            }
            else if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F2)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    if (_instance._playerEntityLastPosition != null)
                    {
                        var _ = new NpcSpawner(
                            createNpcSpawnersModeState.Config.NpcSpawner,
                            _instance._playerEntityLastPosition.Value
                        );
                    }
                }

                return true;
            }
        }
        else if (_instance._modeState is ManageBattleFlagModeState)
        {
            if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F1)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    _instance!._modeState = new MenuModeState
                    {
                        Config = new()
                        {
                            NavigateParams = new()
                            {
                                To = "/menu-mode/menu/battle",
                                Search = [],
                            }
                        }
                    };
                }

                return true;
            }
            else if ((PInvoke.User32.VirtualKey)inputHookClientNativeMessage.WParam == PInvoke.User32.VirtualKey.VK_F2)
            {
                if ((PInvoke.User32.WindowMessage)inputHookClientNativeMessage.Type == PInvoke.User32.WindowMessage.WM_KEYUP)
                {
                    if (_instance._playerEntityLastPosition != null)
                    {
                        _instance._battle.PlaceFlag(_instance._playerEntityLastPosition.Value);
                    }
                }

                return true;
            }
        }

        return false;
    }
}