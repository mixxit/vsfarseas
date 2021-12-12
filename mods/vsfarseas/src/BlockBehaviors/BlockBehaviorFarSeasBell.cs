using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using vsfarseas.src.BlockEntities;
using vsFarSeas.src;

namespace vsfarseas.src.BlockBehaviors
{
    public class BlockBehaviorFarSeasBell : BlockBehavior
    {
        public BlockBehaviorFarSeasBell(Block block) : base(block)
        {

        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            BlockEntity block = byPlayer.Entity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (block != null && !(block is BlockEntityFarSeasBell))
            {
                base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
                return true;
            }

            if (world.Side == EnumAppSide.Client)
            {
                handling = EnumHandling.PreventDefault;
                return true;
            }
            IServerPlayer player = (IServerPlayer)(byPlayer.Entity as EntityPlayer).Player;
            VSFarSeasMod mod = world.Api.ModLoader.GetModSystem<VSFarSeasMod>();

            if (((BlockEntityFarSeasBell)block).GetLastRung() > 0 && (((BlockEntityFarSeasBell)block).GetLastRung()+(mod.GetReturnVesselTimeInSeconds())) > DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("vsfarseas:bellrungtoosoon"), EnumChatType.CommandError);
                handling = EnumHandling.PreventDefault;
                return false;
            }

            ((BlockEntityFarSeasBell)block).SetLastRung(DateTimeOffset.Now.ToUnixTimeSeconds());

            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("vsfarseas:bellrung",mod.GetFarSeasBellChestDistance()), EnumChatType.OwnMessage);
            world.PlaySoundAt(new AssetLocation("game:sounds/creature/bell/bell.ogg"), player, null, false, 32, 3);

            CollectAndSendAllCargo(world, player, blockSel.Position, mod.GetFarSeasBellChestDistance());

