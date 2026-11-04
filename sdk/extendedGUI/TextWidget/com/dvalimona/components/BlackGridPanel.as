package com.dvalimona.components
{
   import flash.display.*;
   
   public class BlackGridPanel extends Panel
   {
      private var backgroundBitmap:BitmapData;
      
      public function BlackGridPanel(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         this.backgroundBitmap = new linegrid_black_png() as BitmapData;
         super(param1,param2,param3);
      }
      
      override public function draw() : void
      {
         super.draw();
         _background.graphics.clear();
         _background.graphics.lineStyle(1,0,0.1);
         _background.graphics.beginFill(Style.BLACK_PANEL_COLOR,0.7);
         _background.graphics.drawRect(0,0,_width,_height);
         _background.graphics.beginBitmapFill(this.backgroundBitmap);
         _background.graphics.drawRect(0,0,_width,_height);
         _background.graphics.endFill();
         drawGrid();
      }
   }
}

