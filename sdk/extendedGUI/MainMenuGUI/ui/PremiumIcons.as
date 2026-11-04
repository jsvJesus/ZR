package ui
{
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   
   public class PremiumIcons
   {
      protected static var N:Number;
      
      protected static var cur_index:Number;
      
      protected static var icon_paths:Array;
      
      public static var Icons:Object = new Object();
      
      public static var images:Object = new Object();
      
      protected static var myLoader1:Loader = new Loader();
      
      private static var dispatcher:EventDispatcher = new EventDispatcher();
      
      public function PremiumIcons()
      {
         super();
      }
      
      public static function byId(id:int) : Bitmap
      {
         var icon:Bitmap = null;
         var icon_path:String = Icons[id.toString()];
         if(images[icon_path] != null)
         {
            icon = new Bitmap(images[icon_path],"auto",true);
            icon.name = icon_path;
         }
         else
         {
            icon = new Bitmap(new premium_active(),"auto",true);
            icon.name = "default";
            load_prem_icon(icon_path);
         }
         icon.width = 105;
         icon.height = 105;
         return icon;
      }
      
      public static function get NO_PREMIUM_ICON() : Bitmap
      {
         var icon:Bitmap = null;
         if(images["NO_PREMIUM"] != null)
         {
            icon = new Bitmap(images["NO_PREMIUM"],"auto",true);
         }
         else
         {
            icon = new Bitmap(new defaultIcon(),"auto",true);
         }
         icon.width = 105;
         icon.height = 105;
         return icon;
      }
      
      public static function load_prem_icon(icon_path:String) : *
      {
         var iconLoader:Loader = null;
         iconLoader = new Loader();
         var url:URLRequest = new URLRequest(icon_path);
         var handler:Function = function(e:Event):void
         {
            doneLoad(iconLoader,icon_path);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         iconLoader.load(url);
      }
      
      public static function LoadIcons(Paths:Array) : *
      {
         var i:* = undefined;
         cur_index = 0;
         icon_paths = new Array();
         for each(i in Paths)
         {
            Icons[i.ID.toString()] = i.path;
            if(icon_paths.indexOf(i.path) == -1)
            {
               icon_paths.push(i.path);
            }
         }
      }
      
      public static function LoadNoPremiumIcon(path:String) : *
      {
         var url:URLRequest;
         var iconLoader:Loader = null;
         iconLoader = new Loader();
         var handler:Function = function(e:Event):void
         {
            doneLoadNoPremiumIcon(iconLoader);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         url = new URLRequest(path);
         iconLoader.load(url);
      }
      
      public static function LoadSettingsBackGround() : *
      {
         var url1:URLRequest = new URLRequest("../soGUI/maps/Login/setting_back.jpg");
         myLoader1.contentLoaderInfo.addEventListener(Event.COMPLETE,doneLoadSetting);
         myLoader1.load(url1);
      }
      
      protected static function NextIcon() : *
      {
         var iconLoader:Loader = null;
         iconLoader = new Loader();
         var url:URLRequest = new URLRequest(icon_paths[cur_index]);
         var handler:Function = function(e:Event):void
         {
            doneLoad(iconLoader,icon_paths[cur_index]);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         iconLoader.load(url);
      }
      
      public static function doneLoadSetting(e:Event) : *
      {
         Icons["Setting"] = Bitmap(myLoader1.content).bitmapData;
      }
      
      internal static function doneLoadNoPremiumIcon(ldr:Loader) : *
      {
         images["NO_PREMIUM"] = Bitmap(ldr.content).bitmapData;
      }
      
      public static function doneLoad(ldr:Loader, image_path:String) : *
      {
         var obj:Object = new Object();
         images[image_path] = Bitmap(ldr.content).bitmapData;
         dispatchEvent(new Event(Event.COMPLETE));
      }
      
      public static function addEventListener(type:String, listener:Function, useCapture:Boolean = false, priority:int = 0, useWeakReference:Boolean = false) : void
      {
         dispatcher.addEventListener(type,listener,useCapture,priority,useWeakReference);
      }
      
      public static function removeEventListener(type:String, listener:Function, useCapture:Boolean = false) : void
      {
         dispatcher.removeEventListener(type,listener,useCapture);
      }
      
      public static function dispatchEvent(event:Event) : Boolean
      {
         return dispatcher.dispatchEvent(event);
      }
      
      public static function hasEventListener(type:String) : Boolean
      {
         return dispatcher.hasEventListener(type);
      }
   }
}

