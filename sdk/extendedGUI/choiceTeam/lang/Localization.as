package lang
{
   import com.adobe.serialization.json.JSONDecoder;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.utils.Dictionary;
   import logging.Logger;
   import utils.ExtendedLoader;
   
   public class Localization extends EventDispatcher
   {
      private static var locales:Vector.<Locale>;
      
      private static var _localizationsPath:String;
      
      private static var _settingsPath:String;
      
      private static var _path:String;
      
      private static var _self:Localization;
      
      public static const LOCALE_CHANGED:String = "localization_locale_changed";
      
      private static var SETTINGS_PATH_DEBUG:String = "";
      
      private static var SETTINGS_PATH_SCALEFORM:String = "../../game/";
      
      private static var SETTINGS_RESOURCE:String = "settings.json";
      
      private static var LOCALES_PATH_DEBUG:String = "/res/local/";
      
      private static var LOCALES_PATH_SCALEFORM:String = "../../res/local/";
      
      private static var LOCALIZATIONS_RESOURCE:String = "localizations.json";
      
      private static var LOCALE_RESOURCE:String = "";
      
      public static var DEFAULT_LOCALE:Locale = new Locale({
         "caption":"Русский",
         "id":"ru",
         "path":"Russian",
         "icon":""
      });
      
      private static var _locale:Locale = DEFAULT_LOCALE;
      
      private static var items:Dictionary = new Dictionary(true);
      
      public function Localization()
      {
         super();
      }
      
      public static function get LOCALIZATIONS_PATH() : String
      {
         try
         {
         }
         catch(error:Error)
         {
         }
         if(!_localizationsPath)
         {
            _localizationsPath = !StalkerBase.isScaleform ? LOCALES_PATH_DEBUG : LOCALES_PATH_SCALEFORM;
         }
         return _localizationsPath;
      }
      
      public static function set LOCALIZATIONS_PATH(value:String) : void
      {
         _localizationsPath = value;
      }
      
      public static function get LOCALIZATIONS_FULL_PATH() : String
      {
         return LOCALES_PATH + LOCALIZATIONS_RESOURCE;
      }
      
      public static function get SETTINGS_PATH() : String
      {
         try
         {
         }
         catch(error:Error)
         {
         }
         if(!_settingsPath)
         {
            _settingsPath = !StalkerBase.isScaleform ? SETTINGS_PATH_DEBUG : SETTINGS_PATH_SCALEFORM;
         }
         return _settingsPath;
      }
      
      public static function set SETTINGS_PATH(value:String) : void
      {
         _settingsPath = value;
      }
      
      public static function get SETTINGS_FULL_PATH() : String
      {
         return SETTINGS_PATH + SETTINGS_RESOURCE;
      }
      
      public static function get LOCALES_PATH() : String
      {
         try
         {
         }
         catch(error:Error)
         {
         }
         if(!_path)
         {
            _path = !StalkerBase.isScaleform ? LOCALES_PATH_DEBUG : LOCALES_PATH_SCALEFORM;
         }
         return _path;
      }
      
      public static function set LOCALES_PATH(value:String) : void
      {
         _path = value;
      }
      
      public static function get LOCALE_FULL_PATH() : String
      {
         return LOCALES_PATH + locale.path + "/" + LOCALE_RESOURCE;
      }
      
      public static function get self() : Localization
      {
         if(!_self)
         {
            _self = new Localization();
         }
         return _self;
      }
      
      public static function set self(value:Localization) : void
      {
         _self = value;
      }
      
      public static function get locale() : Locale
      {
         return _locale;
      }
      
      public static function set locale(value:Locale) : void
      {
         _locale = value;
      }
      
      public static function GET(id:String) : String
      {
         trace("GET",locale);
         return locale.getById(id);
      }
      
      public static function Add(item:*) : void
      {
         items[item] = item;
         item.updateLocalized();
      }
      
      public static function Remove(item:*) : void
      {
         delete items[item];
      }
      
      public static function Update() : void
      {
         var item:* = undefined;
         for each(item in items)
         {
            try
            {
               item.updateLocalized();
            }
            catch(error:Error)
            {
               Logger.LogToChannel(Logger.LOCALIZATION,"Error while update localized:",error);
            }
         }
      }
      
      public static function init(resourceName:String) : void
      {
         LOCALE_RESOURCE = resourceName;
         Logger.LogToChannel(Logger.LOCALIZATION,"Init localization..");
         Logger.LogToChannel(Logger.LOCALIZATION,"Localization path:",LOCALES_PATH);
         Logger.LogToChannel(Logger.LOCALIZATION,"Localization resource:",LOCALE_RESOURCE);
         loadLocalesList();
      }
      
      private static function checkLocaleInSettings() : void
      {
         var loader:ExtendedLoader = new ExtendedLoader();
         Logger.LogToChannel(Logger.LOCALIZATION,"Try to load settings from: [",SETTINGS_FULL_PATH,"]");
         loader.load(SETTINGS_FULL_PATH,function(data:String):void
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Localizations settings loaded successfully. [",data.length," bytes ]");
            parseSettings(data);
         },function(event:Event):void
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Localizations settings not loaded:",event);
         });
      }
      
      private static function parseSettings(data:String) : void
      {
         var settings:Object = null;
         try
         {
            settings = new JSONDecoder(data,true).getValue();
            Logger.LogToChannel(Logger.LOCALIZATION,"Settings parsed successfully.");
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Error while settings parsing.");
         }
         if(settings && settings.language && Boolean(String(settings.language).length))
         {
            setLocaleBySettingsLanguage(settings.language);
         }
         else
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Settings have not locale.");
         }
         trace(settings.language);
      }
      
      private static function setLocaleBySettingsLanguage(string:String) : void
      {
         var loc:Locale = null;
         var changed:Boolean = false;
         for each(loc in locales)
         {
            trace(loc.path,loc.id,string,String(loc.path).toLowerCase() == string.toLocaleLowerCase(),String(loc.id).toLowerCase() == string.toLocaleLowerCase());
            if(String(loc.path).toLowerCase() == string.toLocaleLowerCase() || String(loc.id).toLowerCase() == string.toLocaleLowerCase())
            {
               trace("loc:",loc.path);
               Localization.locale = loc;
               changed = true;
               break;
            }
         }
         if(changed)
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Locale set to:",Localization.locale.caption);
         }
         else
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Locale set to default:",Localization.locale.caption);
         }
         loadLocale();
      }
      
      private static function loadLocalesList() : void
      {
         var loader:ExtendedLoader = new ExtendedLoader();
         Logger.LogToChannel(Logger.LOCALIZATION,"Try to load localizations list from: [",LOCALIZATIONS_FULL_PATH,"]");
         loader.load(LOCALIZATIONS_FULL_PATH,function(data:String):void
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Localizations list loaded successfully. [",data.length," bytes ]");
            parseLocalizations(data);
         },function(event:Event):void
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Localizations list not loaded:",event);
         });
      }
      
      private static function parseLocalizations(data:String) : void
      {
         var localizations:Object = null;
         var i:uint = 0;
         var newLocale:Locale = null;
         try
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Try to parse localisations JSON..");
            localizations = new JSONDecoder(data,true).getValue();
            Logger.LogToChannel(Logger.LOCALIZATION,"Localizations parsed successfully.");
            locales = new Vector.<Locale>();
            for(i = 0; i < localizations.length; i++)
            {
               trace("loc",localizations[i]);
               newLocale = new Locale(localizations[i]);
               Logger.LogToChannel(Logger.LOCALIZATION,"\t:",newLocale.caption,newLocale.id);
               locales.push(newLocale);
            }
            Logger.LogToChannel(Logger.LOCALIZATION,"Total locales:",locales.length);
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Error while localizations parsing.");
         }
         if(localizations != null && Boolean(locales.length))
         {
            checkLocaleInSettings();
         }
         trace(localizations);
      }
      
      private static function loadLocale() : void
      {
         var loader:ExtendedLoader = new ExtendedLoader();
         Logger.LogToChannel(Logger.LOCALIZATION,"Try to load locale from: [",LOCALE_FULL_PATH,"]");
         loader.load(LOCALE_FULL_PATH,function(data:String):void
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Localization resource loaded successfully: [",data.length," bytes ]");
            parseLocale(data);
            trace("JSON locale object:[",locale.resource != null,"]");
         },function(event:Event):void
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Localization resource not loaded:",event);
         });
      }
      
      private static function parseLocale(data:String) : void
      {
         var localizations:Object = null;
         try
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Try to parse locale JSON..");
            locale.resource = new JSONDecoder(data,true).getValue();
            Logger.LogToChannel(Logger.LOCALIZATION,"Locale JSON parsed successfully: [",locale.resource,"]");
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.LOCALIZATION,"Error while locale JSON parsing.");
         }
      }
   }
}

