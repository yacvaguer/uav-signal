﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("UAVSignal", "Yac Vaguer", "1.4.5")]
    [Description("Call a UAV to detect nearby players and NPCs.")]
    public class UAVSignal : RustPlugin
    {
        #region Fields

        private const string PermissionUse = "uavsignal.use";
        private const string SupplySignalShortname = "supply.signal";
        private const string F15Prefab = "assets/scripts/entity/misc/f15/f15e.prefab";
        private HashSet<BasePlayer> playersWithUI = new HashSet<BasePlayer>();
        private HashSet<BasePlayer> currentPlayersInRadius = new HashSet<BasePlayer>();

        private Configuration config;
        private HashSet<BasePlayer> trackedPlayers = new HashSet<BasePlayer>();
        private Timer markingPlayersTimer;
        private Timer playersInRadiusTimer;
        private BasePlayer caller;
        private bool isActive = false;

        private List<LootContainer> processedContainers = new List<LootContainer>();

        #endregion

        #region Initialization

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            LoadDefaultMessages();
        }

        private void OnServerInitialized()
        {
            LoadConfigValues();
        }

        private void Unload()
        {
            DestroyUAV();
        }

        #endregion

        #region Commands

        [ChatCommand("uav")]
        private void UAVCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player))
            {
                player.ChatMessage(GetMessage("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length == 1)
            {
                var targetPlayer = FindPlayer(args[0]);
                if (targetPlayer != null)
                {
                    GiveUAVSignal(targetPlayer);
                    player.ChatMessage(
                        string.Format(
                            GetMessage("UAVGiven", player.UserIDString),
                            targetPlayer.displayName
                        )
                    );
                }
                else
                {
                    player.ChatMessage(
                        string.Format(GetMessage("PlayerNotFound", player.UserIDString), args[0])
                    );
                }
            }
            else
            {
                GiveUAVSignal(player);
                player.ChatMessage(GetMessage("UAVReceived", player.UserIDString));
            }
        }

        [ConsoleCommand("uav")]
        private void UAVConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (player != null && !HasPermission(player))
            {
                arg.ReplyWith(GetMessage("NoPermission", player.UserIDString));
                return;
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                arg.ReplyWith("Usage: uav <player name or ID>");
                return;
            }

            var targetPlayer = FindPlayer(arg.Args[0]);
            if (targetPlayer != null)
            {
                GiveUAVSignal(targetPlayer);
                arg.ReplyWith($"UAV signal given to {targetPlayer.displayName}");
            }
            else
            {
                arg.ReplyWith($"Player '{arg.Args[0]}' not found.");
            }
        }

        #endregion

        #region Event Hooks

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (entity is SupplySignal signal && signal.skinID == config.UAV.SkinID)
            {
                ThrowUAV(signal, player);
            }
        }

        private void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if (entity is SupplySignal signal && signal.skinID == config.UAV.SkinID)
            {
                ThrowUAV(signal, player);
            }
        }

        private void ThrowUAV(SupplySignal signal, BasePlayer player)
        {
            signal.CancelInvoke(signal.Explode);
            signal.CancelInvoke(signal.KillMessage);
            signal.Kill();
            StartUAV(player);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer player)
            {
                if (playersWithUI.Contains(player))
                {
                    HideTrackedIcon(player);
                }

                if (player == caller)
                {
                    DestroyUAV();
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (playersWithUI.Contains(player))
            {
                HideTrackedIcon(player);
            }

            if (player == caller)
            {
                DestroyUAV();
            }
        }

        private object CanLootEntity(BasePlayer player, LootContainer container)
        {
            if (container == null || !config.Loot.Enabled)
                return null;

            if (processedContainers.Contains(container))
                return null;

            processedContainers.Add(container);

            string containerName = container.ShortPrefabName;
            float dropChance;

            if (config.Loot.Containers.TryGetValue(containerName, out dropChance))
            {
                if (RollChance(dropChance))
                {
                    var item = ItemManager.CreateByName(
                        SupplySignalShortname,
                        1,
                        config.UAV.SkinID
                    );
                    if (item != null)
                    {
                        item.name = config.UAV.ItemName;
                        container.inventory.capacity++;
                        container.inventorySlots++;
                        item.MoveToContainer(container.inventory);
                    }
                }
            }

            return null;
        }

        private void OnEntityKill(LootContainer container)
        {
            if (container != null)
            {
                processedContainers.Remove(container);
            }
        }

        #endregion

        #region UAV Logic

        private void StartUAV(BasePlayer player)
        {
            if (isActive)
            {
                player.ChatMessage(GetMessage("UAVAlreadyActive", player.UserIDString));
                GiveUAVSignal(player);
                return;
            }

            isActive = true;
            caller = player;

            SpawnJet(player);
            player.ChatMessage(GetMessage("UAVCalled", player.UserIDString));
            DebugLog(
                $"UAV started by {player.displayName} at position {player.transform.position}"
            );
        }

        private void SpawnJet(BasePlayer player)
        {
            Vector3 targetPosition = player.transform.position;
            Vector3 spawnPosition =
                targetPosition + player.transform.forward.normalized * config.Jet.SpawnDistance;

            var jet = GameManager.server.CreateEntity(F15Prefab, spawnPosition) as F15;
            if (jet == null)
            {
                PrintError("Failed to create F15 entity.");
                isActive = false;
                return;
            }

            jet.transform.position = spawnPosition;
            jet.transform.forward = (targetPosition - spawnPosition).normalized;
            jet.movePosition = targetPosition;
            jet.defaultAltitude = config.Jet.Altitude;
            jet.Spawn();

            jet.Invoke(() => jet.Kill(), config.Jet.Duration);

            timer.Once(config.UAV.Warmup, () => ActivateUAV(player));
            DebugLog("Jet spawned and moving to target position");
        }

        private void ActivateUAV(BasePlayer player)
        {
            player.ChatMessage(GetMessage("UAVActive", player.UserIDString));
            UpdatePlayersInRadius();

            markingPlayersTimer = timer.Every(3f, () => MarkPlayers(player));
            playersInRadiusTimer = timer.Every(3f, UpdatePlayersInRadius);

            timer.Once(config.UAV.Duration, DestroyUAV);
            DebugLog("UAV activated and timers started");
        }

        private void DestroyUAV()
        {
            if (markingPlayersTimer != null && !markingPlayersTimer.Destroyed)
            {
                markingPlayersTimer.Destroy();
            }

            if (playersInRadiusTimer != null && !playersInRadiusTimer.Destroyed)
            {
                playersInRadiusTimer.Destroy();
            }

            isActive = false;
            currentPlayersInRadius.Clear();
            trackedPlayers.Clear();

            foreach (var player in playersWithUI.ToList())
            {
                HideTrackedIcon(player);
            }

            if (caller != null)
            {
                caller.ChatMessage(GetMessage("UAVEnded", caller.UserIDString));
                DebugLog($"UAV ended for {caller.displayName}");
                caller = null;
            }
        }

        private void UpdatePlayersInRadius()
        {
            if (caller == null || !caller.IsConnected)
            {
                DestroyUAV();
                return;
            }

            currentPlayersInRadius.Clear();

            var players = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(
                caller.transform.position,
                config.UAV.Radius,
                players,
                Rust.Layers.Mask.Player_Server
            );

            var validPlayers = players
                .Where(player =>
                    player != null &&
                    player != caller &&
                    (!player.userID.Get().IsSteamId() || player.IsConnected))
                .ToList();

            var playersToRemove = trackedPlayers
                .Except(validPlayers)
                .ToList();
            
            foreach (var player in playersToRemove)
            {
                trackedPlayers.Remove(player);
                DebugLog($"Stopped tracking {(player.IsNpc ? "NPC" : "Player")} '{player.displayName}'");
            }

            foreach (var player in validPlayers)
            {
                currentPlayersInRadius.Add(player);

                if (!playersWithUI.Contains(player))
                {
                    ShowTrackedIcon(player);

                    if (!trackedPlayers.Contains(player))
                    {
                        trackedPlayers.Add(player);

                        if (player.IsNpc)
                        {
                            DebugLog(
                                $"UAV has detected NPC '{player.ShortPrefabName}' at position {player.transform.position}"
                            );
                        }
                        else
                        {
                            DebugLog(
                                $"UAV has detected player '{player.displayName}' (ID: {player.UserIDString}) at position {player.transform.position}"
                            );
                        }
                    }
                }
            }
            HideUIForPlayersThatAreNotInRadius();
        }

        private void HideUIForPlayersThatAreNotInRadius()
        {
            foreach (var player in playersWithUI.ToList())
            {
                if (!currentPlayersInRadius.Contains(player))
                {
                    HideTrackedIcon(player);
                }
            }
        }

        private void MarkPlayers(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
            {
                DestroyUAV();
                DebugLog("Destroying UAV due to error with the player throwing the Signal");
                return;
            }

            foreach (var target in currentPlayersInRadius)
            {
                if (target == null || target.IsDead())
                {
                    DebugLog("Target is null or dead");
                    continue;
                }

                BasePlayer.PingType type = BasePlayer.PingType.Hostile;

                if (player.Team != null && player.Team.members.Contains(target.userID))
                {
                    DebugLog($"Target is part of the team {target.displayName}");
                    continue;
                }

                if (!target.userID.IsSteamId())
                {
                    type = BasePlayer.PingType.Gun;
                }

                Vector3 pingPosition = target.transform.position + Vector3.up * 2f;

                player.AddPingAtLocation(type, pingPosition, 3f, target.net.ID);
                DebugLog(
                    $"Added ping for {(target.IsNpc ? "NPC" : "Player")} '{target.displayName}' level {type} at position {pingPosition}"
                );
            }
        }

        private void ShowTrackedIcon(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
                return;

            if (playersWithUI.Contains(player))
                return;

            CuiElementContainer elements = new CuiElementContainer();

            string panelName = "UAVTrackedPanel";

            elements.Add(
                new CuiPanel
                {
                    Image = { Color = config.UAV.PanelColor },
                    RectTransform =
                    {
                        AnchorMin = config.UAV.TrackedCUIAnchorMin,
                        AnchorMax = config.UAV.TrackedCUIAnchorMax,
                    },
                    CursorEnabled = false,
                },
                "Hud",
                panelName
            );

            elements.Add(
                new CuiLabel
                {
                    Text =
                    {
                        Color = config.UAV.TextColor,
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter,
                        Text = GetMessage("BeingTracked", player.UserIDString),
                    },
                    RectTransform = { AnchorMin = "0.205 0.314", AnchorMax = "0.979 0.8" },
                },
                panelName
            );

            elements.Add(
                new CuiElement
                {
                    Parent = panelName,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Url = config.UAV.TrackedIconUrl,
                        },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0.184 1" },
                    },
                }
            );

            CuiHelper.AddUi(player, elements);
            playersWithUI.Add(player);
        }

        private void HideTrackedIcon(BasePlayer player)
        {
            if (player == null || !player.IsConnected)
                return;

            if (!playersWithUI.Contains(player))
                return;

            CuiHelper.DestroyUi(player, "UAVTrackedPanel");
            playersWithUI.Remove(player);
        }

        #endregion

        #region Helpers

        private bool HasPermission(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, PermissionUse)
                || player.IsAdmin;
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            var players = BasePlayer
                .activePlayerList.Where(p =>
                    p.displayName.Contains(nameOrId, StringComparison.OrdinalIgnoreCase)
                    || p.UserIDString == nameOrId
                )
                .ToList();

            if (players.Count == 1)
                return players[0];

            return null;
        }

        private void GiveUAVSignal(BasePlayer player)
        {
            var item = ItemManager.CreateByName(SupplySignalShortname, 1, config.UAV.SkinID);
            if (item != null)
            {
                item.name = config.UAV.ItemName;
                player.GiveItem(item);
                DebugLog($"Gave UAV signal to {player.displayName}");
            }
        }

        private bool RollChance(float chance)
        {
            return UnityEngine.Random.Range(0f, 100f) <= chance;
        }

        private void DebugLog(string message)
        {
            if (config.DebugMode)
            {
                Puts(message);
            }
        }

        #endregion

        #region Configuration

        private class Configuration
        {
            [JsonProperty("UAV Settings")]
            public UAVSettings UAV { get; set; } = new UAVSettings();

            [JsonProperty("Jet Settings")]
            public JetSettings Jet { get; set; } = new JetSettings();

            [JsonProperty("Loot Settings")]
            public LootSettings Loot { get; set; } = new LootSettings();

            [JsonProperty("Debug Mode")]
            public bool DebugMode { get; set; } = false;
        }

        private class UAVSettings
        {
            [JsonProperty("Duration (seconds)")]
            public float Duration { get; set; } = 180f;

            [JsonProperty("Radius")]
            public float Radius { get; set; } = 40f;

            [JsonProperty("Skin ID")]
            public ulong SkinID { get; set; } = 3248057023;

            [JsonProperty("Warmup Time (seconds)")]
            public float Warmup { get; set; } = 5f;

            [JsonProperty("Item Name")]
            public string ItemName { get; set; } = "UAV Signal";

            [JsonProperty("Tracked Icon URL")]
            public string TrackedIconUrl { get; set; } =
                "https://cdn.rustpluginshub.com/unsafe/50x50/https://rustpluginshub.com/icons/location.png";

            [JsonProperty("Tracked Icon Position (AnchorMin)")]
            public string TrackedCUIAnchorMin { get; set; } = "0.006 0.485";

            [JsonProperty("Tracked Icon Position (AnchorMax)")]
            public string TrackedCUIAnchorMax { get; set; } = "0.105 0.518";

            [JsonProperty("Panel Color")]
            public string PanelColor { get; set; } = "0.96 0.31 0.26 0.47";

            [JsonProperty("Text Color")]
            public string TextColor { get; set; } = "1 1 1 1";
        }

        private class JetSettings
        {
            [JsonProperty("Altitude")]
            public float Altitude { get; set; } = 200f;

            [JsonProperty("Spawn Distance")]
            public float SpawnDistance { get; set; } = 500f;

            [JsonProperty("Duration (seconds)")]
            public float Duration { get; set; } = 15f;
        }

        private class LootSettings
        {
            [JsonProperty("Enable Loot Drops")]
            public bool Enabled { get; set; } = true;

            [JsonProperty("Loot Containers and Drop Chances")]
            public Dictionary<string, float> Containers { get; set; } =
                new Dictionary<string, float>
                {
                    { "crate_normal", 0f },
                    { "crate_normal_2", 0f },
                    { "crate_elite", 2f },
                    { "heli_crate", 5f },
                    { "bradley_crate", 5f },
                };
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            SaveConfig();
            Puts("Default configuration file created.");
        }

        private void LoadConfigValues()
        {
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    SaveConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt; loading default configuration.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    ["NoPermission"] = "You do not have permission to use this command.",
                    ["PlayerNotFound"] = "Player '{0}' not found.",
                    ["UAVGiven"] = "UAV Signal given to {0}.",
                    ["UAVReceived"] = "You have received a UAV Signal.",
                    ["UAVCalled"] = "UAV has been called to your area.",
                    ["UAVAlreadyActive"] = "A UAV is already active.",
                    ["UAVActive"] = "UAV is now active.",
                    ["UAVEnded"] = "UAV has ended.",
                    ["BeingTracked"] = "You are being tracked",
                },
                this
            );
        }

        private string GetMessage(string key, string userId = null)
        {
            return lang.GetMessage(key, this, userId);
        }

        #endregion
    }
}