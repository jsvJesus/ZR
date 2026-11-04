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
      
      public function Screen(param1:String, param2:uint = 0, param3:Boolean = false)
      {
         this._breadCrumb = new BreadCrumb(this);
         this.id = param1;
         this.depth = param2;
         this.use3D = param3;
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
         var _loc1_:Number = NaN;
         if(Base.IN_GAME)
         {
            Base.navigator.header.exitButton.visible = false;
            Base.navigator.header.gold.visible = false;
            this._background = new Bitmap(PremiumIcons.Icons["Setting"],"auto",true);
            _loc1_ = this._background.width / this._background.height;
            this._background.height = Base.stage.stageHeight;
            this._background.width = this._background.height * _loc1_;
            if(this._background.width < Base.stage.stageWidth)
            {
               this._background.width = Base.stage.stageWidth;
            }
            this._background.y = 0;
            this._background.x = (Base.stage.stageWidth - this._background.width) / 2;
            this._background.name = "BACK_GR";
            Base.stage.addChild(this._background);
            Base.stage.setChildIndex(this._background,0);
         }
      }
      
      protected function onDrawHandler(param1:Event) : void
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
      
      public function prepare(... rest) : void
      {
      }
      
      protected function init(... rest) : void
      {
      }
      
      protected function freeze(... rest) : void
      {
      }
      
      protected function unfreeze(... rest) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"screen unfreeze: ",this.id);
         BreadCrumbs.Add(this.breadCrumb);
         Base.stage.focus = this.defaultFocus;
      }
      
      private function animateAppears() : void
      {
      }
      
      public function showUp() : void
      {
         var _loc1_:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            _loc1_ = new TimelineMax({"delay":SHOW_DELAY});
            _loc1_.staggerFromTo(this.appears,SWING_DURATION,{
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
         var _loc1_:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            _loc1_ = new TimelineMax({"delay":SHOW_DELAY});
            _loc1_.staggerFromTo(this.appears,SWING_DURATION,{
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
      
      public function hideUp(param1:Function = null) : void
      {
         var _loc2_:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            _loc2_ = new TimelineMax();
            _loc2_.staggerFromTo(this.appears,SWING_DURATION,{
               "alpha":1,
               "ease":Expo.easeOut
            },{
               "alpha":0,
               "ease":Expo.easeOut
            },SWING_DELAY,"+=0",param1);
         }
         TweenMax.to(this,SWING_DURATION,{
            "alpha":0,
            "x":-500,
            "ease":Expo.easeOut,
            "onComplete":(Boolean(this.appears) && Boolean(this.appears.length) ? null : param1)
         });
      }
      
      public function hideDown(param1:Function = null) : void
      {
         var _loc2_:TimelineMax = null;
         if(Boolean(this.appears) && Boolean(this.appears.length))
         {
            _loc2_ = new TimelineMax();
            _loc2_.staggerFromTo(this.appears,SWING_DURATION,{
               "alpha":1,
               "ease":Expo.easeOut
            },{
               "alpha":0,
               "ease":Expo.easeOut
            },SWING_DELAY,"+=0",param1);
         }
         TweenMax.to(this,SWING_DURATION,{
            "alpha":0,
            "x":500,
            "ease":Expo.easeOut,
            "onComplete":(Boolean(this.appears) && Boolean(this.appears.length) ? null : param1)
         });
      }
      
      protected function initKeyboardShortcuts(... rest) : void
      {
         Base.stage.addEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function destroyKeyboardShortcuts(... rest) : void
      {
         Base.stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onKeyDown);
      }
      
      protected function onKeyDown(param1:KeyboardEvent) : void
      {
         if(Base.stage.focus != this)
         {
            return;
         }
         switch(param1.keyCode)
         {
            case Keyboard.ENTER:
            case Keyboard.NUMPAD_ENTER:
               this.pressDefaultButton();
         }
      }
      
      protected function pressDefaultButton() : void
      {
      }
      
      protected function show(... rest) : void
      {
      }
      
      protected function hide(... rest) : void
      {
      }
      
      protected function resize(... rest) : void
      {
         var _loc2_:Number = this._background.width / this._background.height;
         this._background.height = Base.stage.stageHeight;
         this._background.width = this._background.height * _loc2_;
         if(this._background.width < Base.stage.stageWidth)
         {
            this._background.width = Base.stage.stageWidth;
         }
         this._background.y = 0;
         this._background.x = (Base.stage.stageWidth - this._background.width) / 2;
      }
      
      public function destroy() : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStage);
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
      }
      
      protected function onAddedToStage(param1:Event) : void
      {
         if(!this.inited)
         {
            this.init();
            this.inited = true;
         }
         this.unfreeze();
      }
      
      protected function onStageResize(param1:Event) : void
      {
         this.resize();
      }
      
      protected function onRemovedFromStage(param1:Event) : void
      {
         this.freeze();
      }
      
      public function get enabled() : Boolean
      {
         return this._enabled;
      }
      
      public function set enabled(param1:Boolean) : void
      {
         this._enabled = param1;
      }
      
      public function set defaultFocus(param1:InteractiveObject) : void
      {
         this._defaultFocus = param1;
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