            handling = EnumHandling.PreventDefault;
            return true;
        }

        private void CollectAndSendAllCargo(IWorldAccessor world, IServerPlayer senderPlayer, Vintagestory.API.MathTools.BlockPos position, int distance)
        {
            List<BlockPos> containersPoss = GetNearbyContainersPositions(world, position, distance);
            var items = new Dictionary<string, int>();
            var requisitions = new List<string>();
            
            foreach (var containerPos in containersPoss)
            {
                BlockEntityContainer containerEntity = world.BlockAccessor.GetBlockEntity(containerPos) as BlockEntityContainer;
                if (containerEntity != null && containerEntity.GetContentStacks() != null)
                {
                    foreach(var itemStack in containerEntity.GetContentStacks())
                    {
                        if (itemStack == null || itemStack.Item == null)
                            continue;

                        if (itemStack.Item is ItemRequisition)
                        {
                            senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Found Requisition", EnumChatType.OwnMessage);
                            requisitions.Add(((ItemRequisition)itemStack.Item).GetRequisitionJson(itemStack));
                            continue;
                        }

                        if (!items.ContainsKey(itemStack.Item.Code.ToString()))
                            items.Add(itemStack.Item.Code.ToString(), 0);

                        items[itemStack.Item.Code.ToString()] += itemStack.StackSize;
                    }
                }

                world.BlockAccessor.SetBlock(0, containerPos);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(containerPos);
            }

            PlaceNewRequisitionChests(senderPlayer,containersPoss);
            RewardRequisitions(senderPlayer, containersPoss, requisitions, items);
            
            senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Sent {containersPoss.Count()} containers", EnumChatType.OwnMessage);
        }

        private void PlaceNewRequisitionChests(IServerPlayer senderPlayer, List<BlockPos> positionsToPlaceContainers)
        {
            var requisitionItemStack = new ItemStack(senderPlayer.Entity.World.GetItem(new AssetLocation("vsfarseas:requisition")));
            for (int i = 0; i < senderPlayer.Entity.World.Rand.Next(1, 3); i++)
            {
                var itemStack = requisitionItemStack.Clone();
                ((ItemRequisition)itemStack.Item).SetRandomRequisition(itemStack);
                AddChestRewards(senderPlayer.Entity.World, positionsToPlaceContainers, itemStack, 1);
            }
        }

        private void RewardRequisitions(IServerPlayer senderPlayer, List<BlockPos> positionsToPlaceContainers, List<string> requisitionJsons, Dictionary<string, int> items)
        {
            if (requisitionJsons == null || items == null || requisitionJsons.Count() < 1 || items.Count() < 1)
                return;
            VSFarSeasMod mod = senderPlayer.Entity.World.Api.ModLoader.GetModSystem<VSFarSeasMod>();

            float grandTotal = 0;
            foreach (var requisitionJson in requisitionJsons)
            {
                Dictionary<string, int> requisitions = JsonConvert.DeserializeObject<Dictionary<string, int>>(requisitionJson);
                if (requisitions.Count() < 1)
                    continue;

                bool anyRequisitionItemFailed = false;
                float total = 0;
                foreach(var requisitionItem in requisitions.Keys)
                {
                    if (!items.ContainsKey(requisitionItem))
                    {
                        anyRequisitionItemFailed = true;
                        continue;
                    }
                    
                    var achievedQty = 0;
                    var itemPrice = mod.GetTradeableItems()[requisitionItem];
                    

                    // Has to be full shipment, for bonus reward
                    if (items[requisitionItem] < requisitions[requisitionItem])
                    {
                        achievedQty = items[requisitionItem];
                        items[requisitionItem] -= items[requisitionItem];
                        anyRequisitionItemFailed = true;
                        continue;
                    } else
                    {
                        achievedQty = requisitions[requisitionItem];
                        items[requisitionItem] -= requisitions[requisitionItem];
                    }

                    total += itemPrice * achievedQty;
                }

                total = (float)Math.Floor(total);

                float bonus = 0;
                if (!anyRequisitionItemFailed)
                    bonus = (total / 100) * 10;

                bonus = (float)Math.Floor(bonus);

                if (anyRequisitionItemFailed)
                    senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Partially completed a requisition! Reward: {total}", EnumChatType.OwnMessage);
                else
                    senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Fully completed a requisition! Reward: {total} + Bonus: {bonus}", EnumChatType.OwnMessage);

                float coinCount = total + bonus;
                grandTotal += coinCount;
            }

            var coinItemStack = new ItemStack(senderPlayer.Entity.World.GetItem(new AssetLocation("vsfarseas:coin")));
            AddChestRewards(senderPlayer.Entity.World, positionsToPlaceContainers, coinItemStack, (int)grandTotal);
        }

        private void AddChestRewards(IWorldAccessor world, List<BlockPos> positionsToPlaceContainers, ItemStack itemStack, int countLeftToReward)
        {
            if (positionsToPlaceContainers == null || positionsToPlaceContainers.Count() < 1)
                return;

            if (countLeftToReward < 1)
                return;

            if (itemStack == null)
                return;

            // Add to inventory
            foreach (var chestPosition in positionsToPlaceContainers)
            {
                if (countLeftToReward < 1)
                    break;

                var blockEntity = world.BlockAccessor.GetBlockEntity(chestPosition);

                // Something else is there
                if (blockEntity != null && !(blockEntity is BlockEntityGenericTypedContainer))
                    continue;

                if (blockEntity == null)
                {
                    Block chest = world.GetBlock(new AssetLocation("game:chest-east"));
                    world.BlockAccessor.SetBlock(chest.BlockId, chestPosition);
                    world.BlockAccessor.SpawnBlockEntity(chest.EntityClass, chestPosition);
                    blockEntity = (BlockEntityGenericTypedContainer)world.BlockAccessor.GetBlockEntity(chestPosition);
                    if (((BlockEntityGenericTypedContainer)blockEntity).Inventory.FirstEmptySlot() == null)
                        continue;

                    blockEntity.MarkDirty();
                }

                // Something else is there
                if (blockEntity != null && !(blockEntity is BlockEntityGenericTypedContainer))
                    continue;

                if (countLeftToReward > itemStack.Item.MaxStackSize)
                    itemStack.StackSize = itemStack.Item.MaxStackSize;
                else
                    itemStack.StackSize = countLeftToReward;

                countLeftToReward -= itemStack.StackSize;

                var slotId = ((BlockEntityGenericTypedContainer)blockEntity).Inventory.GetSlotId(((BlockEntityGenericTypedContainer)blockEntity).Inventory.FirstEmptySlot());
                ((BlockEntityGenericTypedContainer)blockEntity).Inventory[slotId].Itemstack = itemStack;
                ((BlockEntityGenericTypedContainer)blockEntity).Inventory[slotId].MarkDirty();
            }
        }

        List<BlockPos> GetNearbyContainersPositions(IWorldAccessor world, BlockPos pos, int distance)
        {
            List<BlockPos> positions = new List<BlockPos>();
            int lowerRange = (distance / 2)*-1;
            int upperRange = (distance / 2);

            for (int dx = lowerRange; dx <= upperRange; dx++)
            {
                for (int dy = lowerRange; dy <= upperRange; dy++)
                {
                    for (int dz = lowerRange; dz <= upperRange; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;

                        BlockPos targetPos = pos.AddCopy(dx, dy, dz);

                        BlockEntityContainer container = world.BlockAccessor.GetBlockEntity(targetPos) as BlockEntityContainer;
                        if (container == null)
                            continue;

                        positions.Add(targetPos);
                    }
                }
            }

            return positions;
        }
    }
}
