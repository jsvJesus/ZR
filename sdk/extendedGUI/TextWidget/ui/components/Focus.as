package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import flash.display.*;
   import flash.geom.*;
   import flash.text.*;
   import flash.utils.*;
   import logging.*;
   
   public class Focus extends Sprite
   {
      private var pulse:Boolean = false;
      
      private var _target:InteractiveObject;
      
      private var fromNull:Boolean = false;
      
      public function Focus()
      {
         super();
         this.blendMode = BlendMode.DARKEN;
         this.alpha = 0;
         this.graphics.clear();
         this.graphics.beginFill(16777215,0.5);
         this.graphics.drawRect(-50,-50,100,100);
         this.graphics.endFill();
      }
      
      public function set target(param1:InteractiveObject) : void
      {
         Logger.Log("Focus.target set",param1);
         this.fromNull = this._target == null;
         this._target = param1;
         this.draw();
      }
      
      protected function checkPulse() : void
      {
         var _loc1_:Number = NaN;
         if(this.pulse)
         {
            _loc1_ = this.alpha > 0.5 ? 0.3 : 1;
            TweenMax.to(this,0.2,{
               "alpha":_loc1_,
               "delay":0,
               "onComplete":this.checkPulse
            });
         }
      }
      
      public function hideMe() : void
      {
         this.visible = false;
      }
      
      public function draw() : void
      {
         if(this._target == null)
         {
            this.pulse = false;
            TweenMax.to(this,0.2,{
               "alpha":0,
               "width":width + 30,
               "height":height + 30,
               "ease":Expo.easeOut,
               "onComplete":this.hideMe
            });
            return;
         }
         this.visible = true;
         var _loc1_:Point = this._target.localToGlobal(new Point(0,0));
         if(this.fromNull)
         {
            this.x = _loc1_.x + this._target.width / 2;
            this.y = _loc1_.y + this._target.height / 2;
         }
         if(this._target is Component)
         {
            (this._target as Component).addChild(this);
            this.x = this._target.width / 2;
            this.y = this._target.height / 2;
            this.width = this._target.width + (this._target as Component).focusMarginX;
            this.height = this._target.height + (this._target as Component).focusMarginY;
            this.pulse = true;
            setTimeout(this.checkPulse,300);
         }
         else if(this._target is TextField)
         {
            this.pulse = false;
            TweenMax.to(this,0.3,{
               "alpha":0,
               "x":this._target.width / 2,
               "y":this._target.height / 2,
               "width":this._target.width,
               "height":this._target.height,
               "ease":Expo.easeInOut,
               "onComplete":null
            });
            setTimeout(this.checkPulse,300);
         }
      }
   }
}

