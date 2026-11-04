package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   
   public class BlackPanel extends Panel
   {
      public function BlackPanel(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         super(param1,param2,param3);
      }
      
      override public function draw() : void
      {
         super.draw();
         _background.graphics.clear();
         _background.graphics.lineStyle(1,0,0.1);
         _background.graphics.beginFill(Style.BLACK_PANEL_COLOR,Style.BLACK_PANEL_ALPHA);
         _background.graphics.drawRect(0,0,_width,_height);
         _background.graphics.endFill();
         drawGrid();
      }
   }
}

