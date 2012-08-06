using System.Collections.Generic;
using Mogade.WindowsPhone;
using Mogade;

namespace SimpleListsOfCloud
{
   public enum Leaderboards
   {
      Main = 1,
   }
   public class MogadeHelper
   {
       //Your game key and game secret
       private const string GameKey = "501f1930563d8a7336012d2a";
       private const string Secret = "H<kW5RdisqCLQJ3pl^L<KO<>]Xx";

       public static IMogadeClient CreateInstance()
       {
           //In your own game, when you are ready, REMOVE the ContectToTest to hit the production mogade server (instead of testing.mogade.com)
           //Also, if you are upgrading from the v1 library and you were using UserId (or not doing anything), you *must* change the UniqueIdStrategy to LegacyUserId
           MogadeConfiguration.Configuration(c => c.UsingUniqueIdStrategy(UniqueIdStrategy.UserId));
           return MogadeClient.Initialize(GameKey, Secret);           
       }
   }
}