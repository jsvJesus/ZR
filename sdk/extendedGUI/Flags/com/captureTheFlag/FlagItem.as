package com.captureTheFlag
{
   import flash.display.MovieClip;
   import flash.text.TextField;
   
   public class FlagItem extends MovieClip
   {
      public var ClanName:TextField;
      
      public var Indicator:FlagIndicator;
      
      protected var _flagName:String;
      
      protected var _value:Number;
      
      protected var _myFlag:Boolean;
      
      protected var _am_i_invader:Boolean;
      
      public function FlagItem()
      {
         super();
      }
      
      public function set flagName(param1:String) : *
      {
         if(param1 == this._flagName)
         {
            return;
         }
         this._flagName = param1;
         this.Indicator.flagName = param1;
         this.draw();
      }
      
      public function get flagName() : String
      {
         return this._flagName;
      }
      
      public function set value(param1:Number) : *
      {
         if(param1 == this._value)
         {
            return;
         }
         this._value = param1;
         this.Indicator.value = param1;
         this.draw();
      }
      
      public function get value() : Number
      {
         return this._value;
      }
      
      public function set myFlag(param1:Boolean) : *
      {
         this._myFlag = param1;
         this.ClanName.text = param1.toString();
         this.draw();
      }
      
      public function get myFlag() : Boolean
      {
         return this._myFlag;
      }
      
      public function set am_i_invader(param1:Boolean) : *
      {
         this._am_i_invader = param1;
         this.draw();
      }
      
      public function get am_i_invader() : Boolean
      {
         return this._am_i_invader;
      }
      
      protected function draw() : *
      {
         this.ClanName.width = this.ClanName.textWidth + 6;
         this.ClanName.x = -this.ClanName.width - 5;
         this.Indicator.invalidate();
      }
   }
}

