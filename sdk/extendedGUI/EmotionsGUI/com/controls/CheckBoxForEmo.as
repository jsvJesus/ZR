package com.controls
{
   import flash.text.TextField;
   import scaleform.clik.controls.Button;
   
   public class CheckBoxForEmo extends Button
   {
      public var text:TextField;
      
      public function CheckBoxForEmo()
      {
         super();
      }
      
      override protected function initialize() : void
      {
         super.initialize();
         _toggle = true;
      }
      
      override protected function updateText() : void
      {
         if(_label != null && this.text != null)
         {
            this.text.text = _label;
         }
      }
      
      override public function get autoRepeat() : Boolean
      {
         return false;
      }
      
      override public function set autoRepeat(value:Boolean) : void
      {
      }
      
      override public function get toggle() : Boolean
      {
         return true;
      }
      
      override public function set toggle(value:Boolean) : void
      {
      }
      
      override public function toString() : String
      {
         return "[CLIK CheckBoxForEmo " + name + "]";
      }
   }
}

