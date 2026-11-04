package com.captureTheFlag
{
   import flash.display.MovieClip;
   
   public class FlagIndicator extends MovieClip
   {
      protected var _minimum:Number = 0;
      
      protected var _maximum:Number = 100;
      
      protected var _value:Number = 0;
      
      public var background:MovieClip;
      
      public var flagThumb:FlagThumb;
      
      public var flagTrack:MovieClip;
      
      public function FlagIndicator()
      {
         super();
      }
      
      public function set value(param1:Number) : *
      {
         this._value = param1;
      }
      
      public function get value() : Number
      {
         return this._value;
      }
      
      public function set flagName(param1:String) : *
      {
         this.flagThumb.label = param1;
      }
      
      public function get flagName() : String
      {
         return this.flagThumb.label;
      }
      
      protected function updateFlagThumb() : *
      {
         var _loc1_:Number = (this._value - this._minimum) / (this._maximum - this._minimum);
         this.flagThumb.x = (this.background.width - this.flagThumb.width) * _loc1_;
         this.flagTrack.width = (this.background.width - this.flagThumb.width) * _loc1_;
      }
      
      public function invalidate() : *
      {
         this.updateFlagThumb();
      }
   }
}

