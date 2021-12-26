﻿using System;
using System.Collections.Generic;
using System.Linq;

using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;

using ExtremeRoles.Roles.Solo.Crewmate;
using ExtremeRoles.Roles.Solo.Neutral;
using ExtremeRoles.Roles.Solo.Impostor;


namespace ExtremeRoles.Roles
{
    public enum ExtremeRoleId
    {
        Null = -100,
        VanillaRole = 50,
        Assassin,
        Marlin,
        Lover,

        Jackal,
        Sidekick,
        
        SpecialCrew,

        SpecialImpostor,

        Alice,
    }
    public enum RoleGameOverReason
    {
        AssassinationMarin = 10,
        AliceKilledByImposter,
        AliceKillAllOthers,
        JackalKillAllOthers,

        UnKnown = 100,
    }


    public static class ExtremeRoleManager
    {
        public const int OptionOffsetPerRole = 50;

        public static readonly List<
            SingleRoleBase> NormalRole = new List<SingleRoleBase>()
            {

                new SpecialCrew(),

                new SpecialImpostor(),

                new Alice(),
                new Jackal(),
            };
        
        public static readonly List<
            CombinationRoleManagerBase> CombRole = new List<CombinationRoleManagerBase>()
            {
                new Combination.Avalon(),
            };

        public static Dictionary<
            byte, SingleRoleBase> GameRole = new Dictionary<byte, SingleRoleBase> ();

        private static int roleControlId = 0;

        public enum ReplaceOperation
        {
            ForceReplaceToSidekick = 0,
            SidekickToJackal
        }

        public static void CreateCombinationRoleOptions(
            int optionIdOffsetChord)
        {
            createOptions(optionIdOffsetChord, CombRole);
        }

        public static void CreateNormalRoleOptions(
            int optionIdOffsetChord)
        {
            createOptions(optionIdOffsetChord, NormalRole);
        }

        public static void GameInit()
        {
            roleControlId = 0;
            GameRole.Clear();
            foreach (var role in CombRole)
            {
                role.GameInit();
            }
        }

        public static SingleRoleBase GetLocalPlayerRole()
        {
            return GameRole[PlayerControl.LocalPlayer.PlayerId];
        }

        public static void SetPlayerIdToMultiRoleId(
            byte roleId, byte playerId, byte id)
        {

            RoleTypes roleType = Helper.Player.GetPlayerControlById(playerId).Data.Role.Role;
            bool hasVanilaRole = roleType != RoleTypes.Crewmate || roleType != RoleTypes.Impostor;

            foreach (var combRole in CombRole)
            {
                foreach (var role in combRole.Roles)
                {
                    if (role.BytedRoleId == roleId)
                    {

                        SingleRoleBase addRole = role.Clone();

                        IRoleAbility abilityRole = addRole as IRoleAbility;

                        if (abilityRole != null && PlayerControl.LocalPlayer.PlayerId == playerId)
                        {
                            Helper.Logging.Debug("Try Create Ability NOW!!!");
                            abilityRole.CreateAbility();
                        }

                        addRole.GameInit();
                        addControlId(addRole);
                        ((MultiAssignRoleBase)addRole).CombinationId = id;

                        GameRole.Add(
                            playerId, addRole);

                        if (hasVanilaRole)
                        {
                            ((MultiAssignRoleBase)GameRole[
                                playerId]).SetAnotherRole(
                                    new Solo.VanillaRoleWrapper(roleType));
                        }
                        Helper.Logging.Debug($"PlayerId:{playerId}   AssignTo:{addRole.RoleName}");
                    }
                }
            }
        }
        public static void SetPlyerIdToSingleRoleId(
            byte roleId, byte playerId)
        {
            foreach (RoleTypes vanilaRole in Enum.GetValues(
                typeof(RoleTypes)))
            {
                if ((byte)vanilaRole == roleId)
                {
                    setPlyerIdToSingleRole(
                        playerId, new Solo.VanillaRoleWrapper(vanilaRole));
                    return;
                }
            }

            foreach (var role in NormalRole)
            {
                if (role.BytedRoleId == roleId)
                {
                    setPlyerIdToSingleRole(playerId, role);
                }
            }
        }

        public static void RoleReplace(
            byte caller, byte targetId, ReplaceOperation ops)
        {
            switch(ops)
            {
                case ReplaceOperation.ForceReplaceToSidekick:
                    Jackal.TargetToSideKick(caller, targetId);
                    break;
                case ReplaceOperation.SidekickToJackal:
                    Sidekick.BecomeToJackal(caller, targetId);
                    break;
                default:
                    break;
            }
        }

        private static void createOptions(
            int optionIdOffsetChord,
            IEnumerable<RoleOptionBase> roles)
        {
            if (roles.Count() == 0) { return; };

            int roleOptionOffset = 0;

            foreach (var item
             in roles.Select((Value, Index) => new { Value, Index }))
            {
                roleOptionOffset = optionIdOffsetChord + (OptionOffsetPerRole * item.Index);
                item.Value.CreateRoleAllOption(roleOptionOffset);
            }
        }

        private static void setPlyerIdToSingleRole(
            byte playerId, SingleRoleBase role)
        {

            SingleRoleBase addRole = role.Clone();


            IRoleAbility abilityRole = addRole as IRoleAbility;

            if (abilityRole != null && PlayerControl.LocalPlayer.PlayerId == playerId)
            {
                Helper.Logging.Debug("Try Create Ability NOW!!!");
                abilityRole.CreateAbility();
            }

            addRole.GameInit();
            addControlId(addRole);

            if (!GameRole.ContainsKey(playerId))
            {
                GameRole.Add(
                    playerId, addRole);

            }
            else
            {
                ((MultiAssignRoleBase)GameRole[
                    playerId]).SetAnotherRole(addRole);
            }
            Helper.Logging.Debug($"PlayerId:{playerId}   AssignTo:{addRole.RoleName}");
        }
        
        private static void addControlId(SingleRoleBase role)
        {
            role.GameControlId = roleControlId;
            roleControlId = roleControlId + 1;
        }

    }
}
