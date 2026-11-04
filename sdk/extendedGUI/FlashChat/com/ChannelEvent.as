package com
{
   import flash.events.Event;
   
   public class ChannelEvent extends Event
   {
      public static const CHANGE:String = "CHANGE";
      
      public static const CHANGE_SOUND:String = "CHANGE_SOUND";
      
      public var index:Number = -1;
      
      public var id:Number = -1;
      
      public var selected:Boolean = false;
      
      public var sound:Boolean = false;
      
      public function ChannelEvent(type:String, index:Number, selected:Boolean, id:Number, sound:Boolean)
      {
         super(type,false,false);
         this.index = index;
         this.selected = selected;
         this.id = id;
         this.sound = sound;
      }
      
      override public function toString() : String
      {
         return formatToString("ChannelEvent","type","index","selected","id","sound");
      }
      
      override public function clone() : Event
      {
         return new ChannelEvent(type,this.index,this.selected,this.id,this.sound);
      }
   }
}

