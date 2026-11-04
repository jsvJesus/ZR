package com
{
   import flash.events.Event;
   
   public class ResizeFrameEvent extends Event
   {
      public static const RESIZE:String = "resize";
      
      public var width:Number;
      
      public var height:Number;
      
      public var x:Number;
      
      public var y:Number;
      
      public function ResizeFrameEvent(type:String, width:Number, height:Number, x:Number = 0, y:Number = 0)
      {
         super(type,false,false);
         this.width = width;
         this.height = height;
         this.x = x;
         this.y = y;
      }
      
      override public function toString() : String
      {
         return formatToString("ResizeFrameEvent","type","width","height");
      }
   }
}

