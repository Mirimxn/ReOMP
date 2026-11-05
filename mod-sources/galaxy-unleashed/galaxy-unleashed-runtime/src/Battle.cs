using System;
using System.Numerics;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    readonly Battle _battle = new();

    class Battle : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public BattleState State { get; private set; }

        public BattleFlag? Flag { get; private set; }

        public Battle()
        {
            IsDisposed = false;

            State = new BattleState
            {
                IsActive = false
            };

            Flag = null;
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        public void PlaceFlag(Vector3 flagPosition)
        {
            ThrowIfDisposed();

            if (Flag != null && !Flag.IsDisposed)
            {
                Flag.Dispose();
            }

            Flag = new BattleFlag(flagPosition);
        }

        public void Update()
        {
            ThrowIfDisposed();

            if (Flag != null && Flag.IsDisposed)
            {
                Flag = null;
            }

            Flag?.Update();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Flag?.Dispose();

            IsDisposed = true;
        }
    }
}