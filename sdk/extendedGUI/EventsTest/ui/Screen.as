package ui
{
   import com.dvalimona.components.Component;
   import com.greensock.TimelineMax;
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import communication.BreadCrumb;
   import communication.BreadCrumbs;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.ui.Keyboard;
   import logging.Logger;
   
   public class Screen extends Sprite
   {
      protected static const SWING_SIZE:uint = 300;
      
      protected static const SWING_DURATION:Number = 0.3;
      
      protected static const SWING_DELAY:Number = 0.02;
      
      protected static const SHOW_DELAY:Number = 0.3;
      
      public var id:String;
      
      public var label:String;
      
      public var depth:uint;
      
      protected var inited:Boolean = false;
      
      public var use3D:Boolean = false;
      
      protected var appears:Array;
      
      private var _breadCrumb:BreadCrumb = new BreadCrumb(this);
      
      protected var _enabled:Boolean = true;
      
      public function Screen(id:String, depth:uint = 0, use3D:Boolean = false)
      {
         this.id = id;
         this.depth = depth;
         this.use3D = use3D;
         Logger.LogToChannel(Logger.DEBUG,"Screen construct:",this.id);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         this.addEventListener(Event.ADDED_TO_STAGE,this.initKeyboardShortcuts);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.destroyKeyboardShortcuts);
         Base.stage.addEventListener(Base.STAGE_RESIZE,this.onStageResize);
         super();
         if(this.use3D)
         {
            this.addEventListener(Component.DRAW,this.onDrawHandler,true);
            this.updateRotation();
         }
      }
      
      public function get fullWidth() : uint
      {
         return Base.stage.stageWidth - 50 - 50;
      }
      
      public function get fullHeight() : uint
      {
         return Base.stage.stageHeight;
      }
      
      public function get widths() : Array
      {
         return [Base.stage.stageWidth > 1280 ? 500 : 400];
      }
      
      public function get breadCrumb() : BreadCrumb
      {
         return this._breadCrumb;
      }
      
      protected function onDrawHandler(event:Event) : void
      {
         if(Base.stage.contains(this))
         {
            this.updateRotation();
         }
      }
      
      protected function updateRotation() : void
      {
         this.rotationY = this.rotationY;
      }
      
      public function prepare(... args) : void
      {
      }
      
      protected function init(... args) : void
      {
      }
      
      protected function freeze(... args) : void
      {
      }
      
      protected function unfreeze(... args) : void
      {
         BreadCrumbs.Add(this.breadCrumb);
      }
      
      private function animateAppears() : void
      {
      }
      
      public function showUp() : void
      {
         var tl:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            tl = new TimelineMax({"delay":SHOW_DELAY});
            tl.staggerFromTo(this.appears,SWING_DURATION,{
               "alpha":0,
               "ease":Expo.easeOut
            },{
               "alpha":1,
               "ease":Expo.easeOut
            },SWING_DELAY,"+=0",this.updateRotation);
         }
         this.x = 500;
         TweenMax.to(this,SWING_DURATION,{
            "alpha":1,
            "x":0,
            "delay":SHOW_DELAY,
            "ease":Expo.easeOut
         });
      }
      
      public function showDown() : void
      {
         var tl:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            tl = new TimelineMax({"delay":SHOW_DELAY});
            tl.staggerFromTo(this.appears,SWING_DURATION,{
               "alpha":0,
               "ease":Expo.easeOut
            },{
               "alpha":1,
               "ease":Expo.easeOut
            },SWING_DELAY,"+=0",this.updateRotation);
         }
         this.x = -500;
         TweenMax.to(this,SWING_DURATION,{
            "alpha":1,
            "x":0,
            "delay":SHOW_DELAY,
            "ease":Expo.easeOut
         });
      }
      
      public function hideUp(onCompleteFunction:Function = null) : void
      {
         var tl:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            tl = new TimelineMax();
            tl.staggerFromTo(this.appears,SWING_DURATION,{
               "alpha":1,
               "ease":Expo.easeOut
            },{
               "alpha":0,
               "ease":Expo.easeOut
            },SWING_DELAY,"+=0",onCompleteFunction);
         }
         TweenMax.to(this,SWING_DURATION,{
            "alpha":0,
            "x":-500,
            "ease":Expo.easeOut,
            "onComplete":(Boolean(this.appears) && Boolean(this.appears.length) ? null : onCompleteFunction)
         });
      }
      
      public function hideDown(onCompleteFunction:Function = null) : void
      {
         var tl:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            tl = new TimelineMax();
            tl.staggerFromTo(this.appears,SWING_DURATION,{
               "alpha":1,
               "ease":Expo.easeOut
            },{
               "alpha":0,
               "ease":Expo.easeOut
            },SWING_DELAY,"+=0",onCompleteFunction);
         }
         TweenMax.to(this,SWING_DURATION,{
            "alpha":0,
            "x":500,
            "ease":Expo.easeOut,
            "onComplete":(Boolean(this.appears) && Boolean(this.appears.length) ? null : onCompleteFunction)
         });
      }
      
      protected function initKeyboardShortcuts(... args) : void
      {
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function destroyKeyboardShortcuts(... args) : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(event:KeyboardEvent) : void
      {
         if(Base.stage.focus != this)
         {
            return;
         }
         switch(event.keyCode)
         {
            case Keyboard.ENTER:
            case Keyboard.NUMPAD_ENTER:
               this.pressDefaultButton();
         }
      }
      
      protected function pressDefaultButton() : void
      {
      }
      
      protected function show(... args) : void
      {
      }
      
      protected function hide(... args) : void
      {
      }
      
      protected function resize(... args) : void
      {
      }
      
      public function destroy() : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
      }
      
      protected function onAddedToStage(event:Event) : void
      {
         if(!this.inited)
         {
            this.init();
            this.inited = true;
         }
         this.unfreeze();
      }
      
      protected function onStageResize(event:Event) : void
      {
         this.resize();
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.freeze();
      }
      
      public function get enabled() : Boolean
      {
         return this._enabled;
      }
      
      public function set enabled(value:Boolean) : void
      {
         this._enabled = value;
      }
      
      public function goBack() : void
      {
         BreadCrumbs.Remove(this.breadCrumb);
      }
   }
}

