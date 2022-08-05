using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace SimpleWarrants
{
    internal class SimpleWarrantsMod : Mod
    {

        public static SimpleWarrantsSettings settings;
        public SimpleWarrantsMod(ModContentPack mod) : base(mod)
        {
            settings = GetSettings<SimpleWarrantsSettings>();
            new Harmony("SimpleWarrants.Mod").PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return Content.Name;
        }
    }
}
