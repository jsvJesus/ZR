package ui.components
{
   import com.dvalimona.components.*;
   import flash.display.*;
   import flash.events.*;
   
   public class Diagrid extends Sprite
   {
      private var _border:Number = 1;
      
      private var _borderAlpha:Number = 0.1;
      
      private var _width:Number;
      
      private var _height:Number;
      
      private var data:BitmapData;
      
      public function Diagrid()
      {
         this.data = new component_diagonal_grid_png() as BitmapData;
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      override public function get width() : Number
      {
         return this._width;
      }
      
      override public function set width(value:Number) : void
      {
         this._width = value;
         this.invalidate();
      }
      
      override public function get height() : Number
      {
         return this._height;
      }
      
      override public function set height(value:Number) : void
      {
         this._height = value;
         this.invalidate();
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.invalidate();
      }
      
      private function invalidate() : void
      {
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
      }
      
      protected function onEnterFrame(event:Event) : void
      {
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.draw();
      }
      
      private function draw() : void
      {
         this.graphics.clear();
         this.graphics.beginBitmapFill(this.data,null,true,false);
         this.graphics.drawRect(0,0,this.width,this.height);
         this.graphics.endFill();
         this.graphics.lineStyle(this._border,16777215,this._borderAlpha,false,LineScaleMode.NONE,CapsStyle.SQUARE,JointStyle.MITER);
         this.graphics.moveTo(this._border / 2,this._border / 2);
         this.graphics.lineTo(this.width - this._border,this._border / 2);
         this.graphics.lineTo(this.width - this._border,this.height - this._border);
         this.graphics.lineTo(this._border / 2,this.height - this._border);
         this.graphics.lineTo(this._border / 2,this._border / 2);
         this.dispatchEvent(new Event(Component.DRAW));
      }
   }
}

