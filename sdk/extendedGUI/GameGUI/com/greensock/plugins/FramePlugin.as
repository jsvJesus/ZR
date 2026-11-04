package com.greensock.plugins
{
   import com.greensock.TweenLite;
   import flash.display.MovieClip;
   
   public class FramePlugin extends TweenPlugin
   {
      public static const API:Number = 2;
      
      protected var _target:MovieClip;
      
      public var frame:int;
      
      public function FramePlugin()
      {
         super("frame,frameLabel,frameForward,frameBackward");
      }
      
      override public function setRatio(v:Number) : void
      {
         super.setRatio(v);
         if(this.frame != _target.currentFrame)
         {
            _target.gotoAndStop(this.frame);
         }
      }
      
      override public function _onInitTween(target:Object, value:*, tween:TweenLite) : Boolean
      {
         if(!(target is MovieClip) || isNaN(value))
         {
            return false;
         }
         _target = target as MovieClip;
         this.frame = _target.currentFrame;
         _addTween(this,"frame",this.frame,value,"frame",true);
         return true;
      }
   }
}

