using HarmonyLib;
using Il2Cpp;

namespace OpenBagsOnTrade.Hooks;

[HarmonyPatch(typeof(EntityPlayerGameObject), nameof(EntityPlayerGameObject.NetworkStart))]
public class OnBeginTrade
{
    private static bool WasCharacterWindowOpen = false;
    private static Dictionary<Il2CppSystem.Guid, bool> SideBagsOpen = new();
    
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
                foreach (var item in __instance.Inventory.items)
                {
                    if (item.Value.IsBag())
                    {
                        UIBagManager.Instance.CreateBagWindowIfItDoesNotExist(item.value);
                    }
                }
                WasCharacterWindowOpen = UICharacterPanel.Instance.IsVisible();
                UICharacterPanel.Instance.Show();
                
                foreach (var bagWindow in UIBagManager.Instance.bagWindows)
                {
                    SideBagsOpen[bagWindow.Key] = bagWindow.Value.Window.IsVisible;
                    bagWindow.Value.Window.Show();
                }
            }));
            
            __instance.Trade.add_TradeClearedEvent(new Action<Trade.TradeInstance>(t =>
            {
                if (!WasCharacterWindowOpen)
                {
                    UICharacterPanel.Instance.Hide();
                }

                foreach (var bag in SideBagsOpen)
                {
                    if (!SideBagsOpen.TryGetValue(bag.Key, out var wasShown))
                    {
                        continue;
                    }
                    if (!wasShown)
                    {
                        UIBagManager.Instance.bagWindows[bag.Key].Window.Hide();
                    }
                }
            }));
        }
    }
}