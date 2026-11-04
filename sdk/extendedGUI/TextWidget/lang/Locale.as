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
      
      public var supportUrl:String;
      
      public function Locale(param1:Object)
      {
         var config:Object = null;
         var loc1:* = undefined;
         var arg1:Object = param1;
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
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,error.toString());
            throw error;
         }
      }
      
      private static function parseLocalesList(param1:Object) : void
      {
         var _loc2_:* = null;
         var _loc3_:* = null;
         Logger.LogToChannel(Logger.DEBUG,"Locale.parseLocalesList");
         var _loc4_:* = 0;
         var _loc5_:* = param1.answer;
         for(_loc2_ in _loc5_)
         {
            Logger.LogToChannel(Logger.DEBUG,"\tLocale.parseLocalesList add locale:",_loc2_);
            _loc3_ = new Locale(param1.answer[_loc2_]);
            locales.push(_loc3_);
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
      
      protected static function getCurrentLocaleHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_CURRENT_LOCALE,getCurrentLocaleHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.getCurrentLocaleHandler",param1.data.name,param1.data.answer.locale);
         initialized = true;
         setLocaleById(param1.data.answer.locale,false,false);
      }
      
      public static function LookInKeybindsTable(param1:String) : String
      {
         return keybinds[param1];
      }
      
      protected static function loadKeybindsLocTableHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_KEYBIND_LOC_TABLE,loadKeybindsLocTableHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadKeybindsLocTableHandler",param1.data.name);
         keybinds = param1.data.answer;
      }
      
      public static function load(param1:Array, param2:Boolean = false) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.load",param1);
         Locale.filesToLoad = param1;
         if(param2)
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
      
      protected static function loadLocalizedResourceHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOCALIZED_RESOURCE,loadLocalizedResourceHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadLocalizedResourceHandler",param1.data.name);
         if(Boolean(current) && Boolean(current.id))
         {
            resources[current.id] = param1.data.answer;
         }
         else
         {
            resources = param1.data.answer;
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
      
      public static function setLocaleById(param1:String, param2:Boolean = true, param3:Boolean = true, param4:Boolean = true) : void
      {
         var _loc5_:* = null;
         Logger.LogToChannel(Logger.DEBUG,"Locale.setLocaleById",param1,param2,param3,param4);
         var _loc6_:* = 0;
         var _loc7_:* = locales;
         for each(_loc5_ in _loc7_)
         {
            if(_loc5_.id == param1)
            {
               current = _loc5_;
               if(param3)
               {
                  Api.call(Api.SET_CURRENT_LOCALE,[{"id":current.id}]);
               }
               if(param4)
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
      
      public static function getById(param1:String) : String
      {
         var id:String = null;
         var path:Array = null;
         var result:Object = null;
         var target:Object = null;
         var i:int = 0;
         var loc1:* = undefined;
         var arg1:String = param1;
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
            trace("error: ",error);
            return id;
         }
      }
      
      public static function AddItem(param1:Component) : void
      {
         items[param1] = param1;
         UpdateItem(param1);
      }
      
      public static function RemoveItem(param1:Component) : void
      {
         delete items[param1];
      }
      
      public static function init() : void
      {
         Locale.core = new EventDispatcher();
         Logger.LogToChannel(Logger.DEBUG,"Locale.init");
         loadLocalesList();
      }
      
      private static function UpdateItem(param1:Component) : void
      {
         Logger.LogToChannel(Logger.LOCALIZATION,param1,param1.$,getById(param1.$));
         param1.updateLocale(getById(param1.$));
      }
      
      public static function Update() : void
      {
         var _loc1_:Component = null;
         var _loc2_:* = undefined;
         _loc1_ = null;
         var _loc3_:* = 0;
         var _loc4_:* = items;
         for each(_loc1_ in _loc4_)
         {
         }
      }
      
      public static function get current() : Locale
      {
         return _current;
      }
      
      public static function set current(param1:Locale) : void
      {
         _current = param1;
      }
      
      private static function loadLocalesList() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadLocalesList");
         Api.self.addEventListener(Api.GET_LOCALES_LIST,localesListHandler);
         Api.call(Api.GET_LOCALES_LIST);
      }
      
      protected static function localesListHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOCALES_LIST,localesListHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.localesListHandler",param1.data,param1.data.name);
         parseLocalesList(param1.data);
         getCurrentLocale();
      }
   }
}

