package lang
{
   import com.dvalimona.components.Component;
   import communication.Api;
   import events.ApiEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.utils.Dictionary;
   import logging.Logger;
   
   public class Locale
   {
      private static var keybinds:Object;
      
      public static var core:EventDispatcher;
      
      public static var onSuccess:Function;
      
      private static var _current:Locale;
      
      public static const CHANGED:String = "language_changed";
      
      private static var files:Array = new Array();
      
      private static var resources:Object = new Object();
      
      private static var initialized:Boolean = false;
      
      private static var filesToLoad:Array = new Array();
      
      private static var filesLoaded:Array = new Array();
      
      public static var locales:Vector.<Locale> = new Vector.<Locale>();
      
      private static var items:Dictionary = new Dictionary();
      
      public var caption:String;
      
      public var id:String;
      
      public var shortcut:String;
      
      public var icon:String;
      
      public var supportUrl:String;
      
      public function Locale(config:Object)
      {
         super();
         try
         {
            this.caption = config.caption;
            this.id = config.id;
            this.shortcut = config.shortcut;
            this.icon = config.icon;
            this.supportUrl = config.supportUrl;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,error.toString());
            throw error;
         }
      }
      
      public static function get current() : Locale
      {
         return _current;
      }
      
      public static function set current(value:Locale) : void
      {
         _current = value;
      }
      
      public static function init() : void
      {
         Locale.core = new EventDispatcher();
         Logger.LogToChannel(Logger.DEBUG,"Locale.init");
         loadLocalesList();
      }
      
      private static function loadLocalesList() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadLocalesList");
         Api.self.addEventListener(Api.GET_LOCALES_LIST,localesListHandler);
         Api.call(Api.GET_LOCALES_LIST);
      }
      
      protected static function localesListHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOCALES_LIST,localesListHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.localesListHandler",event.data,event.data.name);
         parseLocalesList(event.data);
         getCurrentLocale();
      }
      
      private static function parseLocalesList(data:Object) : void
      {
         var lng:String = null;
         var newLocale:Locale = null;
         Logger.LogToChannel(Logger.DEBUG,"Locale.parseLocalesList");
         for(lng in data.answer)
         {
            Logger.LogToChannel(Logger.DEBUG,"\tLocale.parseLocalesList add locale:",lng);
            newLocale = new Locale(data.answer[lng]);
            locales.push(newLocale);
         }
      }
      
      private static function getCurrentLocale() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.getCurrentLocale");
         Api.self.addEventListener(Api.GET_CURRENT_LOCALE,getCurrentLocaleHandler);
         Api.call(Api.GET_CURRENT_LOCALE);
      }
      
      protected static function getCurrentLocaleHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_CURRENT_LOCALE,getCurrentLocaleHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.getCurrentLocaleHandler",event.data.name,event.data.answer.locale);
         initialized = true;
         setLocaleById(event.data.answer.locale,false,false);
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
      
      public static function LookInKeybindsTable(keybindAction:String) : String
      {
         return keybinds[keybindAction];
      }
      
      protected static function loadKeybindsLocTableHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_KEYBIND_LOC_TABLE,loadKeybindsLocTableHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadKeybindsLocTableHandler",event.data.name);
         keybinds = event.data.answer;
      }
      
      public static function load(newFilesToLoad:Array) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Locale.load",newFilesToLoad);
         Locale.filesToLoad = newFilesToLoad;
         if(!initialized)
         {
            Locale.init();
         }
      }
      
      protected static function loadFiles() : void
      {
         Api.self.addEventListener(Api.GET_LOCALIZED_RESOURCE,loadLocalizedResourceHandler);
         Api.call(Api.GET_LOCALIZED_RESOURCE,[{"paths":Locale.filesToLoad}]);
      }
      
      protected static function loadLocalizedResourceHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_LOCALIZED_RESOURCE,loadLocalizedResourceHandler);
         Logger.LogToChannel(Logger.DEBUG,"Locale.loadLocalizedResourceHandler",event.data.name);
         resources[current.id] = event.data.answer;
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
      
      public static function setLocaleById(id:String, needEvent:Boolean = true, needCall:Boolean = true, needReload:Boolean = true) : void
      {
         var locale:Locale = null;
         Logger.LogToChannel(Logger.DEBUG,"Locale.setLocaleById",id,needEvent,needCall,needReload);
         for each(locale in locales)
         {
            if(locale.id == id)
            {
               current = locale;
               if(needCall)
               {
                  Api.call(Api.SET_CURRENT_LOCALE,[{"id":current.id}]);
               }
               if(needReload)
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
      
      public static function getById(id:String) : String
      {
         var path:Array = null;
         var result:Object = null;
         var target:Object = null;
         var i:int = 0;
         try
         {
            trace("getById",id);
            path = id.split(".");
            result = null;
            target = resources[current.id];
            for(i = 0; i < path.length; i++)
            {
               if(!Boolean(target[path[i]]))
               {
                  return id;
               }
               if(target.hasOwnProperty(path[i]))
               {
                  result = target[path[i]];
                  target = result;
               }
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
      
      public static function AddItem(item:Component) : void
      {
         items[item] = item;
         UpdateItem(item);
      }
      
      public static function RemoveItem(item:Component) : void
      {
         delete items[item];
      }
      
      private static function UpdateItem(item:Component) : void
      {
         trace("UpdateItem",item,item.$,getById(item.$));
         item.updateLocale(getById(item.$));
      }
      
      public static function Update() : void
      {
         var item:Component = null;
         for each(item in items)
         {
            try
            {
               if(item && item.$ && Boolean(item.$.length))
               {
                  UpdateItem(item);
               }
            }
            catch(error:Error)
            {
               Logger.LogToChannel(Logger.LOCALIZATION,"Error while update localized:",error);
            }
         }
      }
   }
}

