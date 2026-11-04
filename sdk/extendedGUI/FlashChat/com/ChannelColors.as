package com
{
   public class ChannelColors
   {
      public static var whispID:int;
      
      protected static var _colors:Object = new Object();
      
      protected static var _default_colors:Object = new Object();
      
      public function ChannelColors()
      {
         super();
      }
      
      public static function setColors(arr:Array) : *
      {
         var i:* = undefined;
         for(i in arr)
         {
            _default_colors[arr[i].id.toString()] = uint(arr[i].color);
            _colors[arr[i].id.toString()] = uint(arr[i].color);
         }
      }
      
      public static function resetColors() : *
      {
         var i:* = undefined;
         _colors = new Object();
         for(i in _default_colors)
         {
            _colors[i] = _default_colors[i];
         }
      }
      
      public static function changeColors(arr:Array) : *
      {
         var i:* = undefined;
         for(i in arr)
         {
            _colors[arr[i].id.toString()] = uint(arr[i].color);
         }
      }
      
      public static function setColor(id:int, color:uint) : *
      {
         _colors[id.toString()] = color;
      }
      
      public static function getAt(id:int) : uint
      {
         if(_colors[id.toString()] != null)
         {
            return _colors[id.toString()];
         }
         if(id == -99)
         {
            return _colors[whispID.toString()];
         }
         return 0;
      }
      
      public static function getAllColors() : Array
      {
         var i:* = undefined;
         var str:String = null;
         var arr:Array = new Array();
         for(i in _colors)
         {
            str = "0x" + _colors[i].toString(16);
            arr.push({
               "id":i,
               "color":str
            });
         }
         return arr;
      }
   }
}

