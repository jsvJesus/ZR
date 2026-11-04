package ui.components
{
   import com.dvalimona.components.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.utils.*;
   import lang.*;
   import logging.*;
   
   public class ItemStepper extends Component
   {
      protected const DELAY_TIME:int = 500;
      
      protected const REPEAT_TIME:int = 200;
      
      protected const UP:String = "up";
      
      protected const DOWN:String = "down";
      
      public var defaultValue:*;
      
      public var initValue:*;
      
      private var _useDiagridBack:Boolean = true;
      
      private var _mouseDown:Boolean = false;
      
      private var _showCountLength:Boolean = false;
      
      private var _loop:Boolean = true;
      
      private var _count:uint = 0;
      
      public var itemGroup:String;
      
      private var _items:Array;
      
      protected var _delayTimer:Timer = new Timer(this.DELAY_TIME,1);
      
      protected var _repeatTimer:Timer;
      
      protected var _direction:String;
      
      private var back:ui.components.BlackPanel;
      
      private var label:LabelShadowed;
      
      private var minus:PushButton;
      
      private var plus:PushButton;
      
      public function ItemStepper(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      public function get changed() : Boolean
      {
         return this.initValue != this.currentItem.value;
      }
      
      public function get isDefaults() : Boolean
      {
         Logger.LogToChannel(Logger.DEBUG,this,"isDefaults",this.currentItem,this.defaultValue);
         Logger.LogToChannel(Logger.DEBUG,this,"isDefaults",this.currentItem.value,this.defaultValue);
         if(this.currentItem.value == this.defaultValue)
         {
            return true;
         }
         return false;
      }
      
      public function get useDiagridBack() : Boolean
      {
         return this._useDiagridBack;
      }
      
      public function set useDiagridBack(value:Boolean) : void
      {
         if(this._useDiagridBack != value)
         {
            this._useDiagridBack = value;
            invalidate();
            this.back.visible = value;
         }
      }
      
      public function get showCountLength() : Boolean
      {
         return this._showCountLength;
      }
      
      public function set showCountLength(value:Boolean) : void
      {
         this._showCountLength = value;
         invalidate();
      }
      
      public function get loop() : Boolean
      {
         return this._loop;
      }
      
      public function set loop(value:Boolean) : void
      {
         this._loop = value;
         invalidate();
      }
      
      public function get count() : uint
      {
         return this._count;
      }
      
      public function set count(value:uint) : void
      {
         if(this._count == value)
         {
            return;
         }
         this._count = value;
         invalidate();
         dispatchEvent(new Event(Event.CHANGE));
      }
      
      public function get items() : Array
      {
         return this._items;
      }
      
      public function reverseItems() : void
      {
         this._items = this._items.reverse();
      }
      
      public function set items(value:Array) : void
      {
         this._items = this.sortItems(value);
         invalidate();
      }
      
      public function sortItems(nonSorted:Array) : Array
      {
         var temp:Array = nonSorted;
         var sorted:Array = temp.sortOn("ranger",[Array.NUMERIC]);
         for(var i:uint = 0; i < sorted.length; i++)
         {
         }
         return sorted;
      }
      
      public function get currentItem() : Object
      {
         return this.items[this.count];
      }
      
      public function get value() : String
      {
         return String(this.currentItem.value);
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(80,16);
         setTimeout(this.draw,50);
      }
      
      override protected function addChildren() : void
      {
         this.back = new BlackPanel();
         this.back.visible = true;
         this.addChild(this.back);
         this.label = new LabelShadowed();
         this.label.text = "";
         this.label.size = 22;
         this.label.autoSize = false;
         this.label.align = Label.CENTER;
         this.label.debug = false;
         this.addChild(this.label);
         this.minus = new ClearButton();
         this.minus.size = 30;
         this.minus.label = "«";
         this.minus.setSize(35,35);
         this.minus.debug = false;
         this.minus.focusMarginX = 0;
         this.minus.focusMarginY = 0;
         this.addChild(this.minus);
         this.minus.addEventListener(MouseEvent.MOUSE_DOWN,this.onPlus);
         this.plus = new ClearButton();
         this.plus.size = 30;
         this.plus.label = "»";
         this.plus.setSize(35,35);
         this.plus.debug = false;
         this.plus.focusMarginX = 0;
         this.plus.focusMarginY = 0;
         this.addChild(this.plus);
         this.plus.addEventListener(MouseEvent.MOUSE_DOWN,this.onMinus);
         invalidate();
      }
      
      override public function draw() : void
      {
         if(this.useDiagridBack)
         {
            this.back.width = this.width;
            this.back.height = this.height;
         }
         else
         {
            this.back.visible = false;
         }
         this.plus.x = _width - this.plus.width;
         this.plus.y = _height / 2 - this.plus.height / 2;
         this.minus.x = 0;
         this.minus.y = _height / 2 - this.minus.height / 2;
         if(Boolean(this.currentItem) && Boolean(this.currentItem.caption))
         {
            this.label.text = Locale.getById(this.currentItem.caption) + (this.showCountLength ? " [" + String(this.count + 1) + "/" + this.items.length + "]" : "");
         }
         else
         {
            this.label.text = "--";
         }
         this.label.height = height;
         this.label.x = this.minus.x + this.minus.width;
         this.label.y = _height / 2 - this.label.height / 2 + 3;
         this.label.width = _width - this.plus.width - this.minus.width;
      }
      
      public function setByFieldValue(field:String, value:*) : void
      {
         var item:Object = null;
         for(var i:uint = 0; i < this.items.length; i++)
         {
            if(this.items[i][field] == value)
            {
               this.count = i;
               break;
            }
         }
      }
      
      protected function increment() : void
      {
         this.count = this.count + 1 > this.items.length - 1 ? (this.loop ? 0 : this.items.length - 1) : uint(this.count + 1);
         invalidate();
      }
      
      protected function decrement() : void
      {
         this.count = this.count - 1 < 0 ? (this.loop ? this.items.length - 1 : 0) : this.count - 1;
         invalidate();
      }
      
      protected function onDelayComplete(event:TimerEvent) : void
      {
         this._delayTimer.removeEventListener(TimerEvent.TIMER_COMPLETE,this.onDelayComplete);
         this._delayTimer.stop();
         this._delayTimer = null;
         if(this._mouseDown)
         {
            this._repeatTimer = new Timer(this.REPEAT_TIME);
            this._repeatTimer.addEventListener(TimerEvent.TIMER,this.onRepeat);
            this._repeatTimer.start();
         }
      }
      
      protected function onRepeat(event:TimerEvent) : void
      {
         if(this._direction == this.UP)
         {
            this.increment();
         }
         else
         {
            this.decrement();
         }
      }
      
      protected function onMinus(event:MouseEvent) : void
      {
         this._mouseDown = true;
         Base.stage.addEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
         this.decrement();
         this._direction = this.DOWN;
         this._delayTimer.addEventListener(TimerEvent.TIMER_COMPLETE,this.onDelayComplete);
         this._delayTimer.start();
      }
      
      protected function onPlus(event:MouseEvent) : void
      {
         this._mouseDown = true;
         Base.stage.addEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
         this.increment();
         this._direction = this.UP;
         this._delayTimer.addEventListener(TimerEvent.TIMER_COMPLETE,this.onDelayComplete);
         this._delayTimer.start();
      }
      
      protected function onMouseGoUp(event:MouseEvent) : void
      {
         this._mouseDown = false;
         Base.stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
         if(this._repeatTimer != null)
         {
            this._repeatTimer.removeEventListener(TimerEvent.TIMER,this.onRepeat);
            this._repeatTimer.stop();
         }
         if(this._delayTimer != null)
         {
            this._delayTimer.stop();
         }
      }
      
      public function randomize(silence:Boolean = false) : void
      {
         var value:uint = Math.round(Math.random() * (this.items.length - 1));
         if(!silence)
         {
            this.count = value;
         }
         else
         {
            if(this._count == value)
            {
               return;
            }
            this._count = value;
            invalidate();
         }
      }
   }
}

