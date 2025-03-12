using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace OpenBagsOnTrade.Hooks;

[HarmonyPatch(typeof(EntityPlayerGameObject), nameof(EntityPlayerGameObject.NetworkStart))]
public class OnBeginTrade
{
    private static bool _wasCharacterWindowOpen = false;
    private static readonly List<UIBag> BagsToClose = new();
    
    private static void Postfix(EntityPlayerGameObject __instance)
    {
        // Fired in character select
        if (__instance.NetworkId.Value == 1)
        {
            return;
        }

        if (__instance.NetworkId.Value == EntityPlayerGameObject.LocalPlayerId.Value)
        {
            __instance.Trade.add_TradeSetEvent(new Action<Trade.TradeInstance>(t =>
            {
                foreach (var inventoryItem in __instance.Inventory.items)
                {
                    if (!inventoryItem.Value.IsEquippedBag() || inventoryItem.value.CorpseID != 0)
                    {
                        continue;
                    }
                    
                    UIBagManager.Instance.bagWindows.TryGetValue(inventoryItem.Value.ItemInstanceGuid, out var uiBag);

                    if (uiBag == null)
                    {
                        uiBag = UIBagManager.Instance.CreateBagWindowIfItDoesNotExist(inventoryItem.Value);
                    }

                    if (!uiBag.Window.IsVisible)
                    {
                        BagsToClose.Add(uiBag);
                    }
                }
                
                _wasCharacterWindowOpen = UICharacterPanel.Instance.IsVisible();
                UICharacterPanel.Instance.Show();
                
                foreach (var bagWindow in BagsToClose)
                {
                    bagWindow.Window.Show();
                }
            }));
            
            __instance.Trade.add_TradeClearedEvent(new Action<Trade.TradeInstance>(t =>
            {
                MelonLogger.Msg($"Closing {BagsToClose.Count} bags");
                if (!_wasCharacterWindowOpen)
                {
                    UICharacterPanel.Instance.Hide();
                }

                foreach (var bag in BagsToClose)
                {
                    bag.Window.Hide();
                }
                
                BagsToClose.Clear();
            }));
        }
    }
}