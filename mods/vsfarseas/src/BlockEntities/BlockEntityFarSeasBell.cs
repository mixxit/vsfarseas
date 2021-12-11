using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace vsfarseas.src.BlockEntities
{
    public class BlockEntityFarSeasBell : BlockEntity
    {
        long lastRung;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetLong("lastRung", lastRung);
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
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
        }

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
        }

    }
}

