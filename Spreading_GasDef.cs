using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace DarkwatchPack
{
    public class Spreading_GasDef : ThingDef
    {
        public HediffDef hediffToAdd;
        public float hediffChance = 1f;
        public float hediffSeverity = 0.05f;
        public bool ignoreMechanoids;
        public bool bypassToxicSensitivity;
        public DamageDef damageType;
        public float damageAmount = 2f;
        public int hediffFrequency = 30;
        public int damageFrequency = 15;
        public int spawnThingDefFrequency = 60;
        public float spawnThingDefChance = 0.5f;
        public ThingDef thingDefToSpawn;
        public int maxThingSpawns = 2;
        public int cloningGenerations = 0;
    }
}
