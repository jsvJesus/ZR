package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   
   public class ClearButton extends PushButton
   {
      public function ClearButton(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null)
      {
         super(parent,xpos,ypos,label,defaultHandler);
      }
      
      override protected function addChildren() : void
      {
         _back = new Sprite();
         _back.mouseEnabled = false;
         addChild(_back);
         _face = new Sprite();
         _face.mouseEnabled = false;
         addChild(_face);
         _label = new Label(this,0,0,"",22);
         _label.clipContent = true;
         addEventListener(MouseEvent.MOUSE_DOWN,onMouseGoDown);
         addEventListener(MouseEvent.ROLL_OVER,onMouseOver);
         this.upColorAlpha = 0;
         this.downColorAlpha = 0;
         this.overColorAlpha = 0;
         this.labelUpColor = 5626367;
         this.labelOverColor = 10347511;
         this.labelDownColor = 2855599;
         drawFace();
      }
   }
}

