package ui
{
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import flash.display.DisplayObject;
   
   public class ScreenFadeTransitionConductor
   {
      protected var navigator:ScreenNavigator;
      
      protected var _savedOtherTarget:DisplayObject;
      
      protected var _savedCompleteHandler:Function;
      
      public var duration:Number = 0.75;
      
      public var delay:Number = 0.1;
      
      public var ease:Object = Expo.easeOut;
      
      public var skipNextTransition:Boolean = false;
      
      public function ScreenFadeTransitionConductor(navigator:ScreenNavigator)
      {
         super();
         if(!navigator)
         {
            throw new ArgumentError("ScreenNavigator cannot be null.");
         }
         this.navigator = navigator;
         this.navigator.transition = this.onTransition;
      }
      
      protected function onTransition(oldScreen:DisplayObject, newScreen:DisplayObject, onComplete:Function) : void
      {
         var tween:TweenMax = null;
         if(!oldScreen && !newScreen)
         {
            throw new ArgumentError("Cannot transition if both old screen and new screen are null.");
         }
         if(this.skipNextTransition)
         {
            this.skipNextTransition = false;
            this._savedCompleteHandler = null;
            if(Boolean(newScreen))
            {
               newScreen.x = 0;
               newScreen.alpha = 1;
            }
            if(onComplete != null)
            {
               onComplete();
            }
            return;
         }
         this._savedCompleteHandler = onComplete;
         if(Boolean(newScreen))
         {
            newScreen.alpha = 0;
            if(Boolean(oldScreen))
            {
               oldScreen.alpha = 1;
               TweenMax.to(oldScreen,this.duration,{
                  "alpha":0,
                  "delay":this.delay,
                  "ease":this.ease
               });
            }
            this._savedOtherTarget = oldScreen;
            TweenMax.to(newScreen,this.duration,{
               "alpha":1,
               "delay":this.delay,
               "onComplete":this.activeTransition_onComplete,
               "ease":this.ease
            });
         }
         else
         {
            oldScreen.alpha = 1;
            tween = TweenMax.to(newScreen,this.duration,{
               "alpha":0,
               "delay":this.delay,
               "onComplete":this.activeTransition_onComplete,
               "ease":this.ease
            });
         }
      }
      
      protected function activeTransition_onComplete() : void
      {
         this._savedOtherTarget = null;
         if(this._savedCompleteHandler != null)
         {
            this._savedCompleteHandler();
         }
      }
   }
}

