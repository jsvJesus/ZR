package com.dvalimona.components
{
   import flash.display.*;
   import flash.events.*;
   import flash.text.*;
   
   public class PushButtonWithImage extends PushButton
   {
      protected var _image:Bitmap;
      
      protected var _circle:Sprite;
      
      protected var _notify:TextField;
      
      protected var _color:uint = 10830884;
      
      public var labelOffset:int = 0;
      
      public function PushButtonWithImage(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null)
      {
         super(parent,xpos,ypos);
      }
      
      override protected function init() : void
      {
         super.init();
      }
      
      override protected function addChildren() : void
      {
         _back = new Sprite();
         _back.mouseEnabled = false;
         addChild(_back);
         _face = new Sprite();
         _face.mouseEnabled = false;
         addChild(_face);
         this._image = new Bitmap();
         this._circle = new Sprite();
         this._circle.mouseEnabled = false;
         addChild(this._circle);
         this._notify = new TextField();
         var format:TextFormat = this._notify.defaultTextFormat;
         format.align = TextFormatAlign.CENTER;
         format.font = Base.FONT_REGULAR;
         format.size = 22;
         format.color = 4294967295;
         this._notify.setTextFormat(format);
         this._notify.defaultTextFormat = format;
         this._notify.width = 30;
         this._notify.height = 30;
         addChild(this._notify);
         _label = new Label(this,0,0,"",22);
         _label.clipContent = true;
         _label.mouseEnabled = true;
         addEventListener(MouseEvent.MOUSE_DOWN,onMouseGoDown);
         addEventListener(MouseEvent.ROLL_OVER,onMouseOver);
      }
      
      public function get notifyColor() : uint
      {
         return this._color;
      }
      
      public function set notifyColor(value:uint) : void
      {
         this._color = value;
         this.drawCircle();
      }
      
      public function get notifyCount() : uint
      {
         return uint;
      }
      
      public function set notifyCount(value:uint) : void
      {
         var result:String = "";
         if(value < 1)
         {
            result = "";
         }
         else if(value > 9)
         {
            result = "9+";
         }
         else
         {
            result = value.toString();
         }
         this._notify.text = result;
         this.drawCircle();
      }
      
      public function notifyText(value:String) : void
      {
         if(value != "!")
         {
            return;
         }
         this._notify.text = value;
         this.drawCircle();
      }
      
      protected function drawCircle() : *
      {
         this._circle.graphics.clear();
         this._circle.visible = this._notify.text != "";
         if(this._notify.text == "")
         {
            return;
         }
         this._circle.graphics.beginFill(this._color);
         this._circle.graphics.drawCircle(0,0,15);
         this._circle.graphics.endFill();
      }
      
      public function getImageWidth() : int
      {
         if(this.image.width > 0)
         {
            return this.image.width + 4;
         }
         return 0;
      }
      
      public function set image(value:Bitmap) : void
      {
         if(this._image != null && this._image.parent == this)
         {
            this.removeChild(this._image);
         }
         this._image = value;
         if(this._image == null)
         {
            invalidate();
            this.draw();
            this.drawFace();
            return;
         }
         this.addChild(this._image);
         this._image.y = this.height / 2 - this._image.height / 2;
         invalidate();
         this.draw();
         this.drawFace();
      }
      
      public function get image() : Bitmap
      {
         return this._image;
      }
      
      public function updateElementPositions() : void
      {
         if(this._image != null)
         {
            this._image.y = this.height / 2 - this._image.height / 2;
            this._image.x = 0;
         }
         switch(this.align)
         {
            case Label.LEFT:
               _label.x = marginLeft + this.getImageWidth() + this.labelOffset;
               _label.y = _height / 2 - _label.height / 2;
               break;
            case Label.RIGHT:
               _label.x = width - _label.width - marginRight;
               _label.y = _height / 2 - _label.height / 2;
               break;
            case Label.CENTER:
               _label.x = _width / 2 - this.getImageWidth() / 2 - _label.width / 2;
               _label.y = _height / 2 - _label.height / 2;
         }
         if(this._circle != null)
         {
            this._circle.x = _label.x + _label.width + 8;
            this._circle.y = _label.y;
            if(this._notify != null)
            {
               this._notify.x = this._circle.x - 15;
               this._notify.y = _label.y - 16;
            }
         }
      }
      
      override public function draw() : void
      {
         super.draw();
         this.updateElementPositions();
      }
      
      override protected function drawFace() : void
      {
         _face.graphics.clear();
         if(_over)
         {
            if(_down)
            {
               _label.color = labelDownColor;
               _label.alpha = labelDownColorAlpha;
               _face.graphics.beginFill(downColor,downColorAlpha);
            }
            else
            {
               _label.color = labelOverColor;
               _label.alpha = labelOverColorAlpha;
               _face.graphics.beginFill(overColor,overColorAlpha);
            }
         }
         else
         {
            _label.color = labelUpColor;
            _label.alpha = enabled ? labelColorAlpha : disabledLabelAlpha;
            _face.graphics.beginFill(upColor,upColorAlpha);
         }
         if(_autoWidth)
         {
            _width = _label.width + this.getImageWidth();
         }
         _face.graphics.drawRect(0,0,_width,_height);
         _face.graphics.endFill();
         this.updateElementPositions();
      }
      
      private function alignLeft() : void
      {
         _label.text = _labelText;
         _label.draw();
         _label.width = width - this.getImageWidth();
         if(_label.width > width - this.getImageWidth() - marginLeft - marginRight)
         {
            _label.width = width - this.getImageWidth() - marginLeft - marginRight;
         }
         _label.draw();
         _label.x = marginLeft + this.getImageWidth();
         _label.y = _height / 2 - _label.height / 2;
         _label.move(marginLeft + this.getImageWidth(),_height / 2 - _label.height / 2);
         this.updateElementPositions();
      }
      
      private function alignRight() : void
      {
         _label.text = _labelText;
         _label.draw();
         _label.width = width;
         if(_label.width > width - this.getImageWidth() - marginLeft - marginRight)
         {
            _label.width = width - this.getImageWidth() - marginLeft - marginRight;
         }
         _label.draw();
         _label.move(width - _label.width - marginRight,_height / 2 - _label.height / 2);
         this.updateElementPositions();
      }
      
      private function alignCenter() : void
      {
         _label.text = _labelText;
         _label.draw();
         _label.width = width;
         if(_label.width > width - this.getImageWidth() - marginLeft - marginRight)
         {
            _label.autoSize = false;
            _label.width = _width - this.getImageWidth() - marginLeft - marginRight;
         }
         else
         {
            _label.autoSize = true;
         }
         _label.draw();
         _label.move(_width / 2 - this.getImageWidth() / 2 - _label.width / 2,_height / 2 - _label.height / 2);
         this.updateElementPositions();
      }
   }
}

