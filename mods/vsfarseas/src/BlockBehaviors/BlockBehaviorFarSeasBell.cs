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
            List<BlockPos> containers = GetNearbyContainers(world, position, distance);

            foreach(var container in containers)
            {
                world.BlockAccessor.SetBlock(0,container);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(container);
            }

            senderPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Sent {containers.Count()} containers", EnumChatType.OwnMessage);
        }

        List<BlockPos> GetNearbyContainers(IWorldAccessor world, BlockPos pos, int distance)
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
