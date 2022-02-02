﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ExtremeRoles.Module;
using ExtremeRoles.Module.RoleAbilityButton;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

using BepInEx.IL2CPP.Utils.Collections;

namespace ExtremeRoles.Roles.Solo.Neutral
{
    public class Missionary : SingleRoleBase, IRoleAbility, IRoleUpdate
    {

        public enum MissionaryOption
        {
            TellDeparture,
            DepartureMinTime,
            DepartureMaxTime,
            PropagateRange
        }

        public RoleAbilityButtonBase Button
        {
            get => this.propagate;
            set
            {
                this.propagate = value;
            }
        }

        public byte TargetPlayer = byte.MaxValue;

        private Queue<byte> lamb = new Queue<byte>();
        private float timer;

        private float propagateRange;
        private float minTimerTime;
        private float maxTimerTime;
        private bool tellDeparture;

        private TMPro.TextMeshPro tellText;

        private RoleAbilityButtonBase propagate;

        public Missionary() : base(
            ExtremeRoleId.Missionary,
            ExtremeRoleType.Neutral,
            ExtremeRoleId.Missionary.ToString(),
            ColorPalette.FanaticBlue,
            false, false, false, false)
        { }

        public override bool IsSameTeam(SingleRoleBase targetRole)
        {
            if (OptionHolder.Ship.IsSameNeutralSameWin)
            {
                return this.Id == targetRole.Id;
            }
            else
            {
                return (this.Id == targetRole.Id) && this.IsSameControlId(targetRole);
            }
        }

        protected override void CreateSpecificOption(
            CustomOptionBase parentOps)
        {
            CustomOption.Create(
                GetRoleOptionId((int)MissionaryOption.TellDeparture),
                string.Concat(
                    this.RoleName,
                    MissionaryOption.TellDeparture.ToString()),
                true, parentOps);
            CustomOption.Create(
                GetRoleOptionId((int)MissionaryOption.DepartureMinTime),
                string.Concat(
                    this.RoleName,
                    MissionaryOption.DepartureMinTime.ToString()),
                10f, 1.0f, 15f, 0.5f,
                parentOps, format: "unitSeconds");
            CustomOption.Create(
                GetRoleOptionId((int)MissionaryOption.DepartureMaxTime),
                string.Concat(
                    this.RoleName,
                    MissionaryOption.DepartureMaxTime.ToString()),
                30f, 15f, 60f, 0.5f,
                parentOps, format: "unitSeconds");
            CustomOption.Create(
                GetRoleOptionId((int)MissionaryOption.PropagateRange),
                string.Concat(
                    this.RoleName,
                    MissionaryOption.PropagateRange.ToString()),
                1.0f, 0.0f, 2.0f, 0.1f,
                parentOps);

            this.CreateCommonAbilityOption(parentOps);
        }

        protected override void RoleSpecificInit()
        {
            this.lamb.Clear();
            this.timer = 0;

            this.tellDeparture = OptionHolder.AllOption[
                GetRoleOptionId((int)MissionaryOption.TellDeparture)].GetValue();
            this.maxTimerTime = OptionHolder.AllOption[
                GetRoleOptionId((int)MissionaryOption.DepartureMaxTime)].GetValue();
            this.minTimerTime = OptionHolder.AllOption[
                GetRoleOptionId((int)MissionaryOption.DepartureMinTime)].GetValue();
            this.propagateRange = OptionHolder.AllOption[
                GetRoleOptionId((int)MissionaryOption.PropagateRange)].GetValue();

            resetTimer();
            this.RoleAbilityInit();

        }

        public void CreateAbility()
        {
            this.CreateNormalAbilityButton(
                Helper.Translation.GetString("propagate"),
                Loader.CreateSpriteFromResources(
                    Path.MissionaryPropagate, 115f));
        }

        public bool IsAbilityUse()
        {
            this.setTarget();
            return this.IsCommonUse() && this.TargetPlayer != byte.MaxValue;
        }

        public void RoleAbilityResetOnMeetingStart()
        {
            return;
        }

        public void RoleAbilityResetOnMeetingEnd()
        {
            return;
        }

