package com.colorPicker
{
   import com.dvalimona.components.Component;
   import flash.display.*;
   import flash.events.*;
   
   public class ColorItem extends Component
   {
      internal const COLOR_WIDTH:* = 24;
      
      internal const COLOR_HEIGHT:* = 24;
      
      public var background:Sprite = new Sprite();
      
      public var Frame:Sprite = new Sprite();
      
      public var FrameSelected:Sprite = new Sprite();
      
      protected var _color:uint = 0;
      
      protected var _selected:Boolean = false;
      
      public function ColorItem(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
         this.drawFrame(this.Frame);
         this.drawFrame(this.FrameSelected);
         addChild(this.background);
         addChild(this.Frame);
         addChild(this.FrameSelected);
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
      
      private function drawFrame(frame:Sprite) : *
      {
         frame.graphics.beginFill(16777215,0.9);
         frame.graphics.drawRect(0,0,1,this.COLOR_HEIGHT);
         frame.graphics.drawRect(0,0,this.COLOR_WIDTH,1);
         frame.graphics.drawRect(this.COLOR_WIDTH - 1,0,1,this.COLOR_HEIGHT);
         frame.graphics.drawRect(0,this.COLOR_HEIGHT - 1,this.COLOR_WIDTH,1);
         frame.graphics.endFill();
      }
      
      public function set color(col:uint) : *
      {
         this._color = col;
         this.background.graphics.clear();
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

