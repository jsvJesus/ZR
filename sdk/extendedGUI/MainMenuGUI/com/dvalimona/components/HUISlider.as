package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   
   public class HUISlider extends UISlider
   {
      public function HUISlider(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null, valueVisible:Boolean = false)
      {
         _sliderClass = HSlider;
         super(parent,xpos,ypos,label,defaultHandler);
         _valueLabel.visible = valueVisible;
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         _valueLabel.align = Label.RIGHT;
         _valueLabel.width = 35;
         _valueLabel.autoSize = false;
         _valueLabel.mouseChildren = true;
         _valueLabel.mouseEnabled = true;
         this.mouseChildren = true;
         this.mouseEnabled = true;
      }
      
      override protected function unfreeze() : void
      {
         super.unfreeze();
      }
      
      public function valueSilence(value:Number) : void
      {
         _slider.valueSilence = value;
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(200,18);
      }
      
      override protected function positionLabel() : void
      {
         _valueLabel.x = -40;
         _valueLabel.y = height / 2 - _valueLabel.height / 2;
      }
      
      override public function draw() : void
      {
         super.draw();
         _slider.x = 0;
         _slider.y = height / 2 - _slider.height / 2;
         _slider.height = height;
         _slider.width = width;
         this.positionLabel();
      }
   }
}

