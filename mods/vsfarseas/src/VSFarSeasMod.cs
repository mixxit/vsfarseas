using Vintagestory.API.Common;
using Foundation.Extensions;
using System;
using Vintagestory.API.Server;
using vsfarseas.src;
using Vintagestory.API.MathTools;

namespace vsFarSeas.src
{
    public class VSFarSeasMod : ModSystem
    {
        private ICoreAPI api;

        public override void StartPre(ICoreAPI api)
        {
            VSFarSeasModConfigFile.Current = api.LoadOrCreateConfig<VSFarSeasModConfigFile>(typeof(VSFarSeasMod).Name + ".json");
            api.World.Config.SetInt("returnVesselTimeInSeconds", VSFarSeasModConfigFile.Current.ReturnVesselTimeInSeconds);
            base.StartPre(api);
            api.RegisterItemClass("requisition", typeof(ItemRequisition));
        }


        public override void Start(ICoreAPI api)
        {
            this.api = api;
            base.Start(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.RegisterCommand("createrandomrequisition", "Creates a random requisition", "", CmdCreateRandomRequisition, "root");
            base.StartServerSide(api);
        }

        private void CmdCreateRandomRequisition(IServerPlayer player, int groupId, CmdArgs args)
        {
            try
            {
                string requisitionBody = "This is a requisition body";
                Item item = player.Entity.World.GetItem(new AssetLocation("requisition:requisition"));
                if (!(item is ItemRequisition))
                {
                    player.SendMessage(groupId, "Could not find requisition when creating", EnumChatType.CommandError);
                    return;
                }

                ItemStack itemStack = new ItemStack(item);
                ((ItemRequisition)itemStack.Item).SetRandomRequisition(itemStack);

                player.Entity.TryGiveItemStack(itemStack);
                player.SendMessage(groupId, $"Generating Random Requisition Body", EnumChatType.CommandSuccess);
            }
            catch (Exception e)
            {
                player.SendMessage(groupId, e.Message, EnumChatType.CommandError);
                return;
            }



        }
    }

    public class VSFarSeasModConfigFile
    {
        public static VSFarSeasModConfigFile Current { get; set; }
        public int ReturnVesselTimeInSeconds = 300;
    }
}
