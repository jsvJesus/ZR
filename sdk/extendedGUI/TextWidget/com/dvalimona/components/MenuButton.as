package com.dvalimona.components
{
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.*;
   import flash.events.*;
   
   public class MenuButton extends PushButton
   {
      public function MenuButton(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Function = null)
      {
         super(param1,param2,param3,param4,param5);
      }
      
      override protected function addChildren() : void
      {
         _back = new Sprite();
         _back.mouseEnabled = false;
         addChild(_back);
         _face = new Sprite();
         _face.mouseEnabled = false;
         addChild(_face);
         _label = new LabelShadowed(this,0,0,"");
         (_label as LabelShadowed).shadowColor = 0;
         (_label as LabelShadowed).shadowAlpha = 0.9;
         (_label as LabelShadowed).shadowSize = 1;
         _label.size = 22;
         _label.clipContent = true;
         addEventListener(MouseEvent.MOUSE_DOWN,onMouseGoDown);
         addEventListener(MouseEvent.ROLL_OVER,onMouseOver);
         this.autoWidth = true;
         this.upColorAlpha = 0;
         this.downColorAlpha = 0;
         this.overColorAlpha = 0;
         this.labelUpColor = 16777215;
         this.labelOverColor = 10347511;
         this.labelDownColor = 2855599;
         this.align = Label.LEFT;
      }
      
      override protected function drawFace() : void
      {
         _face.graphics.clear();
         if(_over)
         {
            if(_down)
            {
               TweenMax.to((_label as LabelShadowed).textField,0.1,{"ease":Expo.easeOut});
               _label.color = labelDownColor;
               _label.alpha = labelDownColorAlpha;
               _face.graphics.beginFill(downColor,downColorAlpha);
            }
            else
            {
               TweenMax.to((_label as LabelShadowed).textField,0.3,{"ease":Expo.easeOut});
               _label.color = labelOverColor;
               _label.alpha = labelOverColorAlpha;
               _face.graphics.beginFill(overColor,overColorAlpha);
            }
         }
         else
         {
            TweenMax.to((_label as LabelShadowed).textField,0.3,{"ease":Expo.easeOut});
            _label.color = labelUpColor;
            _label.alpha = enabled ? labelColorAlpha : disabledLabelAlpha;
            _face.graphics.beginFill(upColor,upColorAlpha);
         }
         if(_autoWidth)
         {
            _width = _label.width;
         }
         _face.graphics.drawRect(0,0,_width,_height);
         _face.graphics.endFill();
      }
   }
}

