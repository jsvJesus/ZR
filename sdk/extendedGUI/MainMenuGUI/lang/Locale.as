package lang
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.events.*;
   import flash.utils.*;
   import logging.*;
   
   public class Locale
   {
      private static var files:Array;
      
      private static var resources:Object;
      
      private static var keybinds:Object;
      
      private static var filesToLoad:Array;
      
      private static var filesLoaded:Array;
      
      public static var core:EventDispatcher;
      
      public static var onSuccess:Function;
      
      private static var _current:Locale;
      
      public static var locales:Vector.<Locale>;
      
      private static var items:Dictionary;
      
      public static const CHANGED:String = "language_changed";
      
      public static const LOADED:String = "all_loaded";
      
      private static var initialized:Boolean = false;
      
      files = new Array();
      resources = new Object();
      initialized = false;
      filesToLoad = new Array();
      filesLoaded = new Array();
      locales = new Vector.<Locale>();
      items = new Dictionary();
      
      public var caption:String;
      
      public var id:String;
      
      public var shortcut:String;
      
      public var icon:String;
      
      public var registrationUrl:String;
      
      public var userUrl:String;
      
      public var supportUrl:String;
      
      public function Locale(arg1:Object)
      {
         var config:Object = null;
         var loc1:* = undefined;
         config = arg1;
         super();
         try
         {
            this.caption = config.caption;
            this.id = config.id;
            this.shortcut = config.shortcut;
            this.icon = config.icon;
            this.supportUrl = config.supportUrl;
            this.registrationUrl = config.registrationUrl;
            this.userUrl = config.addGoldUrl;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,error.toString());
            throw error;
         }
      }
      
      private static function parseLocalesList(arg1:Object) : void
      {
         var loc1:* = null;
         var loc2:* = null;
         Logger.LogToChannel(Logger.DEBUG,"Locale.parseLocalesList");
         var loc3:* = 0;
         var loc4:* = arg1.answer;
         for(loc1 in loc4)
         {
            Logger.LogToChannel(Logger.DEBUG,"\tLocale.parseLocalesList add locale:",loc1);
            loc2 = new Locale(arg1.answer[loc1]);
            locales.push(loc2);
         }
      }
      
      private static function getCurrentLocale() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.getCurrentLocale");
         Api.self.addEventListener(Api.GET_CURRENT_LOCALE,getCurrentLocaleHandler);
         Api.call(Api.GET_CURRENT_LOCALE);
      }
      
      protected static function get needLoading() : Boolean
      {
         return Boolean(Locale.filesToLoad) && Boolean(Locale.filesToLoad.length);
      }
      
      public static function LoadKeybindsLocTable() : void
      {
         Api.self.addEventListener(Api.GET_KEYBIND_LOC_TABLE,loadKeybindsLocTableHandler);
         Api.call(Api.GET_KEYBIND_LOC_TABLE,[]);
      }
      
      protected static function getCurrentLocaleHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_CURRENT_LOCALE,getCurrentLocaleHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.getCurrentLocaleHandler",arg1.data.name,arg1.data.answer.locale);
         initialized = true;
         setLocaleById(arg1.data.answer.locale,false,false);
      }
      
      public static function LookInKeybindsTable(arg1:String) : String
      {
         return keybinds[arg1];
      }
      
      protected static function loadKeybindsLocTableHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_KEYBIND_LOC_TABLE,loadKeybindsLocTableHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadKeybindsLocTableHandler",arg1.data.name);
         keybinds = arg1.data.answer;
      }
      
      public static function load(arg1:Array, arg2:Boolean = false) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.load",arg1);
         Locale.filesToLoad = arg1;
         if(arg2)
         {
            loadFiles();
         }
         else
         {
            Locale.init();
         }
      }
      
      protected static function loadFiles() : void
      {
         Api.self.addEventListener(Api.GET_LOCALIZED_RESOURCE,loadLocalizedResourceHandler);
         Api.call(Api.GET_LOCALIZED_RESOURCE,[{"paths":Locale.filesToLoad}]);
      }
      
      protected static function loadLocalizedResourceHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOCALIZED_RESOURCE,loadLocalizedResourceHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadLocalizedResourceHandler",arg1.data.name);
         if(Boolean(current) && Boolean(current.id))
         {
            resources[current.id] = arg1.data.answer;
         }
         else
         {
            resources = arg1.data.answer;
         }
         sayOkIfNeed();
         Locale.core.dispatchEvent(new Event(CHANGED));
         Locale.Update();
      }
      
      protected static function sayOkIfNeed() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.sayOkIfNeed",onSuccess != null);
         if(onSuccess != null)
         {
            onSuccess();
            onSuccess = null;
         }
      }
      
      public static function setLocaleById(arg1:String, arg2:Boolean = true, arg3:Boolean = true, arg4:Boolean = true) : void
      {
         var loc1:* = null;
         Logger.LogToChannel(Logger.DEBUG,"Locale.setLocaleById",arg1,arg2,arg3,arg4);
         var loc2:* = 0;
         var loc3:* = locales;
         for each(loc1 in loc3)
         {
            if(loc1.id == arg1)
            {
               current = loc1;
               if(arg3)
               {
                  Api.call(Api.SET_CURRENT_LOCALE,[{"id":current.id}]);
               }
               if(arg4)
               {
                  loadFiles();
               }
               else
               {
                  Locale.core.dispatchEvent(new Event(CHANGED));
               }
            }
         }
      }
      
      public static function getById(arg1:String) : String
      {
         var id:String = null;
         var path:Array = null;
         var result:Object = null;
         var target:Object = null;
         var i:int = 0;
         var loc1:* = undefined;
         path = null;
         result = null;
         target = null;
         i = 0;
         id = arg1;
         try
         {
            path = id.split(".");
            result = null;
            target = Boolean(current) && Boolean(current.id) ? resources[current.id] : resources;
            i = 0;
            while(i < path.length)
            {
               if(!target[path[i]])
               {
                  return id;
               }
               if(target.hasOwnProperty(path[i]))
               {
                  result = target[path[i]];
                  target = result;
               }
               i++;
            }
            if(result == null)
            {
               return id;
            }
            return String(result);
         }
         catch(error:Error)
         {
            return id;
         }
      }
      
      public static function AddItem(arg1:Component) : void
      {
         items[arg1] = arg1;
         UpdateItem(arg1);
      }
      
      public static function RemoveItem(arg1:Component) : void
      {
         delete items[arg1];
      }
      
      public static function init() : void
      {
         Locale.core = new EventDispatcher();
         Logger.LogToChannel(Logger.DEBUG,"Locale.init");
         loadLocalesList();
      }
      
      private static function UpdateItem(arg1:Component) : void
      {
         Logger.LogToChannel(Logger.LOCALIZATION,arg1,arg1.$,getById(arg1.$));
         arg1.updateLocale(getById(arg1.$));
      }
      
      public static function Update() : void
      {
         var item:Component = null;
         var loc1:* = undefined;
         item = null;
         var loc2:* = 0;
         var loc3:* = items;
         for each(item in loc3)
         {
         }
      }
      
      public static function get current() : Locale
      {
         return _current;
      }
      
      public static function set current(arg1:Locale) : void
      {
         _current = arg1;
      }
      
      private static function loadLocalesList() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadLocalesList");
         Api.self.addEventListener(Api.GET_LOCALES_LIST,localesListHandler);
         Api.call(Api.GET_LOCALES_LIST);
      }
      
      protected static function localesListHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOCALES_LIST,localesListHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.localesListHandler",arg1.data,arg1.data.name);
         parseLocalesList(arg1.data);
         getCurrentLocale();
      }
   }
}

