using System;
using System.Collections.Generic;
using Harmony;
using System.Reflection;
using UnityEngine;

namespace AutoLootHeavies
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.AutoLootHeavies");
            val.PatchAll(Assembly.GetExecutingAssembly());
            // HarmonyInstance.DEBUG = true;
            //FileLog.logPath = "./qmods/log.txt";
        }


        //this works, but need a way to destroy the object on drop when the things arent full
        [HarmonyPatch(typeof(BaseCharacterComponent))]
        [HarmonyPatch(nameof(BaseCharacterComponent.DropOverheadItem))]
        public class DropPatching
        {
            [HarmonyPrefix]
            public static bool Prefix(BaseCharacterComponent __instance,
                out (int freeTimber, int freeStone, int freeOre, WorldGameObject tWgo, WorldGameObject sWgo,
                    WorldGameObject oWgo) __state)
            {
                var timberPileCount = 0;
                var stonePileCount = 0;
                var orePileCount = 0;
                var usedTimberSlots = 0;
                var usedStoneSlots = 0;
                var usedOreSlots = 0;


                var objects = UnityEngine.Object.FindObjectsOfType<WorldGameObject>();
                WorldGameObject timberTemp = null, oreTemp = null, stoneTemp = null;
                foreach (var obj in objects)
                {
                    if (obj.obj_id.Contains("mf_timber"))
                    {
                        usedTimberSlots += obj.data.inventory.Count;
                        timberPileCount++;
                        timberTemp = obj;
                    }

                    if (obj.obj_id.Contains("mf_ore"))
                    {
                        usedOreSlots += obj.data.inventory.Count;
                        orePileCount++;
                        oreTemp = obj;
                    }

                    if (obj.obj_id.Contains("mf_stones"))
                    {
                        usedStoneSlots += obj.data.inventory.Count;
                        stonePileCount++;
                        stoneTemp = obj;
                    }
                }

                var freeTimberSlots = (9 * timberPileCount) - usedTimberSlots;
                var freeStoneSlots = (6 * stonePileCount) - usedStoneSlots;
                var freeOreSlots = (6 * orePileCount) - usedOreSlots;
                __state = (freeTimberSlots, freeStoneSlots, freeOreSlots, timberTemp, oreTemp, stoneTemp);
                return false;
            }


            [HarmonyPostfix]
            public static void Postfix(BaseCharacterComponent __instance,
                (int freeTimber, int freeStone, int freeOre, WorldGameObject tWgo, WorldGameObject sWgo, WorldGameObject
                    oWgo) __state,
                bool to_right = false)
            {
                try
                {
                    var isAttacking = __instance.playing_animation | __instance.playing_work_animation |
                                      __instance.attack.performing_attack;
                    // var wgo = MainGame.me.player;
                    var tWgo = __state.tWgo; //timber
                    var sWgo = __state.sWgo; //stone
                    var oWgo = __state.oWgo; //iron ore

                    var freeTimberSlots = __state.freeTimber;
                    var freeStoneSlots = __state.freeStone; //stone and marble share
                    var freeOreSlots = __state.freeOre;

                    var itemsDidntFit = new List<Item>();
                    var itemsToInsert = new List<Item>();


                    var itemIsLog = __instance.GetOverheadItem().definition.GetItemName(true).Contains("Log");
                    var itemIsStone = __instance.GetOverheadItem().definition.GetItemName(true).Contains("Stone") ||
                                      __instance.GetOverheadItem().definition.GetItemName(true).Contains("Marble");
                    var itemIsOre = __instance.GetOverheadItem().definition.GetItemName(true).Contains("Iron");

                    if (__instance.GetOverheadItem() != null)
                    {
                        var item = __instance.GetOverheadItem();


                        if (itemIsOre)
                        {
                            itemsToInsert.Clear();
                            itemsDidntFit.Clear();
                            itemsToInsert.Add(item);

                            if (oWgo == null)
                            {
                                if (!isAttacking)
                                {
                                    __instance.ShowCustomNeedBubble($"No ore stockpile in range.");
                                }

                                DropResGameObject.Drop(__instance.tf.position, item,
                                    __instance.tf.parent,
                                    to_right ? __instance.anim_direction.ClockwiseDir() : __instance.anim_direction,
                                    3f,
                                    UnityEngine.Random.Range(0, 2), false, false);

                                __instance.SetOverheadItem(null);
                            }
                            else
                            {
                                
                                switch (freeOreSlots)
                                {
                                    case 0:
                                        DropResGameObject.Drop(__instance.tf.position, item,
                                            __instance.tf.parent,
                                            to_right ? __instance.anim_direction.ClockwiseDir() : __instance.anim_direction,
                                            3f,
                                            UnityEngine.Random.Range(0, 2), false, false);
                                        __instance.SetOverheadItem(null);
                                        break;
                                    case 1:
                                        oWgo.PutToAllPossibleInventories(itemsToInsert, out itemsDidntFit);
                                        __instance.SetOverheadItem(null);
                                        freeOreSlots = -1;
                                        __instance.ShowCustomNeedBubble("Ore storage is now full!");

                                        item.definition.item_size = 1;
                                        DropCollectGUI.OnDropCollected(item);
                                        item.definition.item_size = 2;
                                        Sounds.PlaySound("pickup", null, false, 0f);
                                        break;
                                }

                                switch (freeOreSlots)
                                {
                                    case > 0:
                                    {
                                        oWgo.PutToAllPossibleInventories(itemsToInsert, out itemsDidntFit);
                                        __instance.SetOverheadItem(null);

                                        item.definition.item_size = 1;
                                        DropCollectGUI.OnDropCollected(item);
                                        item.definition.item_size = 2;
                                        Sounds.PlaySound("pickup", null, false, 0f);

                                        if (itemsDidntFit.Count > 0)
                                        {
                                            __instance.ShowCustomNeedBubble("Can't do that, have no storage left!");
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                        else if (itemIsLog)
                        {
                            itemsToInsert.Clear();
                            itemsDidntFit.Clear();
                            itemsToInsert.Add(item);

                            if (tWgo == null)
                            {
                                if (!isAttacking)
                                {
                                    __instance.ShowCustomNeedBubble($"No timber stockpile in range.");
                                }

                                DropResGameObject.Drop(__instance.tf.position, item,
                                    __instance.tf.parent,
                                    to_right ? __instance.anim_direction.ClockwiseDir() : __instance.anim_direction,
                                    3f,
                                    UnityEngine.Random.Range(0, 2), false, false);
                                __instance.SetOverheadItem(null);
                            }
                            else
                            {
                                switch (freeTimberSlots)
                                {
                                    case 0:
                                        DropResGameObject.Drop(__instance.tf.position, item,
                                            __instance.tf.parent,
                                            to_right
                                                ? __instance.anim_direction.ClockwiseDir()
                                                : __instance.anim_direction,
                                            3f,
                                            UnityEngine.Random.Range(0, 2), false, false);
                                        __instance.SetOverheadItem(null);
                                        break;
                                    case 1:
                                        tWgo.PutToAllPossibleInventories(itemsToInsert, out itemsDidntFit);
                                        __instance.SetOverheadItem(null);
                                        freeTimberSlots = -1;
                                        __instance.ShowCustomNeedBubble("Timber storage is now full!");
                                        item.definition.item_size = 1;
                                        DropCollectGUI.OnDropCollected(item);
                                        item.definition.item_size = 2;
                                        Sounds.PlaySound("pickup", null, false, 0f);
                                        break;
                                }

                                switch (freeTimberSlots)
                                {
                                    case > 0:
                                    {
                                        tWgo.PutToAllPossibleInventories(itemsToInsert, out itemsDidntFit);
                                        __instance.SetOverheadItem(null);
                                        item.definition.item_size = 1;
                                        DropCollectGUI.OnDropCollected(item);
                                        item.definition.item_size = 2;
                                        Sounds.PlaySound("pickup", null, false, 0f);
                                        if (itemsDidntFit.Count > 0)
                                        {
                                            __instance.ShowCustomNeedBubble("Can't do that, have no storage left!");
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                        else if (itemIsStone)
                        {
                            itemsToInsert.Clear();
                            itemsDidntFit.Clear();
                            itemsToInsert.Add(item);

                            if (sWgo == null)
                            {
                                if (!isAttacking)
                                {
                                    __instance.ShowCustomNeedBubble($"No stone stockpile in range.");
                                }

                                DropResGameObject.Drop(__instance.tf.position, item,
                                    __instance.tf.parent,
                                    to_right ? __instance.anim_direction.ClockwiseDir() : __instance.anim_direction, 3f,
                                    UnityEngine.Random.Range(0, 2), false, false);
                                __instance.SetOverheadItem(null);
                            }
                            else
                            {
                                switch (freeStoneSlots)
                                {
                                    case 0:
                                        DropResGameObject.Drop(__instance.tf.position, item,
                                            __instance.tf.parent,
                                            to_right ? __instance.anim_direction.ClockwiseDir() : __instance.anim_direction,
                                            3f,
                                            UnityEngine.Random.Range(0, 2), false, false);
                                        __instance.SetOverheadItem(null);
                                        break;
                                    case 1:
                                        oWgo.PutToAllPossibleInventories(itemsToInsert, out itemsDidntFit);
                                        __instance.SetOverheadItem(null);
                                        freeStoneSlots = -1;
                                        __instance.ShowCustomNeedBubble("Stone storage is now full!");
                                        item.definition.item_size = 1;
                                        DropCollectGUI.OnDropCollected(item);
                                        item.definition.item_size = 2;
                                        Sounds.PlaySound("pickup", null, false, 0f);
                                        break;
                                }

                                switch (freeStoneSlots)
                                {
                                    case > 0:
                                    {
                                        oWgo.PutToAllPossibleInventories(itemsToInsert, out itemsDidntFit);
                                        __instance.SetOverheadItem(null);
                                        item.definition.item_size = 1;
                                        DropCollectGUI.OnDropCollected(item);
                                        item.definition.item_size = 2;
                                        Sounds.PlaySound("pickup", null, false, 0f);
                                        if (itemsDidntFit.Count > 0)
                                        {
                                            __instance.ShowCustomNeedBubble("Can't do that, have no storage left!");
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            DropResGameObject.Drop(__instance.tf.position, item, __instance.tf.parent,
                                to_right ? __instance.anim_direction.ClockwiseDir() : __instance.anim_direction, 3f,
                                UnityEngine.Random.Range(0, 2), false, false);
                            __instance.SetOverheadItem(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //FileLog.Log($"AutoLootHeavies: {ex.Source} - {ex.Message} - {ex.StackTrace}");
                }
            }
        }
    }
}