package com.dvalimona.components
{
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.DisplayObjectContainer;
   
   public class HUISlider extends UISlider
   {
      public function HUISlider(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Function = null)
      {
         _sliderClass = HSlider;
         super(param1,param2,param3,param4,param5);
      }
      
      override protected function unfreeze() : void
      {
         super.unfreeze();
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(200,18);
      }
      
      override protected function positionLabel() : void
      {
         if(_slider.dragNow)
         {
            _valueLabel.x = _slider.handleX;
         }
         else
         {
            TweenMax.to(_valueLabel,Style.SLIDER_ANIMATION_TIME,{
               "x":_slider.handleX,
               "ease":Expo.easeOut,
               "onComplete":null
            });
         }
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

