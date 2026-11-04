package com.dvalimona.components
{
   import flash.display.BitmapData;
   import flash.display.DisplayObjectContainer;
   
   public class BlackGridPanel extends Panel
   {
      private var backgroundBitmap:BitmapData = new linegrid_black_png() as BitmapData;
      
      public function BlackGridPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      override public function draw() : void
      {
         super.draw();
         _background.graphics.clear();
         _background.graphics.lineStyle(1,0,0.1);
         _background.graphics.beginBitmapFill(this.backgroundBitmap);
         _background.graphics.drawRect(0,0,_width,_height);
         _background.graphics.endFill();
         drawGrid();
      }
   }
}

