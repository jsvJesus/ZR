package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   
   public class BlackPanel extends Panel
   {
      public var backColor:uint = Style.BLACK_PANEL_COLOR;
      
      public function BlackPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      override public function draw() : void
      {
         super.draw();
         _background.graphics.clear();
         _background.graphics.lineStyle(1,0,0.1);
         _background.graphics.beginBitmapFill(Style.backgroundBitmap);
         _background.graphics.drawRect(0,0,_width,_height);
         _background.graphics.endFill();
         _background.alpha = Style.BLACK_PANEL_ALPHA;
         drawGrid();
      }
   }
}

