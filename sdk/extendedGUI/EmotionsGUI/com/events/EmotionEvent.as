package com.events
{
   import flash.display.MovieClip;
   import flash.events.Event;
   
   public class EmotionEvent extends Event
   {
      public static const CLICK:* = "Click";
      
      public static const CHECK:* = "CHECK";
      
      public static const KEY_BIND:* = "KEY_BIND";
      
      public var sender:MovieClip;
      
      public var id:Number;
      
      public var check:Boolean;
      
      public var index:Number;
      
      public function EmotionEvent(type:String, sender:MovieClip = null, id:Number = -1, check:Boolean = false, index:Number = 0)
      {
         super(type,false,false);
         this.id = id;
         this.sender = sender;
         this.check = check;
         this.index = index;
      }
      
      override public function toString() : String
      {
         return formatToString("EmotionEvent","type","sender");
      }
      
      override public function clone() : Event
      {
         return new EmotionEvent(type,this.sender,this.id,this.check,this.index);
      }
   }
}

