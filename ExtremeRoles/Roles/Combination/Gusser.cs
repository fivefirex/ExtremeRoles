﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using ExtremeRoles.Helper;
using ExtremeRoles.Module;
using ExtremeRoles.Module.CustomMonoBehaviour;
using ExtremeRoles.Resources;
using ExtremeRoles.Roles.API;
using ExtremeRoles.Roles.API.Interface;
using ExtremeRoles.Performance;

namespace ExtremeRoles.Roles.Combination
{
    public sealed class GuesserManager : FlexibleCombinationRoleManagerBase
    {
        public GuesserManager() : base(new Guesser(), 1)
        { }

    }

    public sealed class Guesser : 
        MultiAssignRoleBase, 
        IRoleSpecialSetUp,
        IRoleMeetingButtonAbility
    {
        private bool isEvil = false;
        private int bulletNum;
        private int maxGuessNum;
        private int curGuessNum;

        private GameObject uiPrefab;
        private GuesserUi guesserUi;

        private static HashSet<ExtremeRoleId> alwaysMissRole = new HashSet<ExtremeRoleId>()
        {
            ExtremeRoleId.Assassin,
            ExtremeRoleId.Marlin,
            ExtremeRoleId.Villain
        };

        public Guesser(
            ) : base(
                ExtremeRoleId.Guesser,
                ExtremeRoleType.Crewmate,
                ExtremeRoleId.Guesser.ToString(),
                ColorPalette.SupporterGreen,
                false, true, false, false,
                tab: OptionTab.Combination)
        { }

        private static List<GuessBehaviour.RoleInfo> createRoleInfo()
        {
         
            List<GuessBehaviour.RoleInfo> result = new List<GuessBehaviour.RoleInfo>();

            Dictionary<ExtremeRoleType, List<ExtremeRoleId>> separetedRoleId = new Dictionary<ExtremeRoleType, List<ExtremeRoleId>>()
            {
                {ExtremeRoleType.Crewmate, new List<ExtremeRoleId>() },
                {ExtremeRoleType.Impostor, new List<ExtremeRoleId>() },
                {ExtremeRoleType.Neutral , new List<ExtremeRoleId>() },
            };

            bool queenOn = false;
            bool jackalOn = false;
            bool jackalForceReplaceLover = false;

            var allOption = OptionHolder.AllOption;

            void Add(
                ExtremeRoleId id,
                ExtremeRoleType team,
                ExtremeRoleId another = ExtremeRoleId.Null)
            {
                result.Add(
                    new GuessBehaviour.RoleInfo()
                    {
                        Id = id,
                        AnothorId = another,
                        Team = team,
                    });
            }
            void ListAdd(ExtremeRoleId baseId, ExtremeRoleType team, List<ExtremeRoleId> list)
            {
                foreach (var roleId in list)
                {
                    Add(baseId, team, roleId);
                }
            }

            foreach (var (id, role) in ExtremeRoleManager.NormalRole)
            {
                int spawnOptSel = allOption[
                    role.GetRoleOptionId(RoleCommonOption.SpawnRate)].GetValue();
                int roleNum = allOption[
                    role.GetRoleOptionId(RoleCommonOption.RoleNum)].GetValue();

                if (spawnOptSel < 1 || roleNum <= 0)
                {
                    continue;
                }

                ExtremeRoleId exId = (ExtremeRoleId)id;
                ExtremeRoleType team = role.Team;

                // クイーンとサーヴァントとジャッカルとサイドキックはニュートラルの最後に追加する(役職のパターンがいくつかあるため)
                if (exId != ExtremeRoleId.Queen ||
                    exId != ExtremeRoleId.Jackal)
                {
                    Add(exId, team);
                    separetedRoleId[team].Add(exId);
                }
                switch (exId)
                {
                    case ExtremeRoleId.Jackal:
                        jackalOn = true;
                        jackalForceReplaceLover = allOption[role.GetRoleOptionId(
                            Solo.Neutral.Jackal.JackalOption.ForceReplaceLover)].GetValue();
                        break;
                    case ExtremeRoleId.Queen:
                        queenOn = true;
                        break;
                    case ExtremeRoleId.Hypnotist:
                        // 本来はニュートラルであるがソート用にインポスターとして突っ込む
                        Add(ExtremeRoleId.Doll, ExtremeRoleType.Impostor);
                        break;
                    default:
                        break;
                }
            }

            // ジャッカルとサイドキック、サイドキック + ラバーズの追加
            if (jackalOn)
            {
                Add(ExtremeRoleId.Jackal, ExtremeRoleType.Neutral);
                Add(ExtremeRoleId.Sidekick, ExtremeRoleType.Neutral);
                foreach (var (id, roleMng) in ExtremeRoleManager.CombRole)
                {
                    int spawnOptSel = allOption[
                        roleMng.GetRoleOptionId(RoleCommonOption.SpawnRate)].GetValue();
                    int roleNum = allOption[
                        roleMng.GetRoleOptionId(RoleCommonOption.RoleNum)].GetValue();

                    if (spawnOptSel < 1 || roleNum <= 0 ||
                        id != (byte)CombinationRoleType.Lover)
                    {
                        continue;
                    }
                    Add(ExtremeRoleId.Lover, ExtremeRoleType.Neutral, ExtremeRoleId.Sidekick);
                }
            }

            // クイーンとサーヴァント、サーヴァント + 〇〇、〇〇 + サーヴァントの追加
            if (queenOn)
            {
                ExtremeRoleType queenTeam = ExtremeRoleType.Neutral;
                Add(ExtremeRoleId.Queen, queenTeam);
                ExtremeRoleId servantId = ExtremeRoleId.Servant;

                if (separetedRoleId[queenTeam].Count > 1)
                {
                    Add(servantId, queenTeam);
                }
                foreach (var roleList in new List<ExtremeRoleId>[]
                    { 
                        separetedRoleId[ExtremeRoleType.Crewmate],
                        separetedRoleId[ExtremeRoleType.Impostor],
                    })
                {
                    ListAdd(servantId, queenTeam, roleList);
                }
                foreach (var (id, roleMng) in ExtremeRoleManager.CombRole)
                {
                    int spawnOptSel = allOption[
                        roleMng.GetRoleOptionId(RoleCommonOption.SpawnRate)].GetValue();
                    int roleNum = allOption[
                        roleMng.GetRoleOptionId(RoleCommonOption.RoleNum)].GetValue();

                    if (spawnOptSel < 1 || roleNum <= 0)
                    {
                        continue;
                    }
                    foreach (var role in roleMng.Roles)
                    {
                        Add(role.Id, queenTeam, servantId);
                    }
                }
            }

            foreach (var (id, roleMng) in ExtremeRoleManager.CombRole)
            {
                int spawnOptSel = allOption[
                    roleMng.GetRoleOptionId(RoleCommonOption.SpawnRate)].GetValue();
                int roleNum = allOption[
                    roleMng.GetRoleOptionId(RoleCommonOption.RoleNum)].GetValue();

                bool multiAssign = allOption[
                    roleMng.GetRoleOptionId(
                        CombinationRoleCommonOption.IsMultiAssign)].GetValue();

                if (spawnOptSel < 1 || roleNum <= 0)
                {
                    continue;
                }
                if (multiAssign && id != (byte)CombinationRoleType.Traitor)
                {
                    foreach (var role in roleMng.Roles)
                    {
                        ExtremeRoleType team = role.Team;
                        ListAdd(role.Id, team, separetedRoleId[team]);
                    }
                }
                else
                {
                    foreach (var role in roleMng.Roles)
                    {
                        Add(role.Id, role.Team);
                    }
                }
            }

            return result.OrderBy(
                (GuessBehaviour.RoleInfo x) =>
                {
                    ExtremeRoleType team = x.Team;
                    if (team == ExtremeRoleType.Neutral)
                    {
                        return 5000;
                    }
                    else
                    {
                        return (int)team;
                    }
                }).ToList();
        }

        private static void missGuess()
        {
            Player.RpcUncheckMurderPlayer(
                CachedPlayerControl.LocalPlayer.PlayerId,
                CachedPlayerControl.LocalPlayer.PlayerId,
                byte.MinValue);
            Sound.RpcPlaySound(Sound.SoundType.Kill);
        }

        public void GuessAction(GuessBehaviour.RoleInfo roleInfo, byte playerId)
        {
            // まず弾をへらす
            this.bulletNum = this.bulletNum - 1;

            var targetRole = ExtremeRoleManager.GameRole[playerId];
            ExtremeRoleId roleId = targetRole.Id;
            ExtremeRoleId anotherRoleId = ExtremeRoleId.Null;
            if (targetRole is MultiAssignRoleBase multiRole &&
                multiRole.AnotherRole != null)
            {
                anotherRoleId = multiRole.AnotherRole.Id;
            }
            
            if (Solo.Crewmate.BodyGuard.TryGetShiledPlayerId(playerId, out byte _) ||
                alwaysMissRole.Contains(targetRole.Id))
            {
                missGuess();
            }
            else if (roleInfo.Id == roleId || roleInfo.AnothorId == anotherRoleId)
            {
                Player.RpcUncheckMurderPlayer(
                    CachedPlayerControl.LocalPlayer.PlayerId,
                    playerId, byte.MinValue);
                Sound.RpcPlaySound(Sound.SoundType.Kill);
            }
            else
            {
                missGuess();
            }
        }

        public void IntroBeginSetUp()
        {
            this.isEvil = false;
            if (this.IsImpostor())
            {
                this.RoleName = string.Concat("Evil", this.RoleName);
                this.isEvil = true;
            }
            else
            {
                this.RoleName = string.Concat("Nice", this.RoleName);
            }
        }

        public void IntroEndSetUp()
        {
            return;
        }

        public bool IsBlockMeetingButtonAbility(
            PlayerVoteArea instance)
        {
            byte target = instance.TargetPlayerId;

            return
                this.bulletNum <= 0 ||
                this.curGuessNum >= this.maxGuessNum ||
                target == 253;
        }

        public void ButtonMod(PlayerVoteArea instance, UiElement abilityButton)
        {
            
        }

        public Action CreateAbilityAction(PlayerVoteArea instance)
        {
            void openGusserUi()
            {
                if (this.uiPrefab == null)
                {
                    this.uiPrefab = UnityEngine.Object.Instantiate(
                        Loader.GetGameObjectFromResources(
                            Path.GusserUiResources,
                            Path.GusserUiPrefab));

                    this.uiPrefab.SetActive(false);
                }
                if (this.guesserUi == null)
                {
                    GameObject obj = UnityEngine.Object.Instantiate(
                        this.uiPrefab, MeetingHud.Instance.transform);
                    this.guesserUi = obj.GetComponent<GuesserUi>();

                    this.guesserUi.gameObject.SetActive(true);
                    this.guesserUi.InitButton(GuessAction, createRoleInfo());
                    this.guesserUi.SetTextFontMaterial(
                        instance.NameText);
                    this.guesserUi.SetTitle(
                        Translation.GetString("guesserUiTitle"));
                }
                this.guesserUi.SetTarget(instance.TargetPlayerId);
                this.guesserUi.gameObject.SetActive(true);
            }
            return openGusserUi;
        }

        public void SetSprite(SpriteRenderer render)
        {
            
        }

        public override string GetFullDescription()
        {
            if (this.isEvil)
            {
                return Translation.GetString(
                    $"{this.Id}ImposterFullDescription");
            }
            else
            {
                return base.GetFullDescription();
            }
        }

        protected override void CreateSpecificOption(
            IOption parentOps)
        {
            var imposterSetting = OptionHolder.AllOption[
                GetManagerOptionId(CombinationRoleCommonOption.IsAssignImposter)];
            CreateKillerOption(imposterSetting);

        }

        protected override void RoleSpecificInit()
        {
            this.isEvil = false;
        }
    }
}