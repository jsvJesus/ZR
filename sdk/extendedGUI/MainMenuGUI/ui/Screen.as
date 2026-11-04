package ui
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import flash.display.*;
   import flash.events.*;
   import flash.ui.*;
   import logging.*;
   
   public class Screen extends Sprite
   {
      protected static const SWING_SIZE:uint = 300;
      
      protected static const SWING_DURATION:Number = 0.3;
      
      protected static const SWING_DELAY:Number = 0.02;
      
      protected static const SHOW_DELAY:Number = 0.3;
      
      protected var is_loaded:Boolean = false;
      
      public var inGame:Boolean = false;
      
      public var id:String;
      
      public var label:String;
      
      public var depth:uint;
      
      protected var inited:Boolean = false;
      
      public var use3D:Boolean = false;
      
      public var _background:Bitmap;
      
      protected var appears:Array;
      
      private var _breadCrumb:BreadCrumb;
      
      protected var _enabled:Boolean = true;
      
      protected var _defaultFocus:InteractiveObject;
      
      public function Screen(id:String, depth:uint = 0, use3D:Boolean = false)
      {
         this._breadCrumb = new BreadCrumb(this);
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
         return [Navigator.LeftWidth];
      }
      
      public function get breadCrumb() : BreadCrumb
      {
         return this._breadCrumb;
      }
      
      public function showBackgroundInGame() : void
      {
         var size:Number = NaN;
         if(Base.IN_GAME)
         {
            Base.navigator.header.exitButton.visible = false;
            Base.navigator.header.account.visible = false;
            this._background = new Bitmap(PremiumIcons.Icons["Setting"],"auto",true);
            size = this._background.width / this._background.height;
            this._background.height = Base.stage.stageHeight;
            this._background.width = this._background.height * size;
            if(this._background.width < Base.stage.stageWidth)
            {
               this._background.width = Base.stage.stageWidth;
            }
            this._background.y = 0;
            this._background.x = (Base.stage.stageWidth - this._background.width) / 2;
            this._background.name = "BACK_GR";
            Base.stage.addChild(this._background);
            Base.stage.setChildIndex(this._background,0);
            this.unfreeze();
         }
      }
      
      public function hideBackgroundInGame() : *
      {
         if(!Base.IN_GAME)
         {
            Base.stage.removeChild(this._background);
         }
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
         Logger.LogToChannel(Logger.DEBUG,"screen unfreeze: ",this.id);
         BreadCrumbs.Add(this.breadCrumb);
         Base.stage.focus = this.defaultFocus;
      }
      
      private function animateAppears() : void
      {
      }
      
      public function onShowComplete() : *
      {
         this.is_loaded = true;
      }
      
      public function showUp() : void
      {
         var tl:TimelineMax = null;
         this.is_loaded = false;
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
            "ease":Expo.easeOut,
            "onComplete":this.onShowComplete
         });
      }
      
      public function showDown() : void
      {
         var tl:TimelineMax = null;
         this.is_loaded = false;
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
            "ease":Expo.easeOut,
            "onComplete":this.onShowComplete
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
         var size:Number = NaN;
         if(this._background != null)
         {
            size = this._background.width / this._background.height;
            this._background.height = Base.stage.stageHeight;
            this._background.width = this._background.height * size;
            if(this._background.width < Base.stage.stageWidth)
            {
               this._background.width = Base.stage.stageWidth;
            }
            this._background.y = 0;
            this._background.x = (Base.stage.stageWidth - this._background.width) / 2;
         }
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
      
      public function set defaultFocus(value:InteractiveObject) : void
      {
         this._defaultFocus = value;
      }
      
      public function get defaultFocus() : InteractiveObject
      {
         return !!this._defaultFocus ? this._defaultFocus : this;
      }
      
      public function goBack() : void
      {
         BreadCrumbs.Remove(this.breadCrumb);
      }
   }
}

