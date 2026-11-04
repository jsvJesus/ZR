package
{
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   import lang.Localization;
   import logging.Logger;
   import ui.ScreenNavigatorItem;
   import ui.screens.ChoiceTeamScreen;
   
   [SWF(frameRate="60",width="1024",height="768",backgroundColor="0x000000")]
   public class choiceTeam extends StalkerBase
   {
      public static const VERSION:String = "1.0.0";
      
      public function choiceTeam()
      {
         super();
         Logger.Log("Choice Team; version: ",VERSION,";");
      }
      
      override public function reset() : void
      {
         Localization.init("ChoiceTeam.json");
         initKeyboard();
         initStage();
         initAppBackground();
         initParallaxBackground();
         initPerspectiveFollower();
         initLogger();
      }
      
      override protected function addScreens() : void
      {
         var choiceTeamScreen:ChoiceTeamScreen = new ChoiceTeamScreen();
         choiceTeamScreen.addEventListener(ChoiceTeamScreen.ON_RED,this.makeRedChoice);
         choiceTeamScreen.addEventListener(ChoiceTeamScreen.ON_BLUE,this.makeBlueChoice);
         navigator.addScreen(choiceTeamScreen.id,new ScreenNavigatorItem(choiceTeamScreen));
         setTimeout(navigator.showScreen,3000,choiceTeamScreen.id);
      }
      
      protected function makeRedChoice(... args) : void
      {
         trace("makeRedChoice");
         if(ExternalInterface.available)
         {
            ExternalInterface.call("choiceTeam",1);
         }
         else
         {
            Logger.LogToChannel(Logger.ERROR,"External Interface not available!");
         }
      }
      
      protected function makeBlueChoice(... args) : void
      {
         trace("makeBlueChoice");
         if(ExternalInterface.available)
         {
            ExternalInterface.call("choiceTeam",2);
         }
         else
         {
            Logger.LogToChannel(Logger.ERROR,"External Interface not available!");
         }
      }
      
      protected function onRedHandler(sender:*) : void
      {
         trace("onRedHandler");
         Logger.LogToChannel(Logger.DEBUG,"RED picked");
         this.makeRedChoice();
      }
      
      protected function onBlueHandler(sender:*) : void
      {
         trace("onBlueHandler");
         Logger.LogToChannel(Logger.DEBUG,"BLUE picked");
         this.makeBlueChoice();
      }
   }
}

