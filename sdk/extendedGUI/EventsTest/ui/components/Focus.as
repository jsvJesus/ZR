package ui.components
{
   import com.dvalimona.components.Component;
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import flash.display.BlendMode;
   import flash.display.InteractiveObject;
   import flash.display.Sprite;
   import flash.geom.Point;
   import flash.text.TextField;
   import flash.utils.setTimeout;
   import logging.Logger;
   
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
      
      public function set target(obj:InteractiveObject) : void
      {
         Logger.Log("Focus.target set",obj);
         this.fromNull = this._target == null;
         this._target = obj;
         this.draw();
      }
      
      protected function checkPulse() : void
      {
         var targetAlpha:Number = NaN;
         if(this.pulse)
         {
            targetAlpha = this.alpha > 0.5 ? 0.3 : 1;
            TweenMax.to(this,0.2,{
               "alpha":targetAlpha,
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
         var pnt:Point = this._target.localToGlobal(new Point(0,0));
         if(this.fromNull)
         {
            this.x = pnt.x + this._target.width / 2;
            this.y = pnt.y + this._target.height / 2;
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

