package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.display.StageAlign;
   import flash.display.StageScaleMode;
   import flash.events.Event;
   import flash.filters.DropShadowFilter;
   import flash.geom.Rectangle;
   import lang.Locale;
   
   [Event(name="draw",type="flash.events.Event")]
   [Event(name="resize",type="flash.events.Event")]
   public class Component extends Sprite
   {
      public static const DRAW:String = "draw";
      
      protected var _width:Number = 0;
      
      protected var _height:Number = 0;
      
      protected var _tag:int = -1;
      
      protected var _enabled:Boolean = true;
      
      private var _disabledAlpha:Number = 0.5;
      
      private var _disabledLabelAlpha:Number = 0.5;
      
      private var _padding:int = 0;
      
      private var _paddingTop:int = 0;
      
      private var _paddingBottom:int = 0;
      
      private var _paddingRight:int = 0;
      
      private var _paddingLeft:int = 0;
      
      private var _clipContent:Boolean = false;
      
      private var _debugColor:int = Style.DEBUG_COLOR;
      
      private var _debugAlpha:int = Style.DEBUG_ALPHA;
      
      private var _needDebugDraw:Boolean = false;
      
      private var _$:String;
      
      public function Component(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super();
         this.addEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHandler);
         this.move(xpos,ypos);
         this.init();
         if(parent != null)
         {
            parent.addChild(this);
         }
      }
      
      public static function initStage(stage:Stage) : void
      {
         stage.align = StageAlign.TOP_LEFT;
         stage.scaleMode = StageScaleMode.NO_SCALE;
      }
      
      protected function onAddedToStageHandler(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this.onAddedToStageHandler);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.onRemoveFromStageHandler);
         this.unfreeze();
      }
      
      protected function onRemoveFromStageHandler(event:Event) : void
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
      
      protected function getShadow(dist:Number, knockout:Boolean = false) : DropShadowFilter
      {
         return new DropShadowFilter(dist,45,Style.DROPSHADOW,1,dist,dist,0.3,1,knockout);
      }
      
      protected function invalidate() : void
      {
         if(this.clipContent)
         {
            this.scrollRect = new Rectangle(0,0,this.width,this.height);
         }
         else
         {
            this.scrollRect = null;
         }
         addEventListener(Event.ENTER_FRAME,this.onInvalidate);
      }
      
      public function move(xpos:Number, ypos:Number) : void
      {
         this.x = Math.round(xpos);
         this.y = Math.round(ypos);
      }
      
      public function setSize(w:Number, h:Number) : void
      {
         this._width = w;
         this._height = h;
         dispatchEvent(new Event(Event.RESIZE));
         this.invalidate();
      }
      
      public function draw() : void
      {
         dispatchEvent(new Event(Component.DRAW));
      }
      
      protected function onInvalidate(event:Event) : void
      {
         removeEventListener(Event.ENTER_FRAME,this.onInvalidate);
         this.draw();
      }
      
      override public function set width(w:Number) : void
      {
         this._width = w;
         this.invalidate();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override public function get width() : Number
      {
         return this._width;
      }
      
      override public function set height(h:Number) : void
      {
         this._height = h;
         this.invalidate();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override public function get height() : Number
      {
         return this._height;
      }
      
      public function set tag(value:int) : void
      {
         this._tag = value;
      }
      
      public function get tag() : int
      {
         return this._tag;
      }
      
      override public function set x(value:Number) : void
      {
         super.x = Math.round(value);
      }
      
      override public function set y(value:Number) : void
      {
         super.y = Math.round(value);
      }
      
      public function set enabled(value:Boolean) : void
      {
         this._enabled = value;
         mouseEnabled = mouseChildren = this._enabled;
         tabEnabled = value;
         alpha = this._enabled ? 1 : this.disabledAlpha;
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
      
      public function set disabledAlpha(value:Number) : void
      {
         this._disabledAlpha = value;
         this.invalidate();
      }
      
      public function get disabledLabelAlpha() : Number
      {
         return this._disabledLabelAlpha;
      }
      
      public function set disabledLabelAlpha(value:Number) : void
      {
         this._disabledLabelAlpha = value;
      }
      
      public function get padding() : int
      {
         return this._padding;
      }
      
      public function set padding(value:int) : void
      {
         this._padding = value;
         this._paddingTop = this._paddingBottom = this._paddingLeft = this._paddingRight = value;
         this.invalidate();
      }
      
      public function get paddingTop() : int
      {
         return this._paddingTop;
      }
      
      public function set paddingTop(value:int) : void
      {
         this._paddingTop = value;
         this.invalidate();
      }
      
      public function get paddingBottom() : int
      {
         return this._paddingBottom;
      }
      
      public function set paddingBottom(value:int) : void
      {
         this._paddingBottom = value;
         this.invalidate();
      }
      
      public function get paddingRight() : int
      {
         return this._paddingRight;
      }
      
      public function set paddingRight(value:int) : void
      {
         this._paddingRight = value;
         this.invalidate();
      }
      
      public function get paddingLeft() : int
      {
         return this._paddingLeft;
      }
      
      public function set paddingLeft(value:int) : void
      {
         this._paddingLeft = value;
         this.invalidate();
      }
      
      public function get clipContent() : Boolean
      {
         return this._clipContent;
      }
      
      public function set clipContent(value:Boolean) : void
      {
         this._clipContent = value;
         this.invalidate();
      }
      
      public function get debugColor() : int
      {
         return this._debugColor;
      }
      
      public function set debugColor(value:int) : void
      {
         this._debugColor = value;
         this.invalidate();
      }
      
      public function get debugAlpha() : int
      {
         return this._debugAlpha;
      }
      
      public function set debugAlpha(value:int) : void
      {
         this._debugAlpha = value;
         this.invalidate();
      }
      
      public function get debug() : Boolean
      {
         return this._needDebugDraw;
      }
      
      public function set debug(value:Boolean) : void
      {
         this._needDebugDraw = value;
         this.invalidate();
      }
      
      public function drawDebug() : void
      {
         if(this.debug)
         {
            this.graphics.clear();
            this.graphics.beginFill(Style.DEBUG_COLOR,Style.DEBUG_ALPHA);
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
      
      public function set $(value:String) : void
      {
         this._$ = value;
         Locale.AddItem(this);
      }
      
      public function updateLocale(localized:String) : void
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

