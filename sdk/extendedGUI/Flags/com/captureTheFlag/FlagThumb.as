package com.captureTheFlag
{
   import flash.display.Sprite;
   import flash.text.TextField;
   
   public class FlagThumb extends Sprite
   {
      public var textField:TextField;
      
      public var background:Sprite;
      
      public function FlagThumb()
      {
         super();
         this.label = "A";
      }
      
      public function set label(param1:String) : *
      {
         this.textField.text = param1;
         this.textField.width = this.textField.textWidth + 8;
         this.background.width = this.textField.width;
         this.background.height = this.textField.height;
      }
      
      public function get label() : String
      {
         return this.textField.text;
      }
   }
}

