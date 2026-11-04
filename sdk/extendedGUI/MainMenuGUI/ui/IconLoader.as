package ui
{
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   
   public class IconLoader
   {
      protected static var iconLoader:Loader = new Loader();
      
      protected static var callbacks:Object = new Object();
      
      public function IconLoader()
      {
         super();
      }
      
      public static function loadIconByPath(image_path:String, callback:Function) : *
      {
         var url:URLRequest = new URLRequest(image_path);
         var handler:Function = function(e:Event):void
         {
            doneLoad(iconLoader,image_path,callback);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         iconLoader.load(url);
      }
      
      protected static function doneLoad(ldr:Loader, image_path:String, callback:Function) : *
      {
         callback(Bitmap(ldr.content).bitmapData);
      }
   }
}

