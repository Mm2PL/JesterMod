using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using UnityEngine;

namespace JesterPlugin
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    public partial class JesterPlugin : BasePlugin
    {
        public const string Id = "pl.kotmisia.jesterrole";

        public static Color JesterColor =
            new Color((float) (240.0 / 255.0), (float) (125.0 / 255.0), (float) (13.0 / 255.0), 1);

        public Harmony Harmony { get; } = new Harmony(Id);

        public override void Load()
        {
            RegisterCustomRpcAttribute.Register(this);
            Harmony.PatchAll();
        }
    }
}