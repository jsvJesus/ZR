package ui
{
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   import flash.utils.*;
   
   public class BoosterIcons
   {
      protected static var N:Number;
      
      protected static var Current:int;
      
      protected static var Current_prem:int;
      
      protected static var Arr:Array;
      
      public static var Icons:Object = new Object();
      
      public static var Icons_prem:Object = new Object();
      
      protected static var myLoader1:Loader = new Loader();
      
      public function BoosterIcons()
      {
         super();
      }
      
      public static function byId(id:int, has_prem:Boolean) : Bitmap
      {
         var icon:Bitmap = null;
         if(has_prem)
         {
            if(Icons_prem[id.toString()] == null)
            {
               return icon;
            }
            icon = new Bitmap(Icons_prem[id.toString()]["image"],"auto",true);
            icon.name = Icons_prem[id.toString()]["name"];
         }
         else
         {
            if(Icons[id.toString()] == null)
            {
               return icon;
            }
            icon = new Bitmap(Icons[id.toString()]["image"],"auto",true);
            icon.name = Icons[id.toString()]["name"];
         }
         if(icon != null)
         {
            icon.width = 105;
            icon.height = 105;
         }
         return icon;
      }
      
      public static function LoadIcons(Paths:Array) : *
      {
         var id:Number = NaN;
         N = Paths.length;
         Current = 0;
         Current_prem = 0;
         Arr = Paths;
         NextIcon();
         NextIcon_prem();
      }
      
      protected static function NextIcon() : *
      {
         var url:URLRequest;
         var handler:Function;
         var iconLoader:Loader = null;
         var old_index:int = 0;
         if(Current >= N)
         {
            return;
         }
         iconLoader = new Loader();
         url = new URLRequest(Arr[Current].path);
         old_index = Current;
         handler = function(e:Event):void
         {
            doneLoad(false,iconLoader,Arr[old_index].ID,Arr[old_index].path);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         iconLoader.load(url);
         Current = old_index + 1;
      }
      
      protected static function NextIcon_prem() : *
      {
         var url:URLRequest;
         var handler:Function;
         var old_index:int = 0;
         var iconLoader:Loader = null;
         if(Current_prem >= N)
         {
            return;
         }
         old_index = Current_prem;
         iconLoader = new Loader();
         url = new URLRequest(Arr[old_index].path_with_prem);
         handler = function(e:Event):void
         {
            doneLoad(true,iconLoader,Arr[old_index].ID,Arr[old_index].path_with_prem);
         };
         iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,handler);
         iconLoader.load(url);
         Current_prem = old_index + 1;
      }
      
      public static function doneLoad(is_prem:Boolean, ldr:Loader, i:Number, source:String) : *
      {
         var obj:Object = new Object();
         obj["image"] = Bitmap(ldr.content).bitmapData;
         obj["name"] = source;
         if(is_prem)
         {
            Icons_prem[i.toString()] = obj;
         }
         else
         {
            Icons[i.toString()] = obj;
         }
         if(is_prem)
         {
            setTimeout(NextIcon_prem,0);
         }
         else
         {
            setTimeout(NextIcon,0);
         }
      }
   }
}

