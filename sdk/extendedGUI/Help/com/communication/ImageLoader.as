package com.communication
{
   import flash.display.Bitmap;
   import flash.display.Loader;
   import flash.events.Event;
   import flash.net.URLRequest;
   
   public class ImageLoader
   {
      protected static var Count:Number;
      
      protected static var Current:Number;
      
      protected static var DataArr:Array;
      
      protected static var Icons:Object = new Object();
      
      public function ImageLoader()
      {
         super();
      }
      
      public static function getImageByName(param1:String) : Bitmap
      {
         if(Icons[param1] != null)
         {
            return new Bitmap(Icons[param1],"auto",true);
         }
         return null;
      }
      
      public static function LoadImages(param1:Array) : *
      {
         Count = param1.length;
         Current = 0;
         DataArr = param1;
         NextIcon();
      }
      
      protected static function NextIcon() : *
      {
         var iconLoader:Loader = null;
         iconLoader = new Loader();
         var url:URLRequest = new URLRequest(DataArr[Current].path);
         var handler:Function = function(param1:Event):void
         {
            doneLoad(iconLoader,DataArr[Current].name);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         iconLoader.load(url);
      }
      
      public static function doneLoad(param1:Loader, param2:String) : *
      {
         Icons[param2] = Bitmap(param1.content).bitmapData;
         ++Current;
         if(Current < Count)
         {
            NextIcon();
         }
      }
   }
}

