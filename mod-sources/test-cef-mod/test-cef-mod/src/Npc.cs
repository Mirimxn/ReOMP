using System;
using System.Numerics;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class TestCefMod
{
    class Npc : IDisposable
    {
        public bool IsDisposed { get; private set; }

        ApiWorld.NativeHandle _world;

        ApiEntity.NativeHandle _entity;

        ApiTransformComponent.NativeHandle _entityTransformComponent;

        RespawnContext.NativeHandle _entityRespawnContext;

        RespawnController.NativeHandle _entityRespawnController;

        AnimationRequestComponent.NativeHandle _entityAnimationRequestComponent;

        OpponentInfo.NativeHandle _entityOpponentInfo;

        OpponentChooser.NativeHandle _entityOpponentChooser;

        OpponentSelectorComponent.NativeHandle _entityOpponentSelectorComponent;

        HorizontalCharacterMover.NativeHandle _entityHorizontalCharacterMover;

        NavLinkCapabilitiesComponent.NativeHandle _entityNavLinkCapabilitiesComponent;

        NavRoute.NativeHandle _navRoute;

        public bool IsBattleParticipant { get; set; }

        Vector3? _lastBattleCenterPosition;

        public float MoveToBattleCenterBehaviorSpeed { get; set; }

        public Npc(ApiWorld.NativeHandle world, ApiEntity.NativeHandle prefab)
        {
            IsDisposed = false;

            _world = world;

            _entity = prefab.Clone();

            _entity.SetNoSerialise();
            _entity.SetParent(_world.GetSceneGraphRoot());

            _entityTransformComponent = (ApiTransformComponent.NativeHandle)_entity.FindComponentByTypeName(ApiTransformComponent.Info.ApiClassName);

            if (_entityTransformComponent == nint.Zero)
            {
                throw new InvalidOperationException();
            }

            _entityRespawnContext = (RespawnContext.NativeHandle)_entity.FindComponentByTypeNameRecursive(RespawnContext.Info.ApiClassName, false);

            if (_entityRespawnContext != nint.Zero)
            {
                _entityRespawnContext.Disable();
            }

            _entityRespawnController = (RespawnController.NativeHandle)_entity.FindComponentByTypeNameRecursive(RespawnController.Info.ApiClassName, false);

            _entityOpponentInfo = (OpponentInfo.NativeHandle)_entity.FindComponentByTypeNameRecursive(OpponentInfo.Info.ApiClassName, false);

            _entityOpponentChooser = (OpponentChooser.NativeHandle)_entity.FindComponentByTypeNameRecursive(OpponentChooser.Info.ApiClassName, false);

            _entityOpponentSelectorComponent = (OpponentSelectorComponent.NativeHandle)_entity.FindComponentByTypeNameRecursive(OpponentSelectorComponent.Info.ApiClassName, false);

            _entityHorizontalCharacterMover = (HorizontalCharacterMover.NativeHandle)_entity.FindComponentByTypeNameRecursive(HorizontalCharacterMover.Info.ApiClassName, false);

            _entityNavLinkCapabilitiesComponent = (NavLinkCapabilitiesComponent.NativeHandle)_entity.FindComponentByTypeNameRecursive(NavLinkCapabilitiesComponent.Info.ApiClassName, false);

            _entityAnimationRequestComponent = (AnimationRequestComponent.NativeHandle)_entity.FindComponentByTypeNameRecursive(AnimationRequestComponent.Info.ApiClassName, false);

            if (_entityAnimationRequestComponent != nint.Zero && _entityRespawnContext != nint.Zero)
            {
                _entityAnimationRequestComponent.RequestAnimation(_entityRespawnContext.RespawnAnim);
            }

            if (_entityNavLinkCapabilitiesComponent == nint.Zero)
            {
                _navRoute = (NavRoute.NativeHandle)nint.Zero;
            }
            else
            {
                _navRoute = NavRoute.Create();

                _navRoute.SetCapability(_entityNavLinkCapabilitiesComponent.Get());
                _navRoute.SetRadius(0.5f);
                _navRoute.SetDestinationRadius(0.5f);
            }

            IsBattleParticipant = false;

            _lastBattleCenterPosition = null;

            MoveToBattleCenterBehaviorSpeed = 1.0f;

            _npcs.Add(this);
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        public Vector3 FetchPosition()
        {
            ThrowIfDisposed();

            return _entityTransformComponent.PositionNativeData.ToVector3();
        }

        public void SetPosition(Vector3 position)
        {
            ThrowIfDisposed();

            _entityTransformComponent.PositionNativeData = position.ToVec3();
        }

        public string? FetchFactionName()
        {
            ThrowIfDisposed();

            if (_entityOpponentInfo == nint.Zero)
            {
                return null;
            }

            var factionType = _entityOpponentInfo.GetFaction();

            if (factionType == nint.Zero)
            {
                return null;
            }

            return factionType.FactionName;
        }

        public void SetFactionId(NpcFactionId factionId)
        {
            ThrowIfDisposed();

            if (_entityOpponentInfo == nint.Zero)
            {
                return;
            }

            var factionName = factionId switch
            {
                NpcFactionId.Empire => "Empire",
                NpcFactionId.Rebel => "RebelAlliance",
                NpcFactionId.Sith => "Sith",
                NpcFactionId.Resistance => "Resistance",
                _ => null
            };

            if (factionName == null)
            {
                return;
            }

            _entityOpponentInfo.ChangeFaction(factionName);
        }

        void UpdateMoveToBattleCenterBehavior()
        {
            if (!IsBattleParticipant)
            {
                return;
            }

            if (_battleConfig.CenterPosition == null)
            {
                return;
            }

            if (_entityOpponentSelectorComponent == nint.Zero || _entityOpponentSelectorComponent.CalculateBest() != nint.Zero)
            {
                return;
            }

            if (_entityHorizontalCharacterMover == nint.Zero)
            {
                return;
            }

            var aiSystem = ApiWorldSystemT__AISystem.GetFromCreate(_world);

            if (aiSystem == nint.Zero)
            {
                return;
            }

            var battleCenterPosition = _battleConfig.CenterPosition.Value;

            if (
                _lastBattleCenterPosition == null
                ||
                (_lastBattleCenterPosition.Value - battleCenterPosition).Length() > 0.1f)
            {
                var battleCenterPositionAsVec3 = battleCenterPosition.ToVec3();

                DestinationGoal.NativeHandle destinationGoal;

                unsafe
                {
                    destinationGoal = DestinationGoal.Create(&battleCenterPositionAsVec3);
                }

                _navRoute.SetGoal(destinationGoal);

                _lastBattleCenterPosition = battleCenterPosition;
            }

            var position = FetchPosition();

            var positionAsVec3 = position.ToVec3();

            unsafe
            {
                _navRoute.SetStart(&positionAsVec3);
            }

            _navRoute.Update(aiSystem);

            if (!_navRoute.HasPath())
            {
                return;
            }

            var moveToBattleCenterBehaviorDirection = _navRoute.GetCurrentHeading().ToVector3();

            if (
                MathF.Abs(moveToBattleCenterBehaviorDirection.X)
                +
                MathF.Abs(moveToBattleCenterBehaviorDirection.Z)
                <
                MathF.Abs(moveToBattleCenterBehaviorDirection.Y)
            )
            {
                // TODO: Add jumping
                return;
            }

            var moveToBattleCenterBehaviorVelocity = new Vector3
            {
                X = moveToBattleCenterBehaviorDirection.X,
                Y = 0f,
                Z = moveToBattleCenterBehaviorDirection.Z,
            };

            moveToBattleCenterBehaviorVelocity =
                Vector3.Normalize(moveToBattleCenterBehaviorVelocity)
                *
                MoveToBattleCenterBehaviorSpeed;

            var moveToBattleCenterBehaviorVelocityAsVec3 = moveToBattleCenterBehaviorVelocity.ToVec3();

            unsafe
            {
                _entityHorizontalCharacterMover.SetMoveLaneVelocity(&moveToBattleCenterBehaviorVelocityAsVec3);
            }

            var moveToBattleCenterBehaviorAngle = MathF.Atan2(
                moveToBattleCenterBehaviorDirection.Z,
                moveToBattleCenterBehaviorDirection.X
            ) * 180f / MathF.PI;

            _entityTransformComponent.SetRotation(0f, 270f - moveToBattleCenterBehaviorAngle, 0f);
        }

        public void Update()
        {
            ThrowIfDisposed();

            if (!_entity.IsActive())
            {
                Dispose();
                return;
            }

            UpdateMoveToBattleCenterBehavior();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            _npcs.Remove(this);

            if (_entity.IsActive())
            {
                _entity.DeferredDelete();
            }

            IsDisposed = true;
        }
    }
}