package com.events
{
   import flash.events.Event;
   
   public class ScrollEvent extends Event
   {
      public static const SCROLL:* = "Scroll";
      
      public var position:Number;
      
      public function ScrollEvent(param1:String, param2:Number)
      {
         super(param1,false,false);
         this.position = param2;
      }
      
      override public function toString() : String
      {
         return formatToString("ScrollEvent","type","position");
      }
      
      override public function clone() : Event
      {
         return new ScrollEvent(type,this.position);
      }
   }
}

