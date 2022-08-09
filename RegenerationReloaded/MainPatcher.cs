using HarmonyLib;
using Helper;
using System;
using System.Reflection;
using RegenerationReloaded.lang;
using UnityEngine;
using System.Threading;

namespace RegenerationReloaded
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        private static float _delay;

        public static void Patch()
        {
            try
            {
                _cfg = Config.GetOptions();

                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.RegenerationReloaded");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _delay = _cfg.RegenDelay;
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void Log(string message, bool error = false)
        {
            if (_cfg.Debug || error)
            {
                Tools.Log("RegenerationReloaded", $"{message}", error);
            }
        }

        private static string GetLocalizedString(string content)
        {
            Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            return content;
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {

                if (Input.GetKeyUp(KeyCode.F5))
                {
                    _cfg = Config.GetOptions();
                    _delay = _cfg.RegenDelay;

                    if (!CrossModFields.ConfigReloadShown)
                    {
                        Tools.ShowMessage(GetLocalizedString(strings.ConfigMessage), Vector3.zero);
                        CrossModFields.ConfigReloadShown = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerComponent), "Update")]
        public static class PlayerComponentUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                var energyRegen = Math.Abs(_cfg.EnergyRegen);
                var lifeRegen = Math.Abs(_cfg.LifeRegen);
                var player = MainGame.me.player;
                var save = MainGame.me.save;

                if (_delay == 0.0f)
                {
                    if (player.energy < save.max_energy)
                    {
                        player.energy += energyRegen;
                        if (!player.energy.EqualsOrMore(save.max_energy) && _cfg.ShowRegenUpdates)
                        {
                            EffectBubblesManager.ShowStackedEnergy(player, energyRegen);
                        }
                    }

                    if (player.hp < save.max_hp)
                    {
                        player.hp += lifeRegen;
                        if (!player.hp.EqualsOrMore(save.max_hp) && _cfg.ShowRegenUpdates)
                        {
                            EffectBubblesManager.ShowStackedHP(player, lifeRegen);
                        }
                    }

                    _delay = _cfg.RegenDelay;
                }
                else
                {
                    _delay = _delay <= 0.0 ? 0.0f : _delay - Time.deltaTime;
                }
            }
        }
    }
}