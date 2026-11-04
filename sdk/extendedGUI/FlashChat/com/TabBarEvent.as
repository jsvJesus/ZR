package com
{
   import flash.events.Event;
   
   public class TabBarEvent extends Event
   {
      public static const RIGHT_CLICK:String = "Right_click";
      
      public var index:Number = -1;
      
      public function TabBarEvent(type:String, index:Number)
      {
         super(type,false,false);
         this.index = index;
      }
      
      override public function toString() : String
      {
         return formatToString("TabBarEvent","type","index");
      }
      
      override public function clone() : Event
      {
         return new TabBarEvent(type,this.index);
      }
   }
}

