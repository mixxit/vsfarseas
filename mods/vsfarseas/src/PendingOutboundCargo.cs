using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace vsfarseas.src
{
    internal class PendingOutboundCargo
    {
        private List<string> requisitions = new List<string>();
        private List<BlockPos> oldContainerPositions = new List<BlockPos>();
        private Dictionary<string, int> items = new Dictionary<string, int>();

        public PendingOutboundCargo(List<string> requisitions, Dictionary<string, int> items, List<BlockPos> oldContainerPositions)
        {
            this.requisitions = requisitions;
            this.items = items;
            this.oldContainerPositions = oldContainerPositions;
        }

        public List<BlockPos> GetOldContainerPositions()
        {
            return this.oldContainerPositions;
        }

        public List<string> GetRequisitions()
        {
            return this.requisitions;
        }

        public Dictionary<string,int> GetItems()
        {
            return this.items;
        }
    }
}