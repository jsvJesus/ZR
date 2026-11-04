package communication
{
   import events.ApiEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   import logging.Logger;
   
   public class Settings extends EventDispatcher
   {
      public static var Tune:SettingsObject;
      
      public static var Data:Object;
      
      public static var Ranges:Object;
      
      public static var Default:Object;
      
      private static var _self:Settings;
      
      public static const READY:String = "settings_loaded";
      
      public static const KEYBINDS:String = "keybinds";
      
      public static var Order:Array = [];
      
      public function Settings(target:IEventDispatcher = null)
      {
         super();
      }
      
      public static function get IsReady() : Boolean
      {
         return Data != null && Ranges != null;
      }
      
      public static function Update() : void
      {
         Data = null;
         Ranges = null;
         Default = null;
         Api.self.addEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Api.self.addEventListener(Api.GET_SETTINGS_RANGE,onGetSettingsRangeHandler);
         Api.self.addEventListener(Api.GET_DEFAULT_SETTINGS,onGetDefaultSettingsHandler);
         Api.call(Api.GET_SETTINGS,[]);
         Api.call(Api.GET_SETTINGS_RANGE,[]);
         Api.call(Api.GET_DEFAULT_SETTINGS,[]);
      }
      
      public static function ApplyDefaultSettings(targetPath:Array) : void
      {
      }
      
      public static function GetSettings() : void
      {
         Data = null;
         Api.self.addEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Api.call(Api.GET_SETTINGS,[]);
      }
      
      protected static function onGetSettingsHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Data = event.data.answer;
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetSettingsHandler",event.data.answer,event.data.error);
         dispatchReadyState();
      }
      
      protected static function onGetSettingsRangeHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SETTINGS_RANGE,onGetSettingsRangeHandler);
         Ranges = event.data.answer;
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetSettingsRangeHandler",event.data.answer);
         dispatchReadyState();
      }
      
      protected static function onGetDefaultSettingsHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetDefaultSettingsHandler",event.data.answer,event.data.error);
         Api.self.removeEventListener(Api.GET_DEFAULT_SETTINGS,onGetDefaultSettingsHandler);
         Default = event.data.answer;
         dispatchReadyState();
      }
      
      protected static function dispatchReadyState() : void
      {
         if(Settings.IsReady)
         {
            Settings.self.dispatchEvent(new Event(READY));
         }
      }
      
      public static function get self() : Settings
      {
         if(!_self)
         {
            _self = new Settings();
         }
         return _self;
      }
   }
}

