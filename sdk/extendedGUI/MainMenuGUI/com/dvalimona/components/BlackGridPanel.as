package com.dvalimona.components
{
   import flash.display.*;
   
   public class BlackGridPanel extends Panel
   {
      private var backgroundBitmap:BitmapData;
      
      public function BlackGridPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         this.backgroundBitmap = new linegrid_black_png() as BitmapData;
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

