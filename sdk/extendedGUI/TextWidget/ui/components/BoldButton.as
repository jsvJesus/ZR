package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.text.TextField;
   
   public class BoldButton extends PushButton
   {
      public function BoldButton(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "", param5:Function = null)
      {
         super(param1,param2,param3,param4,param5);
      }
      
      override protected function addChildren() : void
      {
         _face = new noDiagrid();
         addChild(_face);
         _label = new LabelShadowed(this);
         (_label as LabelShadowed).shadowColor = 0;
         (_label as LabelShadowed).shadowAlpha = 0.9;
         (_label as LabelShadowed).shadowSize = 1;
         _label.size = 20;
         _label.font = Base.FONT_BOLD;
         _label.clipContent = true;
         addEventListener(MouseEvent.MOUSE_DOWN,onMouseGoDown);
         addEventListener(MouseEvent.ROLL_OVER,onMouseOver);
         this.autoWidth = true;
         this.upColorAlpha = 0.5;
         this.downColorAlpha = 0.8;
         this.overColorAlpha = 1;
         this.labelUpColor = Style.MENU_LABEL_COLOR;
         this.labelOverColor = Style.MENU_LABEL_OVER_COLOR;
         this.labelDownColor = Style.MENU_LABEL_DOWN_COLOR;
         this.align = Label.LEFT;
         marginLeft = 10;
         marginRight = 10;
      }
      
      override protected function drawFace() : void
      {
         if(_over)
         {
            if(_down)
            {
               _label.color = labelDownColor;
               _label.alpha = labelDownColorAlpha;
               TweenMax.killTweensOf(_face);
               TweenMax.to(_face,0.3,{
                  "alpha":downColorAlpha,
                  "ease":Expo.easeOut
               });
            }
            else
            {
               _label.color = labelOverColor;
               _label.alpha = labelOverColorAlpha;
               TweenMax.killTweensOf(_face);
               TweenMax.to(_face,0.3,{
                  "alpha":overColorAlpha,
                  "ease":Expo.easeOut
               });
            }
         }
         else
         {
            _label.color = labelUpColor;
            _label.alpha = enabled ? labelColorAlpha : disabledLabelAlpha;
            TweenMax.killTweensOf(_face);
            TweenMax.to(_face,0.3,{
               "alpha":upColorAlpha,
               "ease":Expo.easeOut
            });
         }
         if(_autoWidth)
         {
            _width = marginLeft + _label.width + marginRight;
         }
         _face.width = _width;
         _face.height = _height;
      }
      
      public function get textField() : TextField
      {
         return _label.textField;
      }
   }
}

