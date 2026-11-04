package com.colorPicker
{
   import flash.display.MovieClip;
   import flash.events.MouseEvent;
   
   public class ColorItem extends MovieClip
   {
      internal const COLOR_WIDTH:* = 12;
      
      internal const COLOR_HEIGHT:* = 12;
      
      public var background:MovieClip;
      
      public var Frame:MovieClip;
      
      public var FrameSelected:MovieClip;
      
      protected var _color:uint = 0;
      
      protected var _selected:Boolean = false;
      
      public function ColorItem()
      {
         super();
         this.Frame.visible = false;
         this.FrameSelected.visible = false;
         this.addEventListener(MouseEvent.MOUSE_OVER,this.onSelect);
         this.addEventListener(MouseEvent.MOUSE_OUT,this.noSelect);
         this.addEventListener(MouseEvent.CLICK,this.selectColor);
      }
      
      public function get selected() : Boolean
      {
         return this._selected;
      }
      
      public function set selected(value:Boolean) : *
      {
         if(value == this._selected)
         {
            return;
         }
         this._selected = value;
         if(value)
         {
            this.FrameSelected.visible = true;
         }
         else
         {
            this.FrameSelected.visible = false;
         }
      }
      
      public function set color(col:uint) : *
      {
         this._color = col;
         this.background.graphics.beginFill(col,1);
         this.background.graphics.drawRect(0,0,this.COLOR_WIDTH,this.COLOR_HEIGHT);
         this.background.graphics.endFill();
      }
      
      public function get color() : uint
      {
         return this._color;
      }
      
      public function getColor() : String
      {
         return "#" + this._color.toString(16);
      }
      
      protected function onSelect(e:MouseEvent) : *
      {
         this.Frame.visible = true;
      }
      
      protected function noSelect(e:MouseEvent) : *
      {
         this.Frame.visible = false;
      }
      
      protected function selectColor(e:MouseEvent) : *
      {
         dispatchEvent(new ColorEvent(ColorEvent.SELECT,this._color));
      }
   }
}

