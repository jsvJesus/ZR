package com.events
{
   import flash.events.Event;
   
   public class helpEvent extends Event
   {
      public static const SELECT:String = "Select_item";
      
      public static const SCROLL:String = "Scroll";
      
      public var index:Number = -1;
      
      public var parent_index:Number = -1;
      
      public function helpEvent(param1:String, param2:Number, param3:Number = 0)
      {
         super(param1,false,false);
         this.index = param3;
         this.parent_index = param2;
      }
      
      override public function toString() : String
      {
         return formatToString("helpEvent","type","parent_index","index");
      }
      
      override public function clone() : Event
      {
         return new helpEvent(type,this.parent_index,this.index);
      }
   }
}

