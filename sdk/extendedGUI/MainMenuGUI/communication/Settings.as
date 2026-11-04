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
      
      public function Settings(arg1:IEventDispatcher = null)
      {
         super();
         Api.self.addEventListener(Api.SET_ONE_SETTINGS,onGetOneSettings);
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
      
      public static function ApplyDefaultSettings(arg1:Array) : void
      {
      }
      
      public static function GetSettings() : void
      {
         Data = null;
         Api.self.addEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Api.call(Api.GET_SETTINGS,[]);
      }
      
      protected static function onGetSettingsHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SETTINGS,onGetSettingsHandler);
         Data = arg1.data.answer;
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetSettingsHandler",arg1.data.answer,arg1.data.error);
         dispatchReadyState();
      }
      
      protected static function onGetOneSettings(arg:ApiEvent) : void
      {
         Ranges[arg.data.answer["setting"]][arg.data.answer["type"]] = arg.data.answer["data"];
         Data[arg.data.answer["setting"]][arg.data.answer["type"]] = arg.data.answer["value"];
         Base.navigator.update_setting(arg.data.answer["setting"],arg.data.answer["type"]);
      }
      
      protected static function onGetSettingsRangeHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_SETTINGS_RANGE,onGetSettingsRangeHandler);
         Ranges = arg1.data.answer;
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetSettingsRangeHandler",arg1.data.answer);
         dispatchReadyState();
      }
      
      protected static function onGetDefaultSettingsHandler(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Settings.onGetDefaultSettingsHandler",arg1.data.answer,arg1.data.error);
         Api.self.removeEventListener(Api.GET_DEFAULT_SETTINGS,onGetDefaultSettingsHandler);
         Default = arg1.data.answer;
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

