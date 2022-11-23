﻿using System.Collections.Generic;
using UnityEngine;

using ExtremeRoles.Module;

namespace ExtremeRoles.Roles.API
{

    public enum CombinationRoleCommonOption
    {
        IsMultiAssign = 30,
        AssignsNum,
        IsAssignImposter,
        ImposterSelectedRate,
    }

    public abstract class CombinationRoleManagerBase : RoleOptionBase
    {
        public List<MultiAssignRoleBase> Roles = new List<MultiAssignRoleBase>();

        protected Color optionColor;
        protected string roleName = "";
        internal CombinationRoleManagerBase(
            string roleName,
            Color optionColor)
        {
            this.optionColor = optionColor;
            this.roleName = roleName;
        }

        public abstract void AssignSetUpInit(int curImpNum);

        public abstract MultiAssignRoleBase GetRole(
            int roleId, RoleTypes playerRoleType);

        protected override void CreateKillerOption(
            IOption parentOps)
        {
            // 複数ロールの中に殺戮者がいる可能性がため、管理ロールで殺戮者の設定はしない
            return;
        }

        protected override void CreateVisonOption(
            IOption parentOps)
        {
            // 複数のロールがまとまっているため、管理ロールで視界の設定はしない
            return;
        }
        protected override void RoleSpecificInit()
        {
            // 複数のロールがまとまっているため、設定はしない
            return;
        }

    }
}