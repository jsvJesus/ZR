package com.dvalimona.components
{
   import com.greensock.*;
   import flash.display.*;
   import flash.events.*;
   
   public class Window extends Component
   {
      public static const TweenShift:uint = 30;
      
      protected var headerHeight:uint = 50;
      
      protected var footerHeight:uint = 60;
      
      protected var gap:uint = 1;
      
      protected var sideMargin:uint = 20;
      
      protected var topMargin:uint = 20;
      
      protected var bottomMargin:uint = 20;
      
      protected var header:Sprite;
      
      protected var leftItems:HBox;
      
      protected var rightItems:HBox;
      
      protected var _titleBar:BlackPanel;
      
      protected var _title:String;
      
      protected var _titleLabel:Label;
      
      protected var _panel:BlackGridPanel;
      
      protected var _color:int = -1;
      
      protected var _shadow:Boolean = true;
      
      protected var _draggable:Boolean = true;
      
      protected var _minimizeButton:Sprite;
      
      protected var _hasMinimizeButton:Boolean = false;
      
      protected var _minimized:Boolean = false;
      
      protected var _hasCloseButton:Boolean;
      
      protected var _closeButton:PushButton;
      
      protected var _grips:Shape;
      
      public function Window(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, title:String = "Window")
      {
         this._title = title;
         this.addEventListener(Event.ADDED_TO_STAGE,this._onAddedToStageHandler);
         super(parent,xpos,ypos);
      }
      
      protected function _onAddedToStageHandler(event:Event) : void
      {
         this.removeEventListener(Event.ADDED_TO_STAGE,this._onAddedToStageHandler);
         TweenMax.killTweensOf(this);
         this.showOn();
      }
      
      protected function showOn() : void
      {
      }
      
      protected function close() : void
      {
         TweenMax.killTweensOf(this);
      }
      
      private function killWindow() : void
      {
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(100,100);
      }
      
      override protected function addChildren() : void
      {
         super.mouseChildren = true;
         super.addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseGoDown);
         this.header = new Sprite();
         super.addChild(this.header);
         this._titleBar = new BlackPanel();
         this._titleBar.tabChildren = true;
         this._titleBar.tabEnabled = false;
         this._titleBar.buttonMode = false;
         this._titleBar.useHandCursor = false;
         this._titleBar.height = this.headerHeight;
         this.header.addChild(this._titleBar);
         this.leftItems = new HBox();
         this.leftItems.alignment = HBox.MIDDLE;
         this.leftItems.fixedHeight = this.headerHeight;
         this.leftItems.horizontalAlign = HBox.LEFT;
         this.header.addChild(this.leftItems);
         this.rightItems = new HBox();
         this.rightItems.alignment = HBox.MIDDLE;
         this.rightItems.fixedHeight = this.headerHeight;
         this.rightItems.horizontalAlign = HBox.RIGHT;
         this.header.addChild(this.rightItems);
         this._titleLabel = new Label(this.leftItems,0,0,this._title);
         this._grips = new Shape();
         for(var i:int = 0; i < 4; i++)
         {
            this._grips.graphics.lineStyle(1,16777215,0.55);
            this._grips.graphics.moveTo(0,3 + i * 4);
            this._grips.graphics.lineTo(100,3 + i * 4);
            this._grips.graphics.lineStyle(1,0,0.125);
            this._grips.graphics.moveTo(0,4 + i * 4);
            this._grips.graphics.lineTo(100,4 + i * 4);
         }
         this._titleBar.content.addChild(this._grips);
         this._grips.visible = false;
         this._panel = new BlackGridPanel(null,0,this.headerHeight + 1);
         this._panel.visible = !this._minimized;
         super.addChild(this._panel);
         this._minimizeButton = new Sprite();
         this._minimizeButton.graphics.beginFill(0,0);
         this._minimizeButton.graphics.drawRect(-10,-10,20,20);
         this._minimizeButton.graphics.endFill();
         this._minimizeButton.graphics.beginFill(0,0.35);
         this._minimizeButton.graphics.moveTo(-5,-3);
         this._minimizeButton.graphics.lineTo(5,-3);
         this._minimizeButton.graphics.lineTo(0,4);
         this._minimizeButton.graphics.lineTo(-5,-3);
         this._minimizeButton.graphics.endFill();
         this._minimizeButton.x = 10;
         this._minimizeButton.y = 10;
         this._minimizeButton.useHandCursor = true;
         this._minimizeButton.buttonMode = true;
         this._minimizeButton.addEventListener(MouseEvent.CLICK,this.onMinimize);
         this._closeButton = new PushButton(null,86,6,"",this.onClose);
         this._closeButton.setSize(8,8);
      }
      
      override public function addChild(child:DisplayObject) : DisplayObject
      {
         this.content.addChild(child);
         return child;
      }
      
      public function addRawChild(child:DisplayObject) : DisplayObject
      {
         super.addChild(child);
         return child;
      }
      
      override public function draw() : void
      {
         super.draw();
         this.leftItems.x = 20;
         this.leftItems.setSize(width - this.sideMargin * 2,this.headerHeight);
         this.rightItems.x = width - this.sideMargin;
         this.rightItems.setSize(width - this.sideMargin * 2,this.headerHeight);
         this._titleBar.color = this._color;
         this._panel.color = this._color;
         this._titleBar.width = width;
         this._titleBar.draw();
         this._titleLabel.x = !!this._hasMinimizeButton ? 20 : 5;
         this._closeButton.x = _width - 14;
         this._grips.x = this._titleLabel.x + this._titleLabel.width;
         if(this._hasCloseButton)
         {
            this._grips.width = this._closeButton.x - this._grips.x - 2;
         }
         else
         {
            this._grips.width = _width - this._grips.x - 2;
         }
         this._panel.y = this.headerHeight + this.gap;
         this._panel.setSize(_width,_height - this.headerHeight - this.gap - this.footerHeight - this.gap);
         this._panel.draw();
      }
      
      protected function onMouseGoDown(event:MouseEvent) : void
      {
         if(this._draggable)
         {
            this.startDrag();
            stage.addEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
         }
         dispatchEvent(new Event(Event.SELECT));
      }
      
      protected function onMouseGoUp(event:MouseEvent) : void
      {
         this.stopDrag();
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
      }
      
      protected function onMinimize(event:MouseEvent) : void
      {
         this.minimized = !this.minimized;
      }
      
      protected function onClose(event:MouseEvent) : void
      {
         dispatchEvent(new Event(Event.CLOSE));
      }
      
      public function set shadow(b:Boolean) : void
      {
         this._shadow = b;
         if(!this._shadow)
         {
         }
      }
      
      public function get shadow() : Boolean
      {
         return this._shadow;
      }
      
      public function set color(c:int) : void
      {
         this._color = c;
         invalidate();
      }
      
      public function get color() : int
      {
         return this._color;
      }
      
      public function set title(t:String) : void
      {
         this._title = t;
         this._titleLabel.text = this._title;
      }
      
      public function get title() : String
      {
         return this._title;
      }
      
      public function get content() : DisplayObjectContainer
      {
         return this._panel.content;
      }
      
      public function set draggable(b:Boolean) : void
      {
         this._draggable = b;
         this._titleBar.buttonMode = this._draggable;
         this._titleBar.useHandCursor = this._draggable;
      }
      
      public function get draggable() : Boolean
      {
         return this._draggable;
      }
      
      public function set hasMinimizeButton(b:Boolean) : void
      {
         this._hasMinimizeButton = b;
         if(this._hasMinimizeButton)
         {
            super.addChild(this._minimizeButton);
         }
         else if(contains(this._minimizeButton))
         {
            removeChild(this._minimizeButton);
         }
         invalidate();
      }
      
      public function get hasMinimizeButton() : Boolean
      {
         return this._hasMinimizeButton;
      }
      
      public function set minimized(value:Boolean) : void
      {
         this._minimized = value;
         if(this._minimized)
         {
            if(contains(this._panel))
            {
               removeChild(this._panel);
            }
            this._minimizeButton.rotation = -90;
         }
         else
         {
            if(!contains(this._panel))
            {
               super.addChild(this._panel);
            }
            this._minimizeButton.rotation = 0;
         }
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      public function get minimized() : Boolean
      {
         return this._minimized;
      }
      
      override public function get height() : Number
      {
         if(contains(this._panel))
         {
            return _height - this.headerHeight - this.gap;
         }
         return this.headerHeight;
      }
      
      public function set hasCloseButton(value:Boolean) : void
      {
         this._hasCloseButton = value;
         if(this._hasCloseButton)
         {
            this._titleBar.content.addChild(this._closeButton);
         }
         else if(this._titleBar.content.contains(this._closeButton))
         {
            this._titleBar.content.removeChild(this._closeButton);
         }
         invalidate();
      }
      
      public function get hasCloseButton() : Boolean
      {
         return this._hasCloseButton;
      }
      
      public function get titleBar() : BlackPanel
      {
         return this._titleBar;
      }
      
      public function set titleBar(value:BlackPanel) : void
      {
         this._titleBar = value;
      }
      
      public function get grips() : Shape
      {
         return this._grips;
      }
   }
}

