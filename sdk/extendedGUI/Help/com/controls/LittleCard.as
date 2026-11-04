package com.controls
{
   public class LittleCard
   {
      protected var _title:String;
      
      protected var _text:String;
      
      protected var _see:Boolean;
      
      protected var _id:Number;
      
      public function LittleCard(param1:Number, param2:String, param3:String)
      {
         super();
         this.title = param2;
         this.text = param3;
         this.see = false;
         this.id = param1;
      }
      
      public function set title(param1:String) : void
      {
         this._title = param1;
      }
      
      public function get title() : String
      {
         return this._title;
      }
      
      public function set text(param1:String) : void
      {
         this._text = param1;
      }
      
      public function get text() : String
      {
         return this._text;
      }
      
      public function set see(param1:Boolean) : void
      {
         this._see = param1;
      }
      
      public function get see() : Boolean
      {
         return this._see;
      }
      
      public function set id(param1:Number) : void
      {
         this._id = param1;
      }
      
      public function get id() : Number
      {
         return this._id;
      }
   }
}

