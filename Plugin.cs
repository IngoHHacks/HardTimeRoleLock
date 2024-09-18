using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace HardTimeRoleLock
{
    [BepInPlugin(PluginGuid, PluginName, PluginVer)]
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "IngoH.HardTime.HardTimeRoleLock";
        public const string PluginName = "HardTimeRoleLock";
        public const string PluginVer = "1.0.0";

        internal static ManualLogSource Log;
        internal readonly static Harmony Harmony = new(PluginGuid);

        internal static string PluginPath;

        internal static ConfigEntry<bool> LockPlayerRole;
        internal static ConfigEntry<bool> LockNPCRoles;
        internal static ConfigEntry<bool> PreventPlayerDeath;
        internal static ConfigEntry<bool> PreventNPCDeath;
        internal static ConfigEntry<bool> PreventNPCRespawn;
        internal static ConfigEntry<bool> NoRespawnRegenerating;
        internal static ConfigEntry<bool> UndecapitatePreventedDeaths;

        private void Awake()
        {
            Plugin.Log = base.Logger;

            PluginPath = Path.GetDirectoryName(Info.Location);

            LockPlayerRole = Config.Bind("General", "LockPlayerRole", false, "Lock the player's role.");
            LockNPCRoles = Config.Bind("General", "LockNPCRoles", true, "Lock NPC roles.");
            PreventPlayerDeath = Config.Bind("General", "PreventPlayerDeath", false, "Prevent the player from dying.");
            PreventNPCDeath = Config.Bind("General", "PreventNPCDeath", false, "Prevent NPCs from dying.");
            PreventNPCRespawn = Config.Bind("General", "PreventNPCRespawn", true, "Prevent NPCs from respawning. (Includes babies)");
            NoRespawnRegenerating = Config.Bind("General", "NoRespawnRegenerating", false, "If NPC respawning is enabled, make it so NPCs keep their original appearance and stats when they respawn.");
            UndecapitatePreventedDeaths = Config.Bind("General", "UndecapitatePreventedDeaths", true, "Undecapitate NPCs that were prevented from dying (to prevent them from being stuck dying indefinitely).");
        }

        private void OnEnable()
        {
            Harmony.PatchAll();
            Logger.LogInfo($"Loaded {PluginName}!");
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
            Logger.LogInfo($"Unloaded {PluginName}!");
        }

        int[] prevRole = new int[0];
        int[] prevDead = new int[0];
    
        private void Update()
        {
            if (MappedCharacters.c == null)
            {
                return;
            }
            if (SceneManager.GetActiveScene().name == "Editor" || SceneManager.GetActiveScene().name == "Select_Char")
            {
                for (int i = 0; i <= MappedCharacters.no_chars; i++)
                {
                    prevRole[i] = -999;
                    prevDead[i] = -999;
                }
            }
            if (MappedCharacters.no_chars + 1 > prevRole.Length)
            {
                var oldLen = prevRole.Length;
                Array.Resize(ref prevRole, MappedCharacters.no_chars + 1);
                Array.Resize(ref prevDead, MappedCharacters.no_chars + 1);
                for (int i = oldLen; i <= MappedCharacters.no_chars; i++)
                {
                    prevRole[i] = -999;
                    prevDead[i] = -999;
                }
            }
            for (int i = 1; i <= MappedCharacters.no_chars; i++)
            {
                if (MappedCharacters.c[i] == null)
                {
                    continue;
                }
                var isPlayer = i == MappedCharacters.star;
                if (MappedCharacters.c[i].role != prevRole[i])
                {
                    if (prevRole[i] != -999)
                    {
                        if (isPlayer && LockPlayerRole.Value)
                        {
                            MappedCharacters.c[i].role = prevRole[i];
                        }
                        else if (!isPlayer && LockNPCRoles.Value)
                        {
                            MappedCharacters.c[i].role = prevRole[i];
                        }
                    }
                    prevRole[i] = MappedCharacters.c[i].role;
                }
                if (MappedCharacters.c[i].dead != prevDead[i])
                {
                    if (prevDead[i] != -999)
                    {
                        if (isPlayer && PreventPlayerDeath.Value)
                        {
                            MappedCharacters.c[i].dead = prevDead[i];
                        }
                        else if (!isPlayer && MappedCharacters.c[i].dead > 0 && PreventNPCDeath.Value)
                        {
                            MappedCharacters.c[i].dead = prevDead[i];
                            if (UndecapitatePreventedDeaths.Value)
                            {
                                if (MappedCharacters.c[i].scar[3] < 0)
                                {
                                    MappedCharacters.c[i].scar[3] = 0;
                                }
                            }
                        }
                        else if (!isPlayer && MappedCharacters.c[i].dead == 0 && PreventNPCRespawn.Value)
                        {
                            MappedCharacters.c[i].dead = prevDead[i];
                            if (UndecapitatePreventedDeaths.Value)
                            {
                                if (MappedCharacters.c[i].scar[3] < 0)
                                {
                                    MappedCharacters.c[i].scar[3] = 0;
                                }
                            }
                        }
                    }
                    prevDead[i] = MappedCharacters.c[i].dead;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.EAFNGPJHAFG))]
        [HarmonyPrefix]
        private static bool Character_EAFNGPJHAFG(Character __instance)
        {
            var id = __instance.id;
            if (id == MappedCharacters.star && LockPlayerRole.Value)
            {
                return false;
            }
            if (id != MappedCharacters.star && LockNPCRoles.Value)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.PNNCNPKGGGD))]
        [HarmonyPrefix]
        private static bool Character_PNNCNPKGGGD(Character __instance)
        {
            if (__instance.dead > 0 && (PreventNPCRespawn.Value || NoRespawnRegenerating.Value))
            {
                if (!PreventNPCRespawn.Value) {
                    __instance.dead = 0;
                    __instance.health = 100f;
                    __instance.spirit = 50f;
                    __instance.grudge = 0;
                    __instance.pregnant = 0;
                    __instance.cuffed = 0;
                    __instance.chained = 0;
                    __instance.platform = 0;
                    __instance.crime = 0;
                    __instance.warrant = 0;
                }
                return false;
            }
            return true;
        }
    }
}