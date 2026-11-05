using System;
using System.Numerics;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    readonly JetpackBooster _jetpackBooster = new();

    class JetpackBooster : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public JetpackBoosterState State { get; private set; }

        readonly InputHook1.Client _jetpackInputHookClient;

        struct JetpackState
        {
            public EP9JetpackContext.NativeHandle EP9JetpackContext;

            public int EP9JetpackContextAmountOfBursts;

            public float EP9JetpackContextFuelDepletionMultiplier;
        }

        JetpackState? _jetpackSavedState;

        bool _isTurboKeyPressed;

        bool _isFlyUpKeyPressed;

        bool _isFlyDownKeyPressed;

        public JetpackBooster()
        {
            IsDisposed = false;

            State = new JetpackBoosterState
            {
                IsEnabled = false,
                Config = new()
                {
                    IsUnlimitedFuelEnabled = true,
                    TurboSpeedMultiplier = 5
                }
            };

            _jetpackInputHookClient = new InputHook1.Client(
                0,
                (in InputHook1.NativeMessage jetpackInputHookNativeMessage) =>
                {
                    if (jetpackInputHookNativeMessage.WParam == (int)PInvoke.User32.VirtualKey.VK_SHIFT)
                    {
                        if (jetpackInputHookNativeMessage.Type == (int)PInvoke.User32.WindowMessage.WM_KEYDOWN)
                        {
                            _isTurboKeyPressed = true;
                        }
                        else if (jetpackInputHookNativeMessage.Type == (int)PInvoke.User32.WindowMessage.WM_KEYUP)
                        {
                            _isTurboKeyPressed = false;
                        }
                    }
                    else if (jetpackInputHookNativeMessage.WParam == (int)PInvoke.User32.VirtualKey.VK_C)
                    {
                        if (jetpackInputHookNativeMessage.Type == (int)PInvoke.User32.WindowMessage.WM_KEYDOWN)
                        {
                            _isFlyUpKeyPressed = true;
                        }
                        else if (jetpackInputHookNativeMessage.Type == (int)PInvoke.User32.WindowMessage.WM_KEYUP)
                        {
                            _isFlyUpKeyPressed = false;
                        }
                    }
                    else if (jetpackInputHookNativeMessage.WParam == (int)PInvoke.User32.VirtualKey.VK_X)
                    {
                        if (jetpackInputHookNativeMessage.Type == (int)PInvoke.User32.WindowMessage.WM_KEYDOWN)
                        {
                            _isFlyDownKeyPressed = true;
                        }
                        else if (jetpackInputHookNativeMessage.Type == (int)PInvoke.User32.WindowMessage.WM_KEYUP)
                        {
                            _isFlyDownKeyPressed = false;
                        }
                    }

                    return false;
                }
            );

            _jetpackSavedState = null;
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        void SaveJetpackState(EP9JetpackContext.NativeHandle ep9JetpackContext)
        {
            _jetpackSavedState = new JetpackState
            {
                EP9JetpackContext = ep9JetpackContext,
                EP9JetpackContextAmountOfBursts = ep9JetpackContext.AmountOfBursts,
                EP9JetpackContextFuelDepletionMultiplier = ep9JetpackContext.FuelDepletionMultiplier
            };
        }

        void RestoreJetpackState()
        {
            if (!_jetpackSavedState.HasValue)
            {
                return;
            }

            var ep9JetpackContext = _jetpackSavedState.Value.EP9JetpackContext;

            if (ep9JetpackContext.IsActive())
            {
                ep9JetpackContext.AmountOfBursts = _jetpackSavedState.Value.EP9JetpackContextAmountOfBursts;
                ep9JetpackContext.FuelDepletionMultiplier = _jetpackSavedState.Value.EP9JetpackContextFuelDepletionMultiplier;
            }

            _jetpackSavedState = null;
        }

        public void Update()
        {
            ThrowIfDisposed();

            RestoreJetpackState();

            if (State.IsEnabled)
            {
                var playerEntity = _instance!.FetchPlayerEntity();

                if (playerEntity != nint.Zero)
                {
                    var ep9JetpackContext = (EP9JetpackContext.NativeHandle)playerEntity.FindComponentByTypeNameRecursive(
                        EP9JetpackContext.Info.ApiClassName,
                        false
                    );

                    if (ep9JetpackContext != nint.Zero)
                    {
                        SaveJetpackState(ep9JetpackContext);

                        ep9JetpackContext.AmountOfBursts = 99999;
                        if (State.Config.IsUnlimitedFuelEnabled)
                        {
                            ep9JetpackContext.FuelDepletionMultiplier = 0f;
                        }

                        var horizontalVelocityX = BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(ep9JetpackContext.NativeDataRawPtr + 0x3e0)));
                        var horizontalVelocityY = BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(ep9JetpackContext.NativeDataRawPtr + 0x3e8)));

                        var horizontalVelocity = new Vector2(horizontalVelocityX, horizontalVelocityY);

                        if (_isTurboKeyPressed)
                        {
                            if (horizontalVelocity.Length() > 0.5f)
                            {
                                horizontalVelocity = Vector2.Normalize(horizontalVelocity) * State.Config.TurboSpeedMultiplier;
                                Marshal.WriteInt32(ep9JetpackContext.NativeDataRawPtr + 0x3e0, BitConverter.ToInt32(BitConverter.GetBytes(horizontalVelocity.X)));
                                Marshal.WriteInt32(ep9JetpackContext.NativeDataRawPtr + 0x3e8, BitConverter.ToInt32(BitConverter.GetBytes(horizontalVelocity.Y)));
                            }
                        }
                        else
                        {
                            if (horizontalVelocity.Length() > 1f)
                            {
                                horizontalVelocity = Vector2.Normalize(horizontalVelocity);
                                Marshal.WriteInt32(ep9JetpackContext.NativeDataRawPtr + 0x3e0, BitConverter.ToInt32(BitConverter.GetBytes(horizontalVelocity.X)));
                                Marshal.WriteInt32(ep9JetpackContext.NativeDataRawPtr + 0x3e8, BitConverter.ToInt32(BitConverter.GetBytes(horizontalVelocity.Y)));
                            }
                        }

                        if (_isFlyUpKeyPressed || _isFlyDownKeyPressed)
                        {
                            var verticalVelocity = BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(ep9JetpackContext.NativeDataRawPtr + 0x414)));

                            if (_isFlyUpKeyPressed)
                            {
                                verticalVelocity = Math.Min(2f, verticalVelocity + 0.1f);
                            }
                            else if (_isFlyDownKeyPressed)
                            {
                                verticalVelocity = Math.Max(-2f, verticalVelocity - 0.1f);
                            }

                            Marshal.WriteInt32(ep9JetpackContext.NativeDataRawPtr + 0x414, BitConverter.ToInt32(BitConverter.GetBytes(verticalVelocity)));
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            _jetpackInputHookClient.Dispose();

            RestoreJetpackState();

            IsDisposed = true;
        }
    }
}