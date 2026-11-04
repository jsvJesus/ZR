package com.controls
{
   public class Card
   {
      public var id:Number;
      
      public var little:LittleCard;
      
      public var full:Array;
      
      public var title:String;
      
      public function Card(param1:Number, param2:String, param3:Array, param4:String = "")
      {
         super();
         this.id = param1;
         this.title = param2;
         this.little = new LittleCard(param1,param2,param4);
         this.full = param3;
      }
   }
}

