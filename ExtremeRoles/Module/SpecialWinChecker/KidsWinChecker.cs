﻿using System.Collections.Generic;

using ExtremeRoles.Helper;
using ExtremeRoles.Module.Interface;
using ExtremeRoles.Roles;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.Combination;

namespace ExtremeRoles.Module.SpecialWinChecker
{
    internal sealed class KidsWinChecker : IWinChecker
    {
        public RoleGameOverReason Reason => RoleGameOverReason.KidsTooBigHomeAlone;

        private Dictionary<byte, Delinquent> aliveDelinquent = new Dictionary<byte, Delinquent>();

        public KidsWinChecker()
        {
            aliveDelinquent.Clear();
        }

        public void AddAliveRole(
            byte playerId, SingleRoleBase role)
        {
            aliveDelinquent.Add(playerId, (Delinquent)role);
        }

        public bool IsWin(
            ExtremeShipStatus.ExtremeShipStatus.PlayerStatistics statistics)
        {
            byte checkPlayerId = byte.MaxValue;
            float range = float.MinValue;
            Delinquent checkRole = null;
            foreach (var (playerId, role) in aliveDelinquent)
            {
                if (role.WinCheckEnable)
                {
                    checkPlayerId = playerId;
                    range = role.Range;
                    checkRole = role;
                    break;
                }
            }
            
            if (checkPlayerId == byte.MaxValue) { return false; }

            PlayerControl player = Helper.Player.GetPlayerControlById(checkPlayerId);
            if (player == null) { return false; }

            List<PlayerControl> rangeInPlayer = Helper.Player.GetAllPlayerInRange(
                player, checkRole, range);
            int teamAlive = statistics.SeparatedNeutralAlive[
                (NeutralSeparateTeam.Kids, checkRole.GameControlId)];
            int allAlive = statistics.TotalAlive;

            bool isWin = (allAlive - rangeInPlayer.Count - teamAlive) <= 0;

            foreach (PlayerControl target in rangeInPlayer)
            {
                byte targetId = target.PlayerId;
                Player.RpcUncheckMurderPlayer(
                    checkPlayerId, targetId, byte.MinValue);
                ExtremeRolesPlugin.ShipState.RpcReplaceDeadReason(
                    targetId, ExtremeShipStatus.ExtremeShipStatus.PlayerStatus.Explosion);
            }

            Player.RpcUncheckMurderPlayer(
                checkPlayerId, checkPlayerId, byte.MinValue);
            ExtremeRolesPlugin.ShipState.RpcReplaceDeadReason(
                checkPlayerId, ExtremeShipStatus.ExtremeShipStatus.PlayerStatus.Explosion);

            return isWin;
        }
    }
}