package com.dvalimona.components
{
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.*;
   import flash.events.*;
   
   public class MenuButton2 extends PushButton
   {
      public var autoWidthPercentScreen:Number = 0;
      
      public var otherData:String = "";
      
      public function MenuButton2(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null)
      {
         super(parent,xpos,ypos,label,defaultHandler);
         this.autoWidth = true;
         this.upColorAlpha = 0;
         this.downColorAlpha = 0;
         this.overColorAlpha = 0;
         this.labelUpColor = 16777215;
         this.labelOverColor = 5656391;
         this.labelDownColor = 4801598;
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
         this.align = Label.LEFT;
      }
      
      override protected function drawFace() : void
      {
         _face.graphics.clear();
         if(_over || _selected)
         {
            if(_down)
            {
               TweenMax.to((_label as LabelShadowed).textField,0.1,{"ease":Expo.easeOut});
               _label.color = labelDownColor;
               _label.alpha = labelDownColorAlpha;
               if(_selected)
               {
                  _face.graphics.beginFill(downColor,1);
               }
               else
               {
                  _face.graphics.beginFill(downColor,downColorAlpha);
               }
            }
            else
            {
               TweenMax.to((_label as LabelShadowed).textField,0.3,{"ease":Expo.easeOut});
               _label.color = labelOverColor;
               _label.alpha = labelOverColorAlpha;
               if(_selected)
               {
                  _face.graphics.beginFill(downColor,0.7);
               }
               else
               {
                  _face.graphics.beginFill(overColor,overColorAlpha);
               }
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
            if(this.autoWidthPercentScreen > 0 && _selected)
            {
               _width = Base.stage.stageWidth * this.autoWidthPercentScreen;
            }
            else
            {
               _width = _label.width;
            }
         }
         _face.graphics.drawRect(0,0,_width,_height);
         _face.graphics.endFill();
      }
      
      public function set shadowAlpha(value:Number) : *
      {
         (_label as LabelShadowed).shadowAlpha = value;
      }
      
      public function set over(value:Boolean) : *
      {
         _over = value;
         this.drawFace();
      }
      
      public function get over() : Boolean
      {
         return _over;
      }
   }
}

