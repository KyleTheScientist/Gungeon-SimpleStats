using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MonoMod.RuntimeDetour;
using StatType = PlayerStats.StatType;
using ItemAPI;
namespace Mod
{
    public class SimpleStats : MonoBehaviour
    {
        public static SimpleStats Instance;
        private bool m_built;
        private Dictionary<string, Text> textElements, textElements2;
        public static StatType[] stats = (StatType[])Enum.GetValues(typeof(StatType));
        private static Hook recalculateStats = new Hook(
            typeof(PlayerStats).GetMethod("RecalculateStatsInternal", BindingFlags.Public | BindingFlags.Instance),
            typeof(SimpleStats).GetMethod("OnUpdateStats")
        );
        private static string ConfigDirectory = Path.Combine(ETGMod.ResourcesDirectory, "ktsconfig");
        private static string SaveFilePath = Path.Combine(ConfigDirectory, "simplestats.json");
        public Configuration configuration = new Configuration()
        {
            shown = true,
            fontSize = 16,
            buffer = 3,
            activeElements = new string[] { "Curse", "Coolness", "Damage" },
        };


        void Start()
        {
            try
            {
                CreateOrLoadConfiguration();
                Build();
                Instance = this;
                UpdateAppearance();
            }
            catch (Exception e)
            {
                Tools.PrintException(e);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
                this.ToggleVisibility();
        }

        private void CreateOrLoadConfiguration()
        {
            if (!File.Exists(SaveFilePath))
            {
                ETGModConsole.Log("SimpleStats: Unable to find existing config, making a new one!", false);
                Directory.CreateDirectory(ConfigDirectory);
                File.Create(SaveFilePath).Close();
                UpdateConfiguration();
            }
            else
            {
                string json = File.ReadAllText(SaveFilePath);
                if (!string.IsNullOrEmpty(json))
                    configuration = JsonUtility.FromJson<Configuration>(json);
                else
                    UpdateConfiguration();
            }
        }

        void Build()
        {
            if (m_built) return;
            textElements = new Dictionary<string, Text>();
            textElements2 = new Dictionary<string, Text>();
            ETGModConsole.Log("Building Stat Display...");
            float spacing = (configuration.fontSize + configuration.buffer);
            float yOffset = (spacing * stats.Length) / 2f;
            for (int i = 0; i < stats.Length; i++)
            {
                textElements.Add(stats[i].ToString(), KGUI.CreateText(null, new Vector2(0, (i * -spacing) + yOffset), "", TextAnchor.MiddleLeft, configuration.fontSize));
                textElements2.Add(stats[i].ToString(), KGUI.CreateText(null, new Vector2(60, (i * -spacing) + yOffset), "", TextAnchor.MiddleRight, configuration.fontSize));
            }
            KGUI.SetVisible(configuration.shown);
            m_built = true;
        }

        public bool ToggleVisibility()
        {
            bool visible = KGUI.Toggle();
            configuration.shown = visible;
            UpdateConfiguration();
            return visible;
        }

        public bool Toggle(string statName)
        {
            var key = StatMap.FirstOrDefault(x => FormatString(x.Value) == statName).Key;
            bool active = !textElements[key].gameObject.activeSelf;
            textElements[key].gameObject.SetActive(active);
            textElements2[key].gameObject.SetActive(active);
            RepositionElements();
            return active;
        }

        public void SetAll(bool active)
        {
            foreach (var element in textElements)
                element.Value.gameObject.SetActive(active);
            foreach (var element in textElements2)
                element.Value.gameObject.SetActive(active);
            RepositionElements();
        }

        public void UpdateAppearance()
        {
            if (!m_built) return;

            string statID;
            List<string> activeElements = configuration.activeElements.ToList<string>();
            foreach (var playerStat in stats)
            {
                statID = playerStat.ToString();
                var element = Instance.textElements[statID];
                element.text = $"{StatMap[statID]}: {FormatDecimal(GetStatValue(playerStat, 0))}";
                element.fontSize = configuration.fontSize;
                element.gameObject.SetActive(activeElements.Contains(statID));
                var element2 = Instance.textElements2[statID];
                if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                    element2.text = $"{StatMap[statID]}: {FormatDecimal(GetStatValue(playerStat, 1))}";
                else
                    element2.text = string.Empty;
                element2.fontSize = configuration.fontSize;
                element2.gameObject.SetActive(activeElements.Contains(statID));
            }
            RepositionElements();
        }

        public void RepositionElements()
        {
            List<RectTransform> activeElements = new List<RectTransform>();
            List<RectTransform> activeElements2 = new List<RectTransform>();
            List<string> activeElementsConfig = new List<string>();
            foreach (var element in textElements)
            {
                if (element.Value.gameObject.activeSelf)
                {
                    activeElements.Add(element.Value.GetComponent<RectTransform>());
                    activeElementsConfig.Add(element.Key);
                }
            }
            foreach (var element in textElements2)
            {
                if (element.Value.gameObject.activeSelf)
                    activeElements2.Add(element.Value.GetComponent<RectTransform>());
            }
            configuration.activeElements = activeElementsConfig.ToArray();
            UpdateConfiguration();

            float spacing = (configuration.fontSize + configuration.buffer);
            float yOffset = (spacing * activeElements.Count) / 2f;
            for (int i = 0; i < activeElements.Count; i++)
            {
                activeElements[i].anchoredPosition = new Vector2(0, (i * -spacing) + yOffset);
                activeElements2[i].anchoredPosition = new Vector2(0, (i * -spacing) + yOffset);
            }
        }

        public void UpdateConfiguration()
        {
            if (!File.Exists(SaveFilePath))
            {
                ETGModConsole.Log("SimpleStats: Unable to find existing config, making a new one!", false);
                Directory.CreateDirectory(ConfigDirectory);
                File.Create(SaveFilePath).Close();
            }
            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(configuration, true));
        }

