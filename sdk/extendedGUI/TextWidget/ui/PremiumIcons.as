package ui
{
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   
   public class PremiumIcons
   {
      protected static var N:Number;
      
      protected static var Current:Number;
      
      protected static var Arr:Array;
      
      public static var Icons:Object = new Object();
      
      protected static var myLoader1:Loader = new Loader();
      
      public function PremiumIcons()
      {
         super();
      }
      
      public static function byId(param1:int) : Bitmap
      {
         if(Icons[param1.toString()] != null)
         {
            return new Bitmap(Icons[param1.toString()],"auto",true);
         }
         return new Bitmap(new defaultIcon(),"auto",true);
      }
      
      public static function LoadIcons(param1:Array) : *
      {
         var _loc2_:Number = NaN;
         N = param1.length;
         Current = 0;
         Arr = param1;
         NextIcon();
      }
      
      public static function LoadSettingsBackGround() : *
      {
         var _loc1_:URLRequest = new URLRequest("../soGUI/maps/Login/setting_back.jpg");
         myLoader1.contentLoaderInfo.addEventListener(Event.COMPLETE,doneLoadSetting);
         myLoader1.load(_loc1_);
      }
      
      protected static function NextIcon() : *
      {
         var iconLoader:Loader = null;
         iconLoader = new Loader();
         var url:URLRequest = new URLRequest(Arr[Current].Path);
         var handler:Function = function(param1:Event):void
         {
            doneLoad(iconLoader,Arr[Current].ID);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         iconLoader.load(url);
      }
      
      public static function doneLoadSetting(param1:Event) : *
      {
         trace(myLoader1.content);
         Icons["Setting"] = Bitmap(myLoader1.content).bitmapData;
      }
      
      public static function doneLoad(param1:Loader, param2:Number) : *
      {
         Icons[param2.toString()] = Bitmap(param1.content).bitmapData;
         ++Current;
         if(Current < N)
         {
            NextIcon();
         }
      }
   }
}

