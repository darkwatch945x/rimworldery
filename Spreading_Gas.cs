using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace DarkwatchPack
{
    public class Spreading_Gas : Gas
    {
        public Spreading_GasDef Def => this.def as Spreading_GasDef;

        public int generation;
        public int spawnsDone = 0;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            while (true)
            {
                Thing gas = (Thing)this.Position.GetGas(map);
                if (gas != null)
                    gas.Destroy();
                else
                    break;
            }

            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
                this.destroyTick = Find.TickManager.TicksGame + this.def.gas.expireSeconds.RandomInRange.SecondsToTicks();

            this.graphicRotationSpeed = Rand.Range(-this.Def.gas.rotationSpeed, this.Def.gas.rotationSpeed) / 60f;
        }

        public override void Tick()
        {
            if (this.destroyTick <= Find.TickManager.TicksGame)
                this.Destroy();

            if (this.IsHashIntervalTick(this.Def.hediffFrequency) || this.IsHashIntervalTick(this.Def.damageFrequency))
                this.DoEffects();

            if (this.IsHashIntervalTick(this.Def.spawnThingDefFrequency) && !(this.Def.thingDefToSpawn is null))
                this.SpawnThing(this.Position, this.Map, this.Def.thingDefToSpawn);

            this.graphicRotation += this.graphicRotationSpeed;

            if (this.IsHashIntervalTick(60) && this.Def.cloningGenerations != 0)
                this.CloneSelf(this.Position, this.Map, this.Def.thingDefToSpawn);

        }
        public void DoEffects()
        {
            if (this.Destroyed || this.Def.hediffToAdd == null)
                return;
            List<Thing> things = this.Position.GetThingList(this.Map);
            if (things.Count == 0 || things == null)
                return;
            for (int index = 0; index < things.Count; ++index)
            {
                if (things[index] is Pawn pawn1 && pawn1.Spawned && !pawn1.health.Dead)
                {
                    if (this.IsHashIntervalTick(this.Def.hediffFrequency))
                        this.AddHediffToPawn(pawn1, this.Def.hediffToAdd, this.Def.hediffChance, this.Def.hediffSeverity);
                        
                    if (this.IsHashIntervalTick(this.Def.damageFrequency) && !(this.Def.damageType == null))
                        this.HurtPawn(pawn1);
                        
                }
                    
            }
        }

        public void AddHediffToPawn(Pawn pawn, HediffDef hediffToGive, float chanceToAddHediff, float severityToAdd)
        {
            if (!Rand.Chance(chanceToAddHediff) || (float)severityToAdd <= 0.0)
                return;

            float sensitivityValue = pawn.GetStatValue(StatDefOf.ToxicSensitivity);
            float bodyPercentage = pawn.health.hediffSet.GetCoverageOfNotMissingNaturalParts(pawn.RaceProps.body.corePart);

            Hediff hediff = HediffMaker.MakeHediff(hediffToGive, pawn);

            hediff.Severity = this.Def.bypassToxicSensitivity ? severityToAdd * bodyPercentage : severityToAdd * sensitivityValue * bodyPercentage;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Breathing))
                return;

            if (pawn.health.hediffSet.HasHediff(hediffToGive))
                pawn.health.hediffSet.GetFirstHediffOfDef(hediffToGive).Severity += hediff.Severity*Rand.Range(0.7f, 1.2f);
            else
                pawn.health.AddHediff(hediff);
        }

        public void HurtPawn(Pawn pawn)
        {
            List<BodyPartRecord> bodyparts = pawn.health.hediffSet.GetNotMissingParts(depth: BodyPartDepth.Outside).ToList();
            int index = Rand.Range(0, bodyparts.Count - 1);
            float damage = this.Def.damageAmount;
            BodyPartRecord bodypart = pawn.health.hediffSet.GetNotMissingParts(depth: BodyPartDepth.Outside).ElementAt(index);

            DamageInfo dinfo = new DamageInfo(this.Def.damageType, Mathf.RoundToInt(damage * bodypart.coverage * Rand.Range(0.7f, 1.4f)), instigator: ((Thing)this), hitPart: bodypart);
            pawn.TakeDamage(dinfo);
        }

        public void SpawnThing(IntVec3 location, Map map, ThingDef spawnedThing)
        {
            if (Rand.Range(0.0f, 1.0f) <= this.Def.spawnThingDefChance && !this.Destroyed)
            {
                List<IntVec3> cells = new List<IntVec3>();

                Thing newspawn = ThingMaker.MakeThing(this.Def.thingDefToSpawn);

                Room place = this.Position.GetRoom(this.Map);

                //scan surroundings for spawn candidates, exit function if none found
                foreach (IntVec3 cell in GenAdjFast.AdjacentCells8Way(this.Position))
                {
                    if (!cell.Impassable(this.Map) && !(cell.GetFirstThing(map, this.Def.thingDefToSpawn) is Thing) && cell.GetRoom(this.Map) == place)
                    {
                        cells.Add(cell);
                    }
                }

                if (cells.Count == 0)
                    return;

                //spawns the this.Def.thingDefToSpawn (DO NOT CONFUSE WITH CLONE)
                IntVec3 cellpick = cells[Rand.Range(0, cells.Count)];
                GenSpawn.Spawn(newspawn, cellpick, map);
                
            }
                                   
        }

        public void CloneSelf(IntVec3 location, Map map, ThingDef spawnedThing)
        {
            if (!this.Destroyed && this.generation < this.Def.cloningGenerations)
            {
                Log.Message(this.generation.ToString() + " thisone");
                List<IntVec3> cells = new List<IntVec3>();

                Spreading_Gas clone = ThingMaker.MakeThing(this.def) as Spreading_Gas;

                if (!(clone is Spreading_Gas))
                {
                    Log.Error("You did something bad, we expect MyGas here, but didn't create it!");
                    return;
                }

                Room place = this.Position.GetRoom(this.Map);

                //scan surroundings for spawn candidates, exit function if none found
                foreach (IntVec3 cell in GenAdjFast.AdjacentCells8Way(this.Position))
                {
                    if (!cell.Impassable(this.Map) && !(cell.GetFirstThing(map, this.Def) is Thing) && cell.GetRoom(this.Map) == place)
                    {
                        cells.Add(cell);
                    }
                }

                if (cells.Count == 0)
                    return;

                //clone spawner
                clone.generation += 1;
                Log.Message(clone.generation.ToString() + " nextone");
                IntVec3 cellpick = cells[Rand.Range(0, cells.Count)];
                GenSpawn.Spawn(clone, cellpick, map);
                
            }
                                   
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.destroyTick, "destroyTick");
        }
    }
}