        public static void OnUpdateStats(Action<PlayerStats, PlayerController> orig, PlayerStats self, PlayerController player)
        {
            orig(self, player);
            if (Instance)
                Instance.UpdateAppearance();
        }

        public float GetStatValue(StatType type, int playerID = 0)
        {
            PlayerController player = playerID == 0 ? GameManager.Instance?.PrimaryPlayer : GameManager.Instance?.SecondaryPlayer;
            if (!player) return -1;
            if (type == StatType.Coolness)
            {
                float coolness = player.stats.GetStatValue(StatType.Coolness);
                if (PassiveItem.IsFlagSetForCharacter(player, typeof(ChamberOfEvilItem)))
                    coolness += player.stats.GetStatValue(StatType.Curse) * 2;
                return coolness;
            }
            return player.stats.GetStatValue(type);
        }

        public static string FormatDecimal(float num)
        {
            return num.ToString("F2").Replace(".00", "");
        }

        public static string FormatString(string s)
        {
            return s.ToLower().Replace(' ', '_');
        }


        public struct Configuration
        {
            public bool shown;
            public int fontSize;
            public int buffer;
            public string[] activeElements;
        }

        public static Dictionary<string, string> StatMap = new Dictionary<string, string>()
        {
            { "Accuracy", "Spread" },
            { "AdditionalBlanksPerFloor", "Extra Per-Floor Blanks" },
            { "AdditionalClipCapacityMultiplier", "Clip Size" },
            { "AdditionalGunCapacity", "Gun Slots" },
            { "AdditionalItemCapacity", "Extra Active Slots" },
            { "AdditionalShotBounces", "Shot Bounces" },
            { "AdditionalShotPiercing", "Shot Pierces" },
            { "AmmoCapacityMultiplier", "Ammo Capacity" },
            { "ChargeAmountMultiplier", "Charge Multiplier" },
            { "Coolness", "Coolness" },
            { "Curse", "Curse" },
            { "Damage", "Damage" },
            { "DamageToBosses", "Boss Damage" },
            { "DodgeRollDamage", "Roll Damage" },
            { "DodgeRollDistanceMultiplier", "Roll Distance" },
            { "DodgeRollSpeedMultiplier", "Roll Time" },
            { "EnemyProjectileSpeedMultiplier", "Enemy Shot Speed" },
            { "ExtremeShadowBulletChance", "Y.V. Chance" },
            { "GlobalPriceMultiplier", "Price Multiplier" },
            { "Health", "Heart Containers" },
            { "KnockbackMultiplier", "Knockback" },
            { "MoneyMultiplierFromEnemies", "Money Drop Multiplier" },
            { "MovementSpeed", "Speed" },
            { "PlayerBulletScale", "Bullet Scale" },
            { "ProjectileSpeed", "Shot Speed" },
            { "RangeMultiplier", "Range" },
            { "RateOfFire", "Rate of Fire" },
            { "ReloadSpeed", "Reload Time" },
            { "ShadowBulletChance", "Shadow Bullet Chance" },
            { "TarnisherClipCapacityMultiplier", "Tarnisher Clip Debuff" },
            { "ThrownGunDamage", "Thrown Gun Damage" },
        };
    }
}
