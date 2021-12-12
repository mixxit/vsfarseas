using Vintagestory.API.Common;
using Foundation.Extensions;
using System;
using Vintagestory.API.Server;
using vsfarseas.src;
using Vintagestory.API.MathTools;
using vsfarseas.src.BlockBehaviors;
using vsfarseas.src.BlockEntities;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using System.Collections.Generic;

namespace vsFarSeas.src
{
    public class VSFarSeasMod : ModSystem
    {
        private ICoreAPI api;
        

        public override void StartPre(ICoreAPI api)
        {
            VSFarSeasModConfigFile.Current = api.LoadOrCreateConfig<VSFarSeasModConfigFile>(typeof(VSFarSeasMod).Name + ".json");
            api.World.Config.SetInt("returnVesselTimeInSeconds", VSFarSeasModConfigFile.Current.ReturnVesselTimeInSeconds);
            api.World.Config.SetInt("farSeasBellChestDistance", VSFarSeasModConfigFile.Current.FarSeasBellChestDistance);
            base.StartPre(api);
            api.RegisterItemClass("requisition", typeof(ItemRequisition));
            api.RegisterBlockBehaviorClass("BlockBehaviorFarSeasBell", typeof(BlockBehaviorFarSeasBell));
            api.RegisterBlockEntityClass("BlockEntityFarSeasBell", typeof(BlockEntityFarSeasBell));
        }

        public int GetReturnVesselTimeInSeconds()
        {
            return api.World.Config.GetInt("returnVesselTimeInSeconds");
        }
        public int GetFarSeasBellChestDistance()
        {
            return api.World.Config.GetInt("farSeasBellChestDistance");
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

            // Load all prices from Traders
            LoadTradeAbleItems();
        }

        private void LoadTradeAbleItems()
        {
            VSFarSeasModPrices.Instance.TradeableItems = new Dictionary<string, float>();
            foreach (EntityProperties type in api.World.EntityTypes)
            {
                if (!type.Class.Equals("EntityTrader"))
                    continue;

                if (!type.Server.Attributes.HasAttribute("tradeProps"))
                    continue;

                if (type.Server.Attributes["tradeProps"] == null)
                    continue;

                var tradeProps = new JsonObject(type.Server.Attributes["tradeProps"].ToJsonToken()).AsObject<TradeProperties>();

                foreach (var tradeItem in tradeProps.Buying.List)
                {
                    if (VSFarSeasModPrices.Instance.TradeableItems.ContainsKey(tradeItem.Code?.Domain + ":" + tradeItem.Code?.Path))
                        continue;

                    var price = tradeItem.Price.nextFloat();
                    VSFarSeasModPrices.Instance.TradeableItems.Add(tradeItem.Code?.Domain + ":" + tradeItem.Code?.Path, price);
                }
            }
        }

        public override double ExecuteOrder()
        {
            /// Worldgen:
            /// - GenTerra: 0 
            /// - RockStrata: 0.1
            /// - Deposits: 0.2
            /// - Caves: 0.3
            /// - Blocklayers: 0.4
            /// Asset Loading
            /// - Json Overrides loader: 0.05
            /// - Load hardcoded mantle block: 0.1
            /// - Block and Item Loader: 0.2
            /// - Recipes (Smithing, Knapping, Clayforming, Grid recipes, Alloys) Loader: 1
            /// 
            return 1.1;
        }

        private void CmdCreateRandomRequisition(IServerPlayer player, int groupId, CmdArgs args)
        {
            try
            {
                Item item = player.Entity.World.GetItem(new AssetLocation("vsfarseas:requisition"));
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

        internal Dictionary<string,float> GetTradeableItems()
        {
            return VSFarSeasModPrices.Instance.TradeableItems;
        }
    }

    public class VSFarSeasModConfigFile
    {
        public static VSFarSeasModConfigFile Current { get; set; }
        public int ReturnVesselTimeInSeconds = 300;
        public int FarSeasBellChestDistance = 5;
    }

    public class VSFarSeasModPrices
    {
        private static readonly Lazy<VSFarSeasModPrices> lazy =
        new Lazy<VSFarSeasModPrices>(() => new VSFarSeasModPrices());
        public Dictionary<string, float> TradeableItems { get; set; }

        public static VSFarSeasModPrices Instance { get { return lazy.Value; } }

        private VSFarSeasModPrices()
        {
        }
    }
}
