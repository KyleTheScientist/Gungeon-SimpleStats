using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ItemAPI;

namespace Mod
{
    public class Module : ETGModule
    {
        public static readonly string MOD_NAME = "SimpleStats";
        public static readonly string VERSION = "4.2.0";
        public static readonly string TEXT_COLOR = "#00FFFF";

        public override void Start()
        {
            try
            {
                Tools.Init();
                KGUI.Init();
                KGUI.KGUIController.gameObject.AddComponent<SimpleStats>();
                SetupCommands();
            }
            catch (Exception e) { Tools.PrintException(e); }
            Log($"{MOD_NAME} v{VERSION} started successfully.", TEXT_COLOR);
        }

        public static void SetupCommands()
        {
            ETGModConsole.Commands.AddGroup("simplestats", (args) =>
            {
                if (args != null && args.Length != 0)
                {
                    HandleToggle(string.Join(" ", args));
                    return;
                }
                bool active = SimpleStats.Instance.ToggleVisibility();
                string text = active ? "shown" : "hidden";
                string color = active ? "00FF00" : "FF0000";
                Tools.Print($"SimpleStats {text}", color, true);
            }, new AutocompletionSettings(AutoCompleteStatNames));

            ETGModConsole.Commands.GetGroup("simplestats").AddUnit("fontsize", (args) =>
            {
                if (args == null || args.Length == 0)
                {
                    Tools.PrintError($"Invalid fontsize: No value");
                    Tools.PrintError("Usage: simplestats fontsize <number>");
                    return;
                }
                float fontsize;
                if (!float.TryParse(args[0], out fontsize))
                {
                    Tools.PrintError($"Invalid fontsize: \"{args[0]}\"");
                    Tools.PrintError("Usage: simplestats fontsize <number>");
                    return;
                }

                SimpleStats.Instance.configuration.fontSize = (int)fontsize;
                SimpleStats.Instance.UpdateAppearance();

                Tools.Print($"Font size set to {(int)fontsize}", force: true);
            });

            ETGModConsole.Commands.GetGroup("simplestats").AddUnit("list", (args) =>
            {
                foreach (var statName in SimpleStats.StatMap.Values)
                {
                    Tools.Print($"\t{SimpleStats.FormatString(statName)}", "CCCCFF", force: true);
                }
            });

            ETGModConsole.Commands.GetGroup("simplestats").AddUnit("all", (args) => { SimpleStats.Instance.SetAll(true); });

            ETGModConsole.Commands.GetGroup("simplestats").AddUnit("none", (args) =>
            {
                SimpleStats.Instance.SetAll(false);
            });

            ETGModConsole.Commands.AddGroup("setstat", (args) =>
            {
                var player = GameManager.Instance.PrimaryPlayer;
                if (!player)
                {
                    Tools.PrintError("Could not find Player 1");
                    return;
                }
                SetStat(args, player);
            }, new AutocompletionSettings(AutoCompleteStatNames));

            ETGModConsole.Commands.AddGroup("setstat2", (args) =>
            {
                var player = GameManager.Instance.SecondaryPlayer;
                if (!player)
                {
                    Tools.PrintError("Could not find Player 2");
                    return;
                }
                SetStat(args, player);
            }, new AutocompletionSettings(AutoCompleteStatNames));
        }

        public static void SetStat(string[] args, PlayerController player)
        {
            string statInput = args == null ? string.Empty : args[0];
            if (string.IsNullOrEmpty(statInput))
            {
                Tools.PrintError($"Invalid stat ID: \"{statInput}\".");
                Tools.PrintError("Usage: setstat <stat> <number>");
                return;
            }
            if (args.Length == 1)
            {
                Tools.PrintError($"No stat value found.");
                Tools.PrintError("Usage: setstat <stat> <number>");
                return;
            }

            string key = SimpleStats.StatMap.FirstOrDefault(x => SimpleStats.FormatString(x.Value) == statInput).Key;
            PlayerStats.StatType statToSet = PlayerStats.StatType.Accuracy;
            try
            {
                statToSet = Tools.GetEnumValue<PlayerStats.StatType>(key, false);
            }
            catch (Exception e)
            {
                Tools.PrintError($"Invalid stat name: {statInput}");
                Tools.PrintException(e);
                return;
            }
            float value;
            if (!float.TryParse(args[1], out value))
            {
                Tools.PrintError($"Invalid value: \"{args[1]}\"");
                Tools.PrintError("Usage: setstat <stat> 4<number>");
                return;
            }

            if (statToSet == PlayerStats.StatType.DodgeRollDistanceMultiplier)
                player.rollStats.rollDistanceMultiplier = value;
            else if (statToSet == PlayerStats.StatType.DodgeRollSpeedMultiplier)
                player.rollStats.rollTimeMultiplier = value;

            player.stats.SetBaseStatValue(statToSet, value, player);
            Tools.Print($"{key} = {value}", "CCCCFF", true);
        }


        public static void HandleToggle(string input)
        {
            string formattedName;

            foreach (var statName in SimpleStats.StatMap.Values)
            {
                formattedName = SimpleStats.FormatString(statName);
                if (SimpleStats.FormatString(input).Equals(formattedName))
                {
                    bool active = SimpleStats.Instance.Toggle(formattedName);
                    string text = active ? "shown" : "hidden";
                    string color = active ? "00FF00" : "FF0000";
                    Tools.Print($"{statName} {text}", color, true);
                    return;
                }
            }
            Tools.PrintError($"Invalid stat ID: \"{input}\".");
            Tools.PrintError($" Use `simplestats list` for a list of stat names.");
        }

        public static string[] AutoCompleteStatNames(string input)
        {
            List<string> list = new List<string>();
            string formattedName;
            foreach (string statName in SimpleStats.StatMap.Values)
            {
                formattedName = SimpleStats.FormatString(statName);
                if (formattedName.AutocompletionMatch(input.ToLower()))
                    list.Add(formattedName.ToLower());
            }
            return list.ToArray();
        }

        public static void Log(string text, string color = "FFFFFF")
        {
            ETGModConsole.Log($"<color={color}>{text}</color>");
        }

        public override void Exit() { }
        public override void Init() { }
    }
}
