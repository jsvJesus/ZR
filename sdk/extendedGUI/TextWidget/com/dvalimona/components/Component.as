package com.dvalimona.components
{
   import flash.display.*;
   import flash.events.*;
   import flash.filters.*;
   import flash.utils.*;
   import lang.*;
   
   public class Component extends Sprite
   {
      public static const DRAW:String = "draw";
      
      protected var _width:Number = 0;
      
      protected var _height:Number = 0;
      
      protected var _tag:int = -1;
      
      protected var _enabled:Boolean = true;
      
      public var focusMarginX:uint = 20;
      
      public var focusMarginY:uint = 6;
      
      private var tabEnables:Dictionary;
      
      private var _disabledAlpha:Number = 0.5;
      
      private var _disabledLabelAlpha:Number = 0.5;
      
      private var _margin:int = 0;
      
      private var _marginTop:int = 0;
      
      private var _marginBottom:int = 0;
      
      private var _marginRight:int = 0;
      
      private var _marginLeft:int = 0;
      
      private var _padding:int = 0;
      
      private var _paddingTop:int = 0;
      
      private var _paddingBottom:int = 0;
      
      private var _paddingRight:int = 0;
      
      private var _paddingLeft:int = 0;
      
      private var _clipContent:Boolean = false;
      
      private var _debugColor:int = Style.DEBUG_COLOR;
      
      private var _debugAlpha:Number = Style.DEBUG_ALPHA;
      
      private var _needDebugDraw:Boolean = false;
      
      private var _$:String;
      
      public function Component(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         super();
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHandler);
         this.move(param2,param3);
         this.init();
         if(param1 != null)
         {
            param1.addChild(this);
         }
      }
      
      public static function initStage(param1:Stage) : void
      {
         param1.align = StageAlign.TOP_LEFT;
         param1.scaleMode = StageScaleMode.NO_SCALE;
      }
      
      protected function onAddedToStageHandler(param1:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHandler);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemoveFromStageHandler);
         this.unfreeze();
      }
      
      protected function onRemoveFromStageHandler(param1:Event) : void
      {
         this.removeEventListener(Event.REMOVED_FROM_STAGE,this.onRemoveFromStageHandler);
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHandler);
         this.freeze();
      }
      
      protected function freeze() : void
      {
      }
      
      protected function unfreeze() : void
      {
      }
      
      protected function init() : void
      {
         this.addChildren();
         this.invalidate();
      }
      
      protected function addChildren() : void
      {
      }
      
      protected function getShadow(param1:Number, param2:Boolean = false) : DropShadowFilter
      {
         return new DropShadowFilter(param1,45,Style.DROPSHADOW,1,param1,param1,0.3,1,param2);
      }
      
      public function invalidate() : void
      {
         addEventListener(Event.ENTER_FRAME,this.onInvalidate);
      }
      
      public function move(param1:Number, param2:Number) : void
      {
         this.x = Math.round(param1);
         this.y = Math.round(param2);
      }
      
      public function setSize(param1:Number, param2:Number) : void
      {
         this._width = param1;
         this._height = param2;
         dispatchEvent(new Event(Event.RESIZE));
         this.invalidate();
      }
      
      public function draw() : void
      {
         dispatchEvent(new Event(Component.DRAW));
      }
      
      protected function onInvalidate(param1:Event) : void
      {
         removeEventListener(Event.ENTER_FRAME,this.onInvalidate);
         this.draw();
      }
      
      public function get isComponent() : Boolean
      {
         return true;
      }
      
      public function setWidth(param1:Number) : void
      {
         this._width = param1;
         this.invalidate();
      }
      
      override public function set width(param1:Number) : void
      {
         this._width = param1;
         this.invalidate();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override public function get width() : Number
      {
         return this._width;
      }
      
      override public function set height(param1:Number) : void
      {
         this._height = param1;
         this.invalidate();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override public function get height() : Number
      {
         return this._height;
      }
      
      public function set tag(param1:int) : void
      {
         this._tag = param1;
      }
      
      public function get tag() : int
      {
         return this._tag;
      }
      
      override public function set x(param1:Number) : void
      {
         super.x = Math.round(param1);
      }
      
      override public function set y(param1:Number) : void
      {
         super.y = Math.round(param1);
      }
      
      private function rememberTabEnables() : void
      {
         this.tabEnables = new Dictionary();
         this.tabEnables[this] = this.tabEnabled;
      }
      
      private function restoreTabEnables() : void
      {
         this.tabEnabled = this.tabEnables[this];
         this.tabEnables = null;
      }
      
      public function set enabled(param1:Boolean) : void
      {
         this._enabled = param1;
         mouseEnabled = mouseChildren = this._enabled;
         tabEnabled = param1;
         alpha = !!this._enabled ? 1 : this.disabledAlpha;
         this.invalidate();
      }
      
      public function get enabled() : Boolean
      {
         return this._enabled;
      }
      
      public function get disabledAlpha() : Number
      {
         return this._disabledAlpha;
      }
      
      public function set disabledAlpha(param1:Number) : void
      {
         this._disabledAlpha = param1;
         this.invalidate();
      }
      
      public function get disabledLabelAlpha() : Number
      {
         return this._disabledLabelAlpha;
      }
      
      public function set disabledLabelAlpha(param1:Number) : void
      {
         this._disabledLabelAlpha = param1;
      }
      
      public function get margin() : int
      {
         return this._margin;
      }
      
      public function set margin(param1:int) : void
      {
         this._margin = param1;
         this._marginTop = this._marginBottom = this._marginLeft = this._marginRight = param1;
         this.invalidate();
      }
      
      public function get marginTop() : int
      {
         return this._marginTop;
      }
      
      public function set marginTop(param1:int) : void
      {
         this._marginTop = param1;
         this.invalidate();
      }
      
      public function get marginBottom() : int
      {
         return this._marginBottom;
      }
      
      public function set marginBottom(param1:int) : void
      {
         this._marginBottom = param1;
         this.invalidate();
      }
      
      public function get marginRight() : int
      {
         return this._marginRight;
      }
      
      public function set marginRight(param1:int) : void
      {
         this._marginRight = param1;
         this.invalidate();
      }
      
      public function get marginLeft() : int
      {
         return this._marginLeft;
      }
      
      public function set marginLeft(param1:int) : void
      {
         this._marginLeft = param1;
         this.invalidate();
      }
      
      public function get padding() : int
      {
         return this._padding;
      }
      
      public function set padding(param1:int) : void
      {
         this._padding = param1;
         this._paddingTop = this._paddingBottom = this._paddingLeft = this._paddingRight = param1;
         this.invalidate();
      }
      
      public function get paddingTop() : int
      {
         return this._paddingTop;
      }
      
      public function set paddingTop(param1:int) : void
      {
         this._paddingTop = param1;
         this.invalidate();
      }
      
      public function get paddingBottom() : int
      {
         return this._paddingBottom;
      }
      
      public function set paddingBottom(param1:int) : void
      {
         this._paddingBottom = param1;
         this.invalidate();
      }
      
      public function get paddingRight() : int
      {
         return this._paddingRight;
      }
      
      public function set paddingRight(param1:int) : void
      {
         this._paddingRight = param1;
         this.invalidate();
      }
      
      public function get paddingLeft() : int
      {
         return this._paddingLeft;
      }
      
      public function set paddingLeft(param1:int) : void
      {
         this._paddingLeft = param1;
         this.invalidate();
      }
      
      public function get clipContent() : Boolean
      {
         return this._clipContent;
      }
      
      public function set clipContent(param1:Boolean) : void
      {
         this._clipContent = param1;
         this.invalidate();
      }
      
      public function get debugColor() : int
      {
         return this._debugColor;
      }
      
      public function set debugColor(param1:int) : void
      {
         this._debugColor = param1;
         this.invalidate();
      }
      
      public function get debugAlpha() : Number
      {
         return this._debugAlpha;
      }
      
      public function set debugAlpha(param1:Number) : void
      {
         this._debugAlpha = param1;
         this.invalidate();
      }
      
      public function get debug() : Boolean
      {
         return this._needDebugDraw;
      }
      
      public function set debug(param1:Boolean) : void
      {
         this._needDebugDraw = param1;
         this.invalidate();
      }
      
      public function drawDebug() : void
      {
         if(this.debug)
         {
            this.graphics.clear();
            this.graphics.beginFill(this.debugColor,0.1);
            this.graphics.drawRect(0,0,this._width,this._height);
            this.graphics.endFill();
         }
         else
         {
            this.graphics.clear();
         }
      }
      
      public function get $() : String
      {
         return this._$;
      }
      
      public function set $(param1:String) : void
      {
         this._$ = param1;
         Locale.AddItem(this);
      }
      
      public function updateLocale(param1:String) : void
      {
      }
      
      public function destroy() : void
      {
      }
      
      public function enable() : void
      {
      }
      
      public function disable() : void
      {
      }
   }
}