        public void Update(PlayerControl rolePlayer)
        {
            if (ExtremeRolesPlugin.GameDataStore.AssassinMeetingTrigger) { return; }
            if (this.lamb.Count == 0) { return; }

            this.timer -= Time.deltaTime;
            if (this.timer > 0) { return; }

            resetTimer();

            byte targetPlayerId = this.lamb.Dequeue();
            PlayerControl targetPlayer = Helper.Player.GetPlayerControlById(targetPlayerId);

            RPCOperator.Call(
                rolePlayer.NetId,
                RPCOperator.Command.UncheckedMurderPlayer,
                new List<byte>
                {
                    targetPlayer.PlayerId,
                    targetPlayer.PlayerId,
                    byte.MaxValue
                });
            RPCOperator.UncheckedMurderPlayer(
                targetPlayer.PlayerId,
                targetPlayer.PlayerId,
                byte.MaxValue);

            RPCOperator.Call(
                rolePlayer.NetId,
                RPCOperator.Command.ReplaceDeadReason,
                new List<byte>
                {
                    targetPlayer.PlayerId,
                    (byte)GameDataContainer.PlayerStatus.Departure
                });
            ExtremeRolesPlugin.GameDataStore.ReplaceDeadReason(
                targetPlayer.PlayerId, GameDataContainer.PlayerStatus.Departure);
            if (this.tellDeparture)
            {
                rolePlayer.StartCoroutine(showText().WrapToIl2Cpp());
            }
        }

        public bool UseAbility()
        {
            var assassin = ExtremeRoleManager.GameRole[this.TargetPlayer] as Combination.Assassin;

            if (assassin != null)
            {
                if (!assassin.CanKilled)
                {
                    return false;
                }
                if (!assassin.CanKilledFromNeutral)
                {
                    return false;
                }
            }
            this.lamb.Enqueue(this.TargetPlayer);
            this.TargetPlayer = byte.MaxValue;
            return true;
        }

        private void resetTimer()
        {
            this.timer = Random.RandomRange(
                this.minTimerTime, this.maxTimerTime);
        }

        private void setTarget()
        {
            PlayerControl result = null;
            float num = this.propagateRange;
            this.TargetPlayer = byte.MaxValue;

            if (!ShipStatus.Instance)
            {
                return;
            }

            Vector2 truePosition = PlayerControl.LocalPlayer.GetTruePosition();

            Il2CppSystem.Collections.Generic.List<GameData.PlayerInfo> allPlayers = GameData.Instance.AllPlayers;
            for (int i = 0; i < allPlayers.Count; i++)
            {
                GameData.PlayerInfo playerInfo = allPlayers[i];

                if (!playerInfo.Disconnected &&
                    playerInfo.PlayerId != PlayerControl.LocalPlayer.PlayerId &&
                    !playerInfo.IsDead &&
                    !playerInfo.Object.inVent)
                {
                    PlayerControl @object = playerInfo.Object;
                    if (@object)
                    {
                        Vector2 vector = @object.GetTruePosition() - truePosition;
                        float magnitude = vector.magnitude;
                        if (magnitude <= num &&
                            !PhysicsHelpers.AnyNonTriggersBetween(
                                truePosition, vector.normalized,
                                magnitude, Constants.ShipAndObjectsMask))
                        {
                            result = @object;
                            num = magnitude;
                        }
                    }
                }
            }

            if (result)
            {
                if (this.IsSameTeam(ExtremeRoleManager.GameRole[result.PlayerId]))
                {
                    result = null;
                }
            }
            if (result != null)
            {
                if (!this.lamb.Contains(result.PlayerId))
                {
                    this.TargetPlayer = result.PlayerId;
                    Helper.Player.SetPlayerOutLine(result, this.NameColor);
                }
            }
        }
        private IEnumerator showText()
        {
            if (this.tellText == null)
            {
                this.tellText = Object.Instantiate(
                    Prefab.Text, Camera.main.transform, false);
                this.tellText.transform.localPosition = new Vector3(-4.0f, -2.75f, -250.0f);
                this.tellText.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                this.tellText.gameObject.layer = 5;
                this.tellText.text = Helper.Translation.GetString("departureText");
            }
            this.tellText.gameObject.SetActive(true);

            yield return new WaitForSeconds(3.5f);

            this.tellText.gameObject.SetActive(false);

        }
    }
}