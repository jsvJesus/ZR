package ui.components
{
   import com.dvalimona.components.*;
   import flash.display.*;
   import flash.events.*;
   
   public class BlackPanel extends Sprite
   {
      private var _border:Number = 1;
      
      private var _borderAlpha:Number = 0.05;
      
      private var _width:Number = 0;
      
      private var _height:Number = 0;
      
      public function BlackPanel()
      {
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         super();
      }
      
      override public function get width() : Number
      {
         return this._width;
      }
      
      override public function set width(param1:Number) : void
      {
         this._width = param1;
         this.invalidate();
      }
      
      override public function get height() : Number
      {
         return this._height;
      }
      
      override public function set height(param1:Number) : void
      {
         this._height = param1;
         this.invalidate();
      }
      
      protected function onAddedToStage(param1:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.invalidate();
      }
      
      private function invalidate() : void
      {
         this.addEventListener(Event.ENTER_FRAME,this.onEnterFrame);
      }
      
      protected function onEnterFrame(param1:Event) : void
      {
         this.removeEventListener(Event.ENTER_FRAME,this.onEnterFrame);
         this.draw();
      }
      
      private function draw() : void
      {
         this.graphics.clear();
         this.graphics.beginFill(0,0.5);
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

