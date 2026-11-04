package communication
{
   import events.*;
   import flash.events.*;
   import logging.*;
   
   public class Settings extends EventDispatcher
   {
      public static var Tune:SettingsObject;
      
      public static var Data:Object;
      
      public static var Ranges:Object;
      
      public static var Default:Object;
      
      public static var Order:Array;
      
      private static var _self:Settings;
      
      public static const READY:String = "settings_loaded";
      
      public static const KEYBINDS:String = "keybinds";
      
      Order = [];
      
      public function Settings(param1:IEventDispatcher = null)
      {
         super();
      }
      
      public static function get IsReady() : Boolean
      {
         return Data != null && Ranges != null && Default != null;
      }
      
      public static function Update() : void
      {
         UpdateData();
         UpdateRanges();
         UpdateDefaults();
      }
      
      public static function UpdateData() : void
      {
         Data = null;
         Api.self.addEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Api.call(Api.GET_SETTINGS,[]);
      }
      
      public static function UpdateRanges() : void
      {
         Ranges = null;
         Api.self.addEventListener(Api.GET_SETTINGS_RANGE,onGetSettingsRangeHandler);
         Api.call(Api.GET_SETTINGS_RANGE,[]);
      }
      
      public static function UpdateDefaults() : void
      {
         Default = null;
         Api.self.addEventListener(Api.GET_DEFAULT_SETTINGS,onGetDefaultSettingsHandler);
         Api.call(Api.GET_DEFAULT_SETTINGS,[]);
      }
      
      public static function ApplyDefaultSettings(param1:Array) : void
      {
      }
      
      public static function GetSettings() : void
      {
         Data = null;
         Api.self.addEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Api.call(Api.GET_SETTINGS,[]);
      }
      
      protected static function onGetSettingsHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Data = param1.data.answer;
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetSettingsHandler",param1.data.answer,param1.data.error);
         dispatchReadyState();
      }
      
      protected static function onGetSettingsRangeHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SETTINGS_RANGE,onGetSettingsRangeHandler);
         Ranges = param1.data.answer;
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetSettingsRangeHandler",param1.data.answer);
         dispatchReadyState();
      }
      
      protected static function onGetDefaultSettingsHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetDefaultSettingsHandler",param1.data.answer,param1.data.error);
         Api.self.removeEventListener(Api.GET_DEFAULT_SETTINGS,onGetDefaultSettingsHandler);
         Default = param1.data.answer;
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

