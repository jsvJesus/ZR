package ui.components
{
   import com.dvalimona.components.Component;
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import flash.display.CapsStyle;
   import flash.display.InteractiveObject;
   import flash.display.JointStyle;
   import flash.display.LineScaleMode;
   import flash.display.Sprite;
   import flash.geom.Point;
   import flash.utils.setTimeout;
   
   public class Focus extends Sprite
   {
      private var pulse:Boolean = false;
      
      private var _target:InteractiveObject;
      
      private var fromNull:Boolean = false;
      
      public function Focus()
      {
         super();
         this.alpha = 0;
         this.graphics.clear();
         this.graphics.lineStyle(4,5626367,0.5,true,LineScaleMode.NONE,CapsStyle.SQUARE,JointStyle.MITER);
         this.graphics.drawRect(-50,-50,100,100);
         this.graphics.endFill();
      }
      
      public function set target(obj:InteractiveObject) : void
      {
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
      
      public function draw() : void
      {
         if(this._target == null)
         {
            this.pulse = false;
            TweenMax.to(this,0.2,{
               "alpha":0,
               "width":width + 10,
               "height":height + 10,
               "ease":Expo.easeOut,
               "onComplete":null
            });
            return;
         }
         var pnt:Point = this._target.localToGlobal(new Point(0,0));
         if(this.fromNull)
         {
            this.x = pnt.x + this._target.width / 2;
            this.y = pnt.y + this._target.height / 2;
         }
         if(this._target is Component)
         {
            this.pulse = true;
            TweenMax.to(this,0.2,{
               "alpha":0.7,
               "x":pnt.x + this._target.width / 2,
               "y":pnt.y + this._target.height / 2,
               "width":this._target.width + 20,
               "height":this._target.height + 6,
               "ease":Expo.easeOut,
               "onComplete":null
            });
            setTimeout(this.checkPulse,300);
         }
      }
   }
}

