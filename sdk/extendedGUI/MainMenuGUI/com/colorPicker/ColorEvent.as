package com.colorPicker
{
   import flash.events.Event;
   
   public class ColorEvent extends Event
   {
      public static const SELECT:String = "select_color";
      
      public static const SHOW:String = "show_color";
      
      public var color:Number = 0;
      
      public var id:Number = 0;
      
      public var index:Number = 0;
      
      public var targetList:ColorList;
      
      public function ColorEvent(type:String, color:Number = 0, targetList:ColorList = null, index:Number = 0, id:Number = 0)
      {
         super(type,false,false);
         this.color = color;
         this.id = id;
         this.index = index;
         this.targetList = targetList;
      }
      
      override public function toString() : String
      {
         return formatToString("ColorEvent","type","color","index","id");
      }
      
      override public function clone() : Event
      {
         return new ColorEvent(type,this.color,this.targetList,this.index,this.id);
      }
   }
}

