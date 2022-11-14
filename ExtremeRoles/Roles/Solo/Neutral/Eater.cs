﻿using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Roles.API.Extension.Neutral;
using ExtremeRoles.Performance;
using ExtremeRoles.Resources;

using ExtremeRoles.Module.AbilityButton.Roles;

using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace ExtremeRoles.Roles.Solo.Neutral
{
    public sealed class Eater : SingleRoleBase, IRoleAbility, IRoleMurderPlayerHook, IRoleUpdate
    {
        public sealed class EaterAbilityButton : RoleAbilityButtonBase
        {
            public int CurAbilityNum
            {
                get => this.abilityNum;
            }

            public float KillEatTime
            {
                get => this.killEatTime;
            }
            public float CurButtonCoolTime
            {
                get => this.CoolTime;
            }

            private int abilityNum = 0;
            private bool isKillEatMode;
            private float killEatTime;

            private string deadBodyEatString;
            private string killEatString;
            private Sprite deadBodyEatSprite;
            private Sprite killEatSprite;

            private TMPro.TextMeshPro abilityCountText = null;

            public EaterAbilityButton(
                Func<bool> ability,
                Func<bool> canUse,
                Sprite deadBodyEatSprite,
                Sprite killEatSprite,
                Vector3 positionOffset,
                Action abilityCleanUp,
                Func<bool> abilityCheck,
                int winAbilityNum,
                float killEatTime,
                KeyCode hotkey = KeyCode.F,
                bool mirror = false) : base(
                    "",
                    ability,
                    canUse,
                    deadBodyEatSprite,
                    positionOffset,
                    abilityCleanUp,
                    abilityCheck,
                    hotkey, mirror)
            {
                this.abilityCountText = GameObject.Instantiate(
                    this.Button.cooldownTimerText,
                    this.Button.cooldownTimerText.transform.parent);
                updateAbilityCountText();
                this.abilityCountText.enableWordWrapping = false;
                this.abilityCountText.transform.localScale = Vector3.one * 0.5f;
                this.abilityCountText.transform.localPosition += new Vector3(-0.05f, 0.65f, 0);

                this.deadBodyEatString = Translation.GetString("deadBodyEat");
                this.killEatString = Translation.GetString("eatKill");
                this.ButtonText = this.deadBodyEatString;

                this.deadBodyEatSprite = deadBodyEatSprite;
                this.killEatSprite = killEatSprite;

                this.killEatTime = killEatTime;

                this.isKillEatMode = false;
                this.abilityNum = winAbilityNum;
            }

            public void UpdateAbilityCount(int newCount)
            {
                this.abilityNum = newCount;
                this.updateAbilityCountText();
            }

            public void SetKillEatMode(bool isActive)
            {
                this.isKillEatMode = isActive;
                if (this.isKillEatMode)
                {
                    this.ButtonSprite = this.killEatSprite;
                    this.ButtonText = this.killEatString;
                    this.AbilityActiveTime = this.killEatTime;
                }
                else
                {
                    this.ButtonSprite = this.deadBodyEatSprite;
                    this.ButtonText = this.deadBodyEatString;
                    this.AbilityActiveTime = 0.1f;
                }
            }

            public void SetKillEatTime(float newTime)
            {
                this.killEatTime = newTime;
            }

            protected override void AbilityButtonUpdate()
            {
                if (this.CanUse() && this.abilityNum > 0)
                {
                    this.Button.graphic.color = this.Button.buttonLabelText.color = Palette.EnabledColor;
                    this.Button.graphic.material.SetFloat("_Desat", 0f);
                }
                else
                {
                    this.Button.graphic.color = this.Button.buttonLabelText.color = Palette.DisabledClear;
                    this.Button.graphic.material.SetFloat("_Desat", 1f);
                }
                if (this.abilityNum == 0)
                {
                    Button.SetCoolDown(0, this.CoolTime);
                    return;
                }

                if (this.Timer >= 0)
                {
                    bool abilityOn = this.IsHasCleanUp() && IsAbilityOn;

                    if (abilityOn || (
                            !CachedPlayerControl.LocalPlayer.PlayerControl.inVent &&
                            CachedPlayerControl.LocalPlayer.PlayerControl.moveable))
                    {
                        this.Timer -= Time.deltaTime;
                    }
                    if (abilityOn)
                    {
                        if (!this.AbilityCheck())
                        {
                            this.Timer = 0;
                            this.IsAbilityOn = false;
                        }
                    }
                }

                if (this.Timer <= 0 && this.IsHasCleanUp() && IsAbilityOn)
                {
                    this.IsAbilityOn = false;
                    this.Button.cooldownTimerText.color = Palette.EnabledColor;
                    this.CleanUp();
                    this.reduceAbilityCount();
                    this.ResetCoolTimer();
                }

                if (this.abilityNum > 0)
                {
                    Button.SetCoolDown(
                        this.Timer,
                        (this.IsHasCleanUp() && this.IsAbilityOn) ? this.AbilityActiveTime : this.CoolTime);
                    this.updateAbilityCountText();
                }
            }

            protected override void OnClickEvent()
            {
                if (this.CanUse() &&
                    this.Timer < 0f &&
                    this.abilityNum > 0 &&
                    !this.IsAbilityOn)
                {
                    Button.graphic.color = this.DisableColor;

                    if (this.UseAbility())
                    {
                        if (this.IsHasCleanUp())
                        {
                            this.Timer = this.AbilityActiveTime;
                            Button.cooldownTimerText.color = this.TimerOnColor;
                            this.IsAbilityOn = true;
                        }
                        else
                        {
                            this.reduceAbilityCount();
                            this.ResetCoolTimer();
                        }
                    }
                }
            }

            private void reduceAbilityCount()
            {
                this.abilityNum = this.abilityNum - 1;
                updateAbilityCountText();
            }

            private void updateAbilityCountText()
            {
                if (this.abilityCountText == null) { return; }

                this.abilityCountText.text = string.Format(
                    Translation.GetString("eaterWinNum"),
                        this.abilityNum);
            }
        }

        public enum EaterOption
        {
            CanUseVent,
            EatRange,
            DeadBodyEatActiveCoolTimePenalty,
            KillEatCoolTimePenalty,
            KillEatActiveCoolTimeReduceRate,
            IsResetCoolTimeWhenMeeting,
            IsShowArrowForDeadBody
        }

        public RoleAbilityButtonBase Button
        { 
            get => this.eatButton;
            set
            {
                this.eatButton = value;
            }
        }

        private RoleAbilityButtonBase eatButton;
        private PlayerControl tmpTarget;
        private PlayerControl targetPlayer;
        private GameData.PlayerInfo targetDeadBody;
        
        private float range;
        private float deadBodyEatActiveCoolTimePenalty;
        private float killEatCoolTimePenalty;
        private float killEatActiveCoolTimeReduceRate;

        private float defaultCoolTime;
        private bool isResetCoolTimeWhenMeeting;
        private bool isShowArrow;
        private bool isActivated;
        private Dictionary<byte, Arrow> deadBodyArrow;

        public Eater() : base(
           ExtremeRoleId.Eater,
           ExtremeRoleType.Neutral,
           ExtremeRoleId.Eater.ToString(),
           ColorPalette.EaterMaroon,
           false, false, false, false)
        { }

        public void CreateAbility()
        {
            var allOpt = OptionHolder.AllOption;

            int abilityNum = (int)allOpt[GetRoleOptionId(
                RoleAbilityCommonOption.AbilityCount)].GetValue();
            int halfPlayerNum = GameData.Instance.AllPlayers.Count / 2;

            this.Button = new EaterAbilityButton(
                UseAbility,
                IsAbilityUse,
                Loader.CreateSpriteFromResources(
                    Path.EaterDeadBodyEat),
                Loader.CreateSpriteFromResources(
                    Path.EaterEatKill),
                new Vector3(-1.8f, -0.06f, 0),
                CleanUp,
                IsAbilityCheck,
                halfPlayerNum < abilityNum ? halfPlayerNum : abilityNum,
                (float)allOpt[GetRoleOptionId(
                    RoleAbilityCommonOption.AbilityActiveTime)].GetValue());

            abilityInit();
        }

        public void HookMuderPlayer(
            PlayerControl source, PlayerControl target)
        {
            if (MeetingHud.Instance || 
                source.PlayerId == CachedPlayerControl.LocalPlayer.PlayerId ||
                !this.isShowArrow) { return; }

            DeadBody[] array = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            for (int i = 0; i < array.Length; ++i)
            {
                if (GameData.Instance.GetPlayerById(array[i].ParentId).PlayerId == target.PlayerId)
                {
                    Arrow arr = new Arrow(this.NameColor);
                    arr.UpdateTarget(array[i].transform.position);

                    this.deadBodyArrow.Add(target.PlayerId, arr);
                    break;
                }
            }
        }

        public bool IsAbilityUse()
        {

            this.tmpTarget = Player.GetClosestPlayerInRange(
                CachedPlayerControl.LocalPlayer, this, this.range);

            this.targetDeadBody = Player.GetDeadBodyInfo(
                this.range);

            if (this.eatButton == null) { return false; }

            bool hasPlayerTarget = this.tmpTarget != null;
            bool hasDedBodyTarget = this.targetDeadBody != null;

            ((EaterAbilityButton)this.eatButton).SetKillEatMode(
                !hasDedBodyTarget && hasPlayerTarget);

            return this.IsCommonUse() && 
                (hasPlayerTarget || hasDedBodyTarget);
        }

        public void RoleAbilityResetOnMeetingEnd()
        {
            if (this.eatButton != null)
            {
                if (isResetCoolTimeWhenMeeting)
                {
                    this.eatButton.SetAbilityCoolTime(this.defaultCoolTime);
                    this.eatButton.ResetCoolTimer();
                }
                if (!this.isActivated)
                {
                    EaterAbilityButton eaterButton = (EaterAbilityButton)this.eatButton;
                    eaterButton.SetKillEatTime(
                        eaterButton.KillEatTime * this.killEatActiveCoolTimeReduceRate);
                }
            }
            this.isActivated = false;
        }

        public void RoleAbilityResetOnMeetingStart()
        {
            foreach (Arrow arrow in this.deadBodyArrow.Values)
            {
                arrow.Clear();
            }
            this.deadBodyArrow.Clear();
        }

        public bool UseAbility()
        {
            this.targetPlayer = this.tmpTarget;
            return true;
        }

        public void Update(PlayerControl rolePlayer)
        {

            if (CachedShipStatus.Instance == null ||
                GameData.Instance == null ||
                this.IsWin) { return; }
            if (!CachedShipStatus.Instance.enabled ||
                ExtremeRolesPlugin.ShipState.AssassinMeetingTrigger) { return; }

            DeadBody[] array = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            HashSet<byte> existDeadBodyPlayerId = new HashSet<byte>();
            for (int i = 0; i < array.Length; ++i)
            {
                byte playerId = GameData.Instance.GetPlayerById(array[i].ParentId).PlayerId;

                if (this.deadBodyArrow.TryGetValue(playerId, out Arrow arrow))
                {
                    arrow.Update();
                    existDeadBodyPlayerId.Add(playerId);
                }
            }

            HashSet<byte> removePlayerId = new HashSet<byte>();
            foreach (byte playerId in this.deadBodyArrow.Keys)
            {
                if (!existDeadBodyPlayerId.Contains(playerId))
                {
                    removePlayerId.Add(playerId);
                }
            }

            foreach (byte playerId in removePlayerId)
            {
                this.deadBodyArrow[playerId].Clear();
                this.deadBodyArrow.Remove(playerId);
            }


            EaterAbilityButton eaterButton = (EaterAbilityButton)this.eatButton;

            if (eaterButton.CurAbilityNum != 0) { return; }

            ExtremeRolesPlugin.ShipState.RpcRoleIsWin(rolePlayer.PlayerId);
            this.IsWin = true;
        }

        public void CleanUp()
        {
            if (this.targetDeadBody != null)
            {
                Player.RpcCleanDeadBody(this.targetDeadBody.PlayerId);

                if (this.deadBodyArrow.ContainsKey(this.targetDeadBody.PlayerId))
                {
                    this.deadBodyArrow[this.targetDeadBody.PlayerId].Clear();
                    this.deadBodyArrow.Remove(this.targetDeadBody.PlayerId);
                }

                this.targetDeadBody = null;

                if (this.eatButton == null) { return; }

                EaterAbilityButton eaterButton = (EaterAbilityButton)this.eatButton;
                eaterButton.SetKillEatTime(
                    eaterButton.KillEatTime * this.deadBodyEatActiveCoolTimePenalty);
            }
            else
            {

                Player.RpcUncheckMurderPlayer(
                    CachedPlayerControl.LocalPlayer.PlayerId,
                    this.targetPlayer.PlayerId, 0);

                ExtremeRolesPlugin.ShipState.RpcReplaceDeadReason(
                    this.targetPlayer.PlayerId,
                    Module.ExtremeShipStatus.ExtremeShipStatus.PlayerStatus.Eatting);

                if (!this.targetPlayer.Data.IsDead) { return; }

                FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(
                    this.cleanDeadBodyOps(
                        this.targetPlayer.PlayerId).WrapToIl2Cpp());
            }
            this.isActivated = true;
        }

        public bool IsAbilityCheck()
        {
            if (this.targetDeadBody != null) { return true; }
            
            PlayerControl checkPlayer = Player.GetClosestPlayerInRange(
                CachedPlayerControl.LocalPlayer, this, this.range);

            if (checkPlayer == null) { return false; }

            return checkPlayer.PlayerId == this.targetPlayer.PlayerId;
        }

        public override bool IsSameTeam(SingleRoleBase targetRole) =>
            this.IsNeutralSameTeam(targetRole);

        protected override void CreateSpecificOption(
            IOption parentOps)
        {

            CreateBoolOption(
                EaterOption.CanUseVent,
                true, parentOps);
            CreateFloatOption(
                RoleAbilityCommonOption.AbilityCoolTime,
                25.0f, 2.0f, 60.0f, 0.5f,
                parentOps, format: OptionUnit.Second);
            CreateFloatOption(
                RoleAbilityCommonOption.AbilityActiveTime,
                7.5f, 2.5f, 15.0f, 0.5f,
                parentOps, format: OptionUnit.Second);
            CreateIntOption(
                RoleAbilityCommonOption.AbilityCount,
                5, 1, 7, 1, parentOps,
                format: OptionUnit.Shot);
            CreateFloatOption(
                EaterOption.EatRange,
                1.0f, 0.0f, 2.0f, 0.1f,
                parentOps);
            CreateIntOption(
                EaterOption.DeadBodyEatActiveCoolTimePenalty,
                10, 0, 25, 1, parentOps,
                format: OptionUnit.Percentage);
            CreateIntOption(
                EaterOption.KillEatCoolTimePenalty,
                10, 0, 25, 1, parentOps,
                format: OptionUnit.Percentage);
            CreateIntOption(
                EaterOption.KillEatActiveCoolTimeReduceRate,
                10, 0, 50, 1, parentOps,
                format: OptionUnit.Percentage);
            CreateBoolOption(
                EaterOption.IsResetCoolTimeWhenMeeting,
                false, parentOps);
            CreateBoolOption(
                EaterOption.IsShowArrowForDeadBody,
                true, parentOps);
        }

        protected override void RoleSpecificInit()
        {
            this.targetDeadBody = null;
            this.targetPlayer = null;

            var allOps = OptionHolder.AllOption;

            this.UseVent = allOps[
                GetRoleOptionId(EaterOption.CanUseVent)].GetValue();
            this.range = allOps[
                GetRoleOptionId(EaterOption.EatRange)].GetValue();
            this.deadBodyEatActiveCoolTimePenalty = (float)allOps[
               GetRoleOptionId(EaterOption.DeadBodyEatActiveCoolTimePenalty)].GetValue() / 100.0f + 1.0f;
            this.killEatCoolTimePenalty = (float)allOps[
               GetRoleOptionId(EaterOption.KillEatCoolTimePenalty)].GetValue() / 100.0f + 1.0f;
            this.killEatActiveCoolTimeReduceRate = 1.0f - (float)allOps[
               GetRoleOptionId(EaterOption.KillEatCoolTimePenalty)].GetValue() / 100.0f;
            this.isResetCoolTimeWhenMeeting = allOps[
               GetRoleOptionId(EaterOption.IsResetCoolTimeWhenMeeting)].GetValue();
            this.isShowArrow = allOps[
               GetRoleOptionId(EaterOption.IsShowArrowForDeadBody)].GetValue();

            this.deadBodyArrow = new Dictionary<byte, Arrow>();
            this.isActivated = false;

            this.abilityInit();
        }

        private void abilityInit()
        {
            if (this.Button == null) { return; }

            var allOps = OptionHolder.AllOption;

            this.defaultCoolTime = allOps[
                GetRoleOptionId(RoleAbilityCommonOption.AbilityCoolTime)].GetValue();
            
            this.Button.SetAbilityCoolTime(this.defaultCoolTime);
            this.Button.ResetCoolTimer();
        }

        private IEnumerator cleanDeadBodyOps(byte targetPlayerId)
        {
            DeadBody checkDeadBody = null;

            DeadBody[] array = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            for (int i = 0; i < array.Length; ++i)
            {
                if (GameData.Instance.GetPlayerById(
                    array[i].ParentId).PlayerId == targetPlayerId)
                {
                    checkDeadBody = array[i];
                    break;
                }
            }

            if (checkDeadBody == null) { yield break; }

            while(!checkDeadBody.enabled)
            {
                yield return null;
            }

            Player.RpcCleanDeadBody(targetPlayerId);
            
            this.targetPlayer = null;

            if (this.eatButton == null) { yield break; }

            EaterAbilityButton eaterButton = (EaterAbilityButton)this.eatButton;

            eaterButton.SetAbilityCoolTime(
                eaterButton.CurButtonCoolTime * this.killEatCoolTimePenalty);
        }

    }
}
