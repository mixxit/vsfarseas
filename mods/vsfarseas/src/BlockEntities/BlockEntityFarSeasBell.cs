using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using vsFarSeas.src;

namespace vsfarseas.src.BlockEntities
{
    public class BlockEntityFarSeasBell : BlockEntity
    {
        long lastRung = 0;
        PendingOutboundCargo pendingOutboundCargo;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server)
                RegisterGameTickListener(OnServerTick, 8000);
        }

        private void OnServerTick(float dt)
        {
            
            if (!TraderHasReturned())
                return;

            HandlePendingCargo();
        }

        private bool TraderHasReturned()
        {
            if (GetLastRung() < 1)
                return false;

            VSFarSeasMod mod = Api.ModLoader.GetModSystem<VSFarSeasMod>();
            var expectedTime = GetLastRung() + (mod.GetReturnVesselTimeInSeconds());
            var timeNow = DateTimeOffset.Now.ToUnixTimeSeconds();

            return timeNow >= expectedTime;
        }

        private void HandlePendingCargo()
        {
            if (pendingOutboundCargo == null)
                return;

            if (Api is ICoreServerAPI)
                ((ICoreServerAPI)Api).SendMessageToGroup(GlobalConstants.InfoLogChatGroup, $"The Trader has arrived", EnumChatType.Notification);

            PlaceNewRequisitionChests(pendingOutboundCargo.GetOldContainerPositions());
            RewardRequisitions(pendingOutboundCargo.GetOldContainerPositions(), pendingOutboundCargo.GetRequisitions(), pendingOutboundCargo.GetItems());

            ResetCargo();
        }

        private void PlaceNewRequisitionChests(List<BlockPos> positionsToPlaceContainers)
        {
            var requisitionItemStack = new ItemStack(Api.World.GetItem(new AssetLocation("vsfarseas:requisition")));
            for (int i = 0; i < Api.World.Rand.Next(1, 3); i++)
            {
                var itemStack = requisitionItemStack.Clone();
                ((ItemRequisition)itemStack.Item).SetRandomRequisition(itemStack);
                AddChestRewards(Api.World, positionsToPlaceContainers, itemStack, 1);
            }
        }

        private void RewardRequisitions(List<BlockPos> positionsToPlaceContainers, List<string> requisitionJsons, Dictionary<string, int> items)
        {
            if (requisitionJsons == null || items == null || requisitionJsons.Count() < 1 || items.Count() < 1)
                return;
            VSFarSeasMod mod = Api.ModLoader.GetModSystem<VSFarSeasMod>();

            float grandTotal = 0;
            foreach (var requisitionJson in requisitionJsons)
            {
                Dictionary<string, int> requisitions = JsonConvert.DeserializeObject<Dictionary<string, int>>(requisitionJson);
                if (requisitions.Count() < 1)
                    continue;

                bool anyRequisitionItemFailed = false;
                float total = 0;
                foreach (var requisitionItem in requisitions.Keys)
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
                    }
                    else
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

                if (anyRequisitionItemFailed && Api is ICoreServerAPI)
                    ((ICoreServerAPI)Api).SendMessageToGroup(GlobalConstants.InfoLogChatGroup, $"Partially completed a requisition! Reward: {total}", EnumChatType.Notification);

                if (!anyRequisitionItemFailed && Api is ICoreServerAPI)
                    ((ICoreServerAPI)Api).SendMessageToGroup(GlobalConstants.InfoLogChatGroup, $"Fully completed a requisition! Reward: {total} + Bonus: {bonus}", EnumChatType.Notification);

                float coinCount = total + bonus;
                grandTotal += coinCount;
            }

            var coinItemStack = new ItemStack(Api.World.GetItem(new AssetLocation("vsfarseas:coin")));
            AddChestRewards(Api.World, positionsToPlaceContainers, coinItemStack, (int)grandTotal);
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

        private void ResetCargo()
        {
            pendingOutboundCargo = null;
            lastRung = 0;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetLong("lastRung", lastRung);
            tree.SetString("pendingCargo", JsonConvert.SerializeObject(pendingOutboundCargo));
        }

        public void SetLastRung(long lastRung)
        {
            this.lastRung = lastRung;
            this.MarkDirty();
        }

        public long GetLastRung()
        {
            return lastRung;
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            lastRung = tree.GetLong("lastRung");

            if (!String.IsNullOrEmpty(tree.GetString("pendingOutboundCargo")))
                pendingOutboundCargo = JsonConvert.DeserializeObject<PendingOutboundCargo>(tree.GetString("pendingOutboundCargo"));
            else
                pendingOutboundCargo = null;


        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
        }

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
        }

        internal void SetPendingCargo(PendingOutboundCargo pendingOutboundCargo)
        {
            this.pendingOutboundCargo = pendingOutboundCargo;
            this.MarkDirty();
        }
    }
}

