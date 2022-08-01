using Steamworks;

namespace AW2.Core
{
  public class SteamComponent : AWGameComponent
  {
    public SteamComponent(AssaultWingCore game, int updateOrder)
        : base(game, updateOrder)
    {
    }
 
    public override void Update()
    {
      if (!Game.Services.GetService<SteamApiService>().Initialized)
      {
        return;
      }

      SteamAPI.RunCallbacks();
    }

  }
}