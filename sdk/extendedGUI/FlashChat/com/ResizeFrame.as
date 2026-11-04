package com
{
   import flash.display.*;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.geom.*;
   
   public class ResizeFrame extends MovieClip
   {
      public var North_East:MovieClip;
      
      public var North_West:MovieClip;
      
      public var South_East:MovieClip;
      
      public var South_West:MovieClip;
      
      public var down:MovieClip;
      
      public var left:MovieClip;
      
      public var rigth:MovieClip;
      
      public var up:MovieClip;
      
      protected var _minHeight:Number = 150;
      
      protected var _maxHeight:Number = 2000;
      
      protected var _minWidth:Number = 200;
      
      protected var _maxWidth:Number = 700;
      
      public var frame:MovieClip;
      
      public var MaskUp:MovieClip;
      
      public var MaskLeft:MovieClip;
      
      protected var X:Number;
      
      protected var Y:Number;
      
      protected var XX:Number;
      
      protected var YY:Number;
      
      protected var XXwidth:Number;
      
      protected var YYheight:Number;
      
      protected var H:Number;
      
      protected var W:Number;
      
      public function ResizeFrame()
      {
         super();
         this.frame.visible = false;
         this.up.addEventListener(MouseEvent.MOUSE_OUT,this.onMaskUpOut);
         this.down.addEventListener(MouseEvent.MOUSE_OUT,this.onMaskDownOut);
         this.left.addEventListener(MouseEvent.MOUSE_OUT,this.onMaskLeftOut);
         this.rigth.addEventListener(MouseEvent.MOUSE_OUT,this.onMaskRightOut);
         this.up.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskUpOver);
         this.down.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskDownOver);
         this.left.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskLeftOver);
         this.rigth.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskRightOver);
         this.up.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeUp);
         this.down.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeDown);
         this.left.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeLeft);
         this.rigth.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeRight);
         this.North_West.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeUp);
         this.North_West.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeLeft);
         this.South_West.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeDown);
         this.South_West.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeLeft);
         this.South_East.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeDown);
         this.South_East.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeRight);
         this.North_East.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeUp);
         this.North_East.addEventListener(MouseEvent.MOUSE_DOWN,this.onStartResizeRight);
         this.North_West.addEventListener(MouseEvent.MOUSE_DOWN,this.onArcDown);
         this.South_West.addEventListener(MouseEvent.MOUSE_DOWN,this.onArcDown);
         this.South_East.addEventListener(MouseEvent.MOUSE_DOWN,this.onArcDown);
         this.North_East.addEventListener(MouseEvent.MOUSE_DOWN,this.onArcDown);
         this.North_West.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.North_West.addEventListener(MouseEvent.MOUSE_OUT,this.onOut);
         this.South_West.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.South_West.addEventListener(MouseEvent.MOUSE_OUT,this.onOut);
         this.South_East.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.South_East.addEventListener(MouseEvent.MOUSE_OUT,this.onOut);
         this.North_East.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.North_East.addEventListener(MouseEvent.MOUSE_OUT,this.onOut);
      }
      
      internal function onArcDown(e:MouseEvent) : *
      {
         e.target.gotoAndStop("out");
         e.target.removeEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         e.target.addEventListener(MouseEvent.MOUSE_UP,this.onArcUp);
      }
      
      internal function onArcUp(e:MouseEvent) : *
      {
         e.target.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
      }
      
      internal function onOver(e:MouseEvent) : *
      {
         e.target.gotoAndStop("over");
         var revert:String = "";
         switch(e.target.name)
         {
            case "North_West":
               revert = "resizeVH";
               break;
            case "South_West":
               revert = "resizeHV";
               break;
            case "South_East":
               revert = "resizeVH";
               break;
            case "North_East":
               revert = "resizeHV";
         }
         GameCommunication.set_cursor(revert);
      }
      
      internal function onOut(e:MouseEvent) : *
      {
         e.target.gotoAndStop("out");
         GameCommunication.set_cursor("default");
      }
      
      protected function onMaskUpOver(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Up_Start"));
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskUp);
         GameCommunication.set_cursor("resizeV");
      }
      
      protected function onMaskUpOut(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Up_Stop"));
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskUp);
         GameCommunication.set_cursor("default");
      }
      
      protected function onMoveMaskUp(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Up"));
      }
      
      protected function onMaskDownOver(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Down_Start"));
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskDown);
         GameCommunication.set_cursor("resizeV");
      }
      
      protected function onMaskDownOut(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Down_Stop"));
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskDown);
         GameCommunication.set_cursor("default");
      }
      
      protected function onMoveMaskDown(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Down"));
      }
      
      protected function onMaskLeftOver(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Left_Start"));
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskLeft);
         GameCommunication.set_cursor("resizeH");
      }
      
      protected function onMaskLeftOut(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Left_Stop"));
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskLeft);
         GameCommunication.set_cursor("default");
      }
      
      protected function onMoveMaskLeft(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Left"));
      }
      
      protected function onMaskRightOver(e:MouseEvent) : *
      {
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskRight);
         dispatchEvent(new Event("Move_Right_Start"));
         GameCommunication.set_cursor("resizeH");
      }
      
      protected function onMaskRightOut(e:MouseEvent) : *
      {
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveMaskRight);
         dispatchEvent(new Event("Move_Right_Stop"));
         GameCommunication.set_cursor("default");
      }
      
      protected function onMoveMaskRight(e:MouseEvent) : *
      {
         dispatchEvent(new Event("Move_Right"));
      }
      
      private function onStartResizeUp(e:MouseEvent) : *
      {
         this.frame.visible = true;
         this.XX = this.x;
         this.YY = this.y;
         this.YYheight = mouseY - e.target.y;
         this.X = parent.mouseX;
         this.Y = parent.mouseY;
         this.H = this.height;
         this.W = this.width;
         this.onMaskUpOut(e);
         this.North_West.removeEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.up.removeEventListener(MouseEvent.MOUSE_OVER,this.onMaskUpOver);
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveUp);
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onStopResizeUp);
      }
      
      private function onStopResizeUp(e:MouseEvent) : *
      {
         this.North_West.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.up.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskUpOver);
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveUp);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onStopResizeUp);
         dispatchEvent(new ResizeFrameEvent(ResizeFrameEvent.RESIZE,this.width,this.height,0,this.y - this.YY));
         this.y = this.YY;
         this.frame.visible = false;
      }
      
      private function onMoveUp(e:MouseEvent) : *
      {
         var n:Number = this.H - (parent.mouseY - this.Y);
         if(n >= this._minHeight && n <= this._maxHeight)
         {
            this.y = this.YY + (parent.mouseY - this.Y);
            this.height = this.H - (parent.mouseY - this.Y);
         }
      }
      
      private function onStartResizeDown(e:MouseEvent) : *
      {
         this.frame.visible = true;
         this.XX = this.x;
         this.YY = this.y;
         this.X = parent.mouseX;
         this.Y = parent.mouseY;
         this.H = this.height;
         this.W = this.width;
         this.onMaskDownOut(e);
         this.South_West.removeEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.down.removeEventListener(MouseEvent.MOUSE_OVER,this.onMaskDownOver);
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveDown);
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onStopResizeDown);
      }
      
      private function onStopResizeDown(e:MouseEvent) : *
      {
         this.South_West.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.down.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskDownOver);
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveDown);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onStopResizeDown);
         dispatchEvent(new ResizeFrameEvent(ResizeFrameEvent.RESIZE,this.width,this.height));
         this.frame.visible = false;
      }
      
      private function onMoveDown(e:MouseEvent) : *
      {
         var n:Number = this.H + (parent.mouseY - this.Y);
         if(n >= this._minHeight && n <= this._maxHeight)
         {
            this.height = this.H + (parent.mouseY - this.Y);
         }
      }
      
      private function onStartResizeRight(e:MouseEvent) : *
      {
         this.frame.visible = true;
         this.XX = this.x;
         this.YY = this.y;
         this.X = parent.mouseX;
         this.Y = parent.mouseY;
         this.H = this.height;
         this.W = this.width;
         this.onMaskRightOut(e);
         this.North_East.removeEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.South_East.removeEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.rigth.removeEventListener(MouseEvent.MOUSE_OVER,this.onMaskRightOver);
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveRight);
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onStopResizeRight);
      }
      
      private function onStopResizeRight(e:MouseEvent) : *
      {
         this.North_East.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.South_East.addEventListener(MouseEvent.MOUSE_OVER,this.onOver);
         this.rigth.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskRightOver);
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveRight);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onStopResizeRight);
         dispatchEvent(new ResizeFrameEvent(ResizeFrameEvent.RESIZE,this.width,this.height));
         this.frame.visible = false;
      }
      
      private function onMoveRight(e:MouseEvent) : *
      {
         if(this.W + (parent.mouseX - this.X) >= this._minWidth && this.W + (parent.mouseX - this.X) <= this._maxWidth)
         {
            this.width = this.W + (parent.mouseX - this.X);
         }
      }
      
      private function onStartResizeLeft(e:MouseEvent) : *
      {
         this.frame.visible = true;
         this.XX = this.x;
         this.YY = this.y;
         this.XXwidth = mouseX - e.target.x;
         this.X = parent.mouseX;
         this.Y = parent.mouseY;
         this.H = this.height;
         this.W = this.width;
         this.onMaskLeftOut(e);
         this.left.removeEventListener(MouseEvent.MOUSE_OVER,this.onMaskLeftOver);
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onMoveLeft);
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onStopResizeLeft);
      }
      
      private function onStopResizeLeft(e:MouseEvent) : *
      {
         this.left.addEventListener(MouseEvent.MOUSE_OVER,this.onMaskLeftOver);
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onMoveLeft);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onStopResizeLeft);
         dispatchEvent(new ResizeFrameEvent(ResizeFrameEvent.RESIZE,this.width,this.height,this.x - this.XX));
         this.x = this.XX;
         this.frame.visible = false;
      }
      
      private function onMoveLeft(e:MouseEvent) : *
      {
         if(this.W - (parent.mouseX - this.X) >= this._minWidth && this.W - (parent.mouseX - this.X) <= this._maxWidth)
         {
            this.x = this.XX + (parent.mouseX - this.X);
            this.width = this.W - (parent.mouseX - this.X);
         }
      }
   }
}

