package communication
{
   import events.*;
   import flash.events.*;
   import logging.*;
   
   public class ScreenMode extends EventDispatcher
   {
      private static var _self:ScreenMode;
      
      public var state:int = -1;
      
      public function ScreenMode(arg1:IEventDispatcher = null)
      {
         super(arg1);
      }
      
      public static function get self() : ScreenMode
      {
         if(!_self)
         {
            _self = new ScreenMode();
            Api.self.addEventListener(Api.SCREEN_MODE,onUpdateModeHandler);
         }
         return _self;
      }
      
      public static function Init() : void
      {
         self;
         Logger.LogToChannel(Logger.WARNING,"ScreenMode.Init",self);
      }
      
      protected static function onUpdateModeHandler(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.WARNING,"onUpdateModeHandler",arg1.data.answer.state);
         if(arg1.data.answer.state != null)
         {
            self.state = arg1.data.answer.state;
            Settings.self.addEventListener(Settings.READY,onSettingsReady);
            Settings.UpdateData();
         }
      }
      
      protected static function onSettingsReady(arg1:Event) : void
      {
         Settings.self.removeEventListener(Settings.READY,onSettingsReady);
         self.dispatchEvent(new Event(Event.CHANGE));
      }
   }
}

