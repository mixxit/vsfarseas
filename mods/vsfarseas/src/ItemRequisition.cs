using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using vsFarSeas.src;

namespace vsfarseas.src
{
    public class ItemRequisition : Item
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.World.Side != EnumAppSide.Server)
            {
                var dlg = new GuiRequisition((ICoreClientAPI )byEntity.Api, FormatJsonAsText(GetRequisitionJson(itemslot.Itemstack)));
                dlg.TryOpen();

                handling = EnumHandHandling.PreventDefault;
                return;
            }

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            if (!(byPlayer is IServerPlayer))
            {
                return;
            }

            handling = EnumHandHandling.PreventDefault;
        }

        private string FormatJsonAsText(string json)
        {
            string output = "By order of the The Great Ocean Trading Company you are hereby ordered to deliver the following cargo:" + Environment.NewLine;
            output += Environment.NewLine;
            if (String.IsNullOrEmpty(json))
            {
                output += "No Requisitions at this time" + Environment.NewLine;
                return output;
            }

            Dictionary<string, int> requisitions = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
            if (requisitions == null || requisitions.Count() < 1)
            {
                output += "No Requisitions at this time" + Environment.NewLine;
                return output;
            }

            foreach(var key in requisitions.Keys)
            {
                Item item = api.World.GetItem(new AssetLocation(key));
                if (item == null)
                    continue;

                var name = Lang.Get(item.Code?.Domain + ":item-" + item.Code?.Path);
                output += name + " Qty: " + requisitions[key] + Environment.NewLine;
            }

            output += Environment.NewLine;
            output += "Trade Commissioner A. Berhart" + Environment.NewLine;
            return output;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return false;
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed < 1.9) return;
        }

        internal void SetRandomRequisition(ItemStack itemStack)
        {
            // Pick random items
            var requisitionSize = api.World.Rand.Next(1, 10);
            var requisitions = new Dictionary<string, int>();
            VSFarSeasMod mod = api.World.Api.ModLoader.GetModSystem<VSFarSeasMod>();


            for (int i = 0; i < requisitionSize; i++)
            {
                var requisitionItem = api.World.Items
                    .Where(x => x.Code != null && api.World.GridRecipes.Select(r => r.Output.Code?.ToString()).Contains(x.Code.ToString())) // Limits to recipe crafts only
                    .Select(e => e.Code?.Domain + ":" + e.Code?.Path).Where(w => mod.GetTradeableItems().Contains(w) && !requisitions.Keys.Contains(w)).OrderBy(x => api.World.Rand.Next()).Take(1).FirstOrDefault();
                if (requisitionItem == null)
                    continue;

                requisitions.Add(requisitionItem,api.World.Rand.Next(1,10));
            }

            SetRequisitionJson(itemStack, JsonConvert.SerializeObject(requisitions));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            dsc.Append(Lang.Get("A requisition"));
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "vsfarseas:heldhelp-readrequisition",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }

        public void SetRequisitionJson(ItemStack itemStack, string requisitionJson)
        {
            if (itemStack.Attributes != null)
            {
                itemStack.Attributes.SetString("requisitionJson", requisitionJson); 
                if (!itemStack.Attributes.HasAttribute("requisitionJson"))
                    throw new Exception("This should not happen");
            }
        }

        internal string GetRequisitionJson(ItemStack itemStack)
        {
            if (itemStack.Attributes != null)
            {
                try
                {
                    if (!itemStack.Attributes.HasAttribute("requisitionJson"))
                        return "";

                    return itemStack.Attributes.GetString("requisitionJson", "");
                }
                catch (InvalidCastException)
                {

                    return "";
                }
            }
            return "";
        }
    }
}
