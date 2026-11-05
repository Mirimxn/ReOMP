using System;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    readonly JumpBooster _jumpBooster = new();

    class JumpBooster : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public JumpBoosterState State { get; private set; }

        struct JumpState
        {
            public JumpContext.NativeHandle JumpContext;

            public float JumpContextJumpHeight;

            public DoubleJumpContext.NativeHandle DoubleJumpContext;

            public int DoubleJumpContextNumberOfDoubleJumps;

            public float DoubleJumpContextJumpHeight;
        }

        JumpState? _jumpSavedState;

        public JumpBooster()
        {
            IsDisposed = false;

            State = new JumpBoosterState
            {
                IsEnabled = false,
                Config = new()
                {
                    IsUnlimitedDoubleJumpsEnabled = true,
                    JumpHeightMultiplier = 2
                }
            };

            _jumpSavedState = null;
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        void SaveJumpState(JumpContext.NativeHandle jumpContext, DoubleJumpContext.NativeHandle doubleJumpContext)
        {
            _jumpSavedState = new JumpState
            {
                JumpContext = jumpContext,
                JumpContextJumpHeight = jumpContext != nint.Zero ? jumpContext.JumpData.JumpHeight : 0f,
                DoubleJumpContext = doubleJumpContext,
                DoubleJumpContextNumberOfDoubleJumps = doubleJumpContext != nint.Zero ? doubleJumpContext.NumberOfDoubleJumps : 0,
                DoubleJumpContextJumpHeight = doubleJumpContext != nint.Zero ? doubleJumpContext.DoubleJumpData.JumpHeight : 0f
            };
        }

        void RestoreJumpState()
        {
            if (!_jumpSavedState.HasValue)
            {
                return;
            }

            var jumpContext = _jumpSavedState.Value.JumpContext;
            var doubleJumpContext = _jumpSavedState.Value.DoubleJumpContext;

            if (jumpContext != nint.Zero && jumpContext.IsActive())
            {
                var jumpContextJumpData = jumpContext.JumpData;

                jumpContextJumpData.JumpHeight = _jumpSavedState.Value.JumpContextJumpHeight;
            }

            if (doubleJumpContext != nint.Zero && doubleJumpContext.IsActive())
            {
                doubleJumpContext.NumberOfDoubleJumps = _jumpSavedState.Value.DoubleJumpContextNumberOfDoubleJumps;

                var doubleJumpContextDoubleJumpData = doubleJumpContext.DoubleJumpData;

                doubleJumpContextDoubleJumpData.JumpHeight = _jumpSavedState.Value.DoubleJumpContextJumpHeight;
            }

            _jumpSavedState = null;
        }

        public void Update()
        {
            ThrowIfDisposed();

            RestoreJumpState();

            if (State.IsEnabled)
            {
                var playerEntity = _instance!.FetchPlayerEntity();

                if (playerEntity != nint.Zero)
                {
                    var jumpContext = (JumpContext.NativeHandle)playerEntity.FindComponentByTypeNameRecursive(
                        JumpContext.Info.ApiClassName,
                        false
                    );

                    var doubleJumpContext = (DoubleJumpContext.NativeHandle)playerEntity.FindComponentByTypeNameRecursive(
                        DoubleJumpContext.Info.ApiClassName,
                        false
                    );

                    SaveJumpState(jumpContext, doubleJumpContext);

                    if (jumpContext != nint.Zero)
                    {
                        var jumpContextJumpData = jumpContext.JumpData;

                        jumpContextJumpData.JumpHeight *= State.Config.JumpHeightMultiplier;
                    }

                    if (doubleJumpContext != nint.Zero)
                    {
                        if (State.Config.IsUnlimitedDoubleJumpsEnabled)
                        {
                            doubleJumpContext.NumberOfDoubleJumps = 0;
                        }

                        var doubleJumpContextDoubleJumpData = doubleJumpContext.DoubleJumpData;

                        doubleJumpContextDoubleJumpData.JumpHeight *= State.Config.JumpHeightMultiplier;
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

            RestoreJumpState();

            IsDisposed = true;
        }
    }
}