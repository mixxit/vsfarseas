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

            RewardRequisitions(senderPlayer, containersPoss, requisitions, items);

            senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Sent {containersPoss.Count()} containers", EnumChatType.OwnMessage);
        }

        private void RewardRequisitions(IServerPlayer senderPlayer, List<BlockPos> positionsToPlaceContainers, List<string> requisitionJsons, Dictionary<string, int> items)
        {
            if (requisitionJsons == null || items == null || requisitionJsons.Count() < 1 || items.Count() < 1)
                return;

            foreach(var requisitionJson in requisitionJsons)
            {
                Dictionary<string, int> requisitions = JsonConvert.DeserializeObject<Dictionary<string, int>>(requisitionJson);
                if (requisitions.Count() < 1)
                    continue;

                bool anyRequisitionItemFailed = false;
                foreach(var requisitionItem in requisitions.Keys)
                {
                    if (!items.ContainsKey(requisitionItem))
                    {
                        anyRequisitionItemFailed = true;
                        continue;
                    }

                    // Has to be full shipment, not partial
                    if (items[requisitionItem] < requisitions[requisitionItem])
                    {
                        anyRequisitionItemFailed = true;
                        continue;
                    }

                    items[requisitionItem] -= requisitions[requisitionItem];
                }

                if (anyRequisitionItemFailed)
                    senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Failed to complete a requisition", EnumChatType.OwnMessage);
                else
                    senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Fully completed a requisition!", EnumChatType.OwnMessage);
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
