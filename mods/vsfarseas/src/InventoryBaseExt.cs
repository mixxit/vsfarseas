using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace vsfarseas.src
{
    public static class InventoryBaseExt
    {
        public static ItemSlot FirstEmptySlot(this InventoryBase inventory)
        {
            foreach (ItemSlot slot in inventory)
            {
                if (slot.Empty) return slot;
            }

            return null;
        }
    }
}
