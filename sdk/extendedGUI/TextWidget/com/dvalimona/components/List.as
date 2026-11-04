package com.dvalimona.components
{
   import flash.display.*;
   import flash.events.*;
   import logging.*;
   
   public class List extends Component
   {
      protected var _items:Array;
      
      protected var _listItems:Array;
      
      protected var _itemHolder:Sprite;
      
      protected var _panel:Panel;
      
      protected var _listItemHeight:Number = 20;
      
      protected var _listItemClass:Class = ListItem;
      
      protected var _scrollbar:VScrollBar;
      
      protected var _selectedIndex:int = -1;
      
      protected var _defaultColor:uint = Style.LIST_DEFAULT;
      
      protected var _alternateColor:uint = Style.LIST_ALTERNATE;
      
      protected var _selectedColor:uint = Style.LIST_SELECTED;
      
      protected var _rolloverColor:uint = Style.LIST_ROLLOVER;
      
      protected var _alternateRows:Boolean = false;
      
      protected var _spacing:uint = 0;
      
      protected var _labelShift:uint = 0;
      
      public function List(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:Array = null)
      {
         if(param4 != null)
         {
            this._items = param4;
         }
         else
         {
            this._items = new Array();
         }
         super(param1,param2,param3);
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(100,100);
         addEventListener(MouseEvent.MOUSE_WHEEL,this.onMouseWheel);
         addEventListener(Event.RESIZE,this.onResize);
         this.makeListItems();
         this.fillItems();
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this._panel = new Panel(this,0,0);
         this._panel.color = this._defaultColor;
         this._panel.colorAlpha = 0.6;
         this._panel.backgroundAlpha = 0;
         this._itemHolder = new Sprite();
         this._panel.content.addChild(this._itemHolder);
         this._scrollbar = new VScrollBar(this,0,0,this.onScroll);
         this._scrollbar.hideButtons = true;
         this._scrollbar.setSliderParams(0,0,0);
      }
      
      protected function makeListItems() : void
      {
         var _loc1_:ListItem = null;
         while(this._itemHolder.numChildren > 0)
         {
            _loc1_ = ListItem(this._itemHolder.getChildAt(0));
            _loc1_.removeEventListener(MouseEvent.CLICK,this.onSelect);
            this._itemHolder.removeChildAt(0);
         }
         this._listItems = new Array();
         var _loc2_:int = Math.ceil(_height / this._listItemHeight);
         _loc2_ = Math.min(_loc2_,this._items.length);
         _loc2_ = Math.max(_loc2_,0);
         var _loc3_:int = 0;
         while(_loc3_ < _loc2_)
         {
            _loc1_ = new this._listItemClass(this._itemHolder,0,_loc3_ * this._listItemHeight + _loc3_ * this.spacing);
            this._listItems.push(_loc1_);
            _loc1_.setSize(width - this._scrollbar.width,this._listItemHeight);
            _loc1_.defaultColor = this._defaultColor;
            _loc1_.selectedColor = this._selectedColor;
            _loc1_.rolloverColor = this._rolloverColor;
            _loc1_.addEventListener(MouseEvent.CLICK,this.onSelect);
            _loc1_.doubleClickEnabled = true;
            _loc1_.addEventListener(MouseEvent.DOUBLE_CLICK,this.onDoubleClick);
            _loc3_++;
         }
      }
      
      protected function fillItems() : void
      {
         var offset:int = 0;
         var numItems:int = 0;
         var i:int = 0;
         var item:ListItem = null;
         try
         {
            offset = int(this._scrollbar.value);
            numItems = Math.ceil(_height / this._listItemHeight);
            numItems = Math.min(numItems,this._items.length);
            i = 0;
            while(i < numItems)
            {
               item = this._listItems[i] as ListItem;
               if(offset + i < this._items.length)
               {
                  item.data = this._items[offset + i];
               }
               else
               {
                  item.data = "";
               }
               if(this._alternateRows)
               {
                  item.defaultColor = (offset + i) % 2 == 0 ? uint(this._defaultColor) : uint(this._alternateColor);
               }
               else
               {
                  item.defaultColor = this._defaultColor;
               }
               if(offset + i == this._selectedIndex)
               {
                  item.selected = true;
               }
               else
               {
                  item.selected = false;
               }
               i++;
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.DEBUG,"ERROR List.fillItems:",error);
         }
      }
      
      protected function scrollToSelection() : void
      {
         var _loc1_:int = Math.ceil(_height / this._listItemHeight);
         if(this._selectedIndex != -1)
         {
            if(this._scrollbar.value <= this._selectedIndex)
            {
               if(this._scrollbar.value + _loc1_ < this._selectedIndex)
               {
                  this._scrollbar.value = this._selectedIndex - _loc1_ + 1;
               }
            }
         }
         else
         {
            this._scrollbar.value = 0;
         }
         this.fillItems();
      }
      
      override public function draw() : void
      {
         super.draw();
         var _loc1_:int = int(this._selectedIndex);
         this._selectedIndex = Math.min(this._selectedIndex,this._items.length - 1);
         if(_loc1_ != this._selectedIndex)
         {
            dispatchEvent(new Event(Event.SELECT));
         }
         this._panel.setSize(_width,_height);
         this._panel.color = this._defaultColor;
         this._panel.draw();
         this._scrollbar.x = _width - 10;
         var _loc2_:Number = this._items.length * this._listItemHeight;
         this._scrollbar.setThumbPercent(_height / _loc2_);
         var _loc3_:Number = Math.floor(_height / this._listItemHeight);
         this._scrollbar.maximum = Math.max(0,this._items.length - _loc3_);
         this._scrollbar.pageSize = _loc3_;
         this._scrollbar.height = _height + this._items.length * this.spacing;
         this._scrollbar.draw();
         this.scrollToSelection();
      }
      
      public function addItem(param1:Object) : void
      {
         this._items.push(param1);
         invalidate();
         this.makeListItems();
         this.fillItems();
      }
      
      public function addItemAt(param1:Object, param2:int) : void
      {
         param2 = Math.max(0,param2);
         param2 = Math.min(this._items.length,param2);
         this._items.splice(param2,0,param1);
         invalidate();
         this.makeListItems();
         this.fillItems();
      }
      
      public function removeItem(param1:Object) : void
      {
         var _loc2_:int = int(this._items.indexOf(param1));
         this.removeItemAt(_loc2_);
      }
      
      public function removeItemAt(param1:int) : void
      {
         if(param1 < 0 || param1 >= this._items.length)
         {
            return;
         }
         this._items.splice(param1,1);
         invalidate();
         this.makeListItems();
      }
      
      public function removeAll() : void
      {
         this._items = new Array();
         invalidate();
         this.makeListItems();
      }
      
      protected function onSelect(param1:Event) : void
      {
         var offset:int = 0;
         var i:int = 0;
         var event:Event = param1;
         try
         {
            if(!(event.target is ListItem))
            {
               return;
            }
            offset = int(this._scrollbar.value);
            i = 0;
            while(i < this._itemHolder.numChildren)
            {
               if(this._itemHolder.getChildAt(i) == event.target)
               {
                  this._selectedIndex = i + offset;
               }
               ListItem(this._itemHolder.getChildAt(i)).selected = false;
               i++;
            }
            ListItem(event.target).selected = true;
            dispatchEvent(new Event(Event.SELECT));
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.DEBUG,"ERROR List.onSelect:",error);
         }
      }
      
      protected function onDoubleClick(param1:MouseEvent) : void
      {
         dispatchEvent(new Event(Event.OPEN));
      }
      
      protected function onScroll(param1:Event) : void
      {
         this.fillItems();
      }
      
      protected function onMouseWheel(param1:MouseEvent) : void
      {
         this._scrollbar.value -= param1.delta;
         this.fillItems();
      }
      
      protected function onResize(param1:Event) : void
      {
         this.makeListItems();
         this.fillItems();
      }
      
      public function set selectedIndex(param1:int) : void
      {
         if(param1 >= 0 && param1 < this._items.length)
         {
            this._selectedIndex = param1;
         }
         else
         {
            this._selectedIndex = -1;
         }
         invalidate();
         dispatchEvent(new Event(Event.SELECT));
      }
      
      public function get selectedIndex() : int
      {
         return this._selectedIndex;
      }
      
      public function set selectedItem(param1:Object) : void
      {
         var _loc2_:int = int(this._items.indexOf(param1));
         this.selectedIndex = _loc2_;
         invalidate();
         dispatchEvent(new Event(Event.SELECT));
      }
      
      public function get selectedItem() : Object
      {
         if(this._selectedIndex >= 0 && this._selectedIndex < this._items.length)
         {
            return this._items[this._selectedIndex];
         }
         return null;
      }
      
      public function set defaultColor(param1:uint) : void
      {
         this._defaultColor = param1;
         invalidate();
      }
      
      public function get defaultColor() : uint
      {
         return this._defaultColor;
      }
      
      public function set selectedColor(param1:uint) : void
      {
         this._selectedColor = param1;
         invalidate();
      }
      
      public function get selectedColor() : uint
      {
         return this._selectedColor;
      }
      
      public function set rolloverColor(param1:uint) : void
      {
         this._rolloverColor = param1;
         invalidate();
      }
      
      public function get rolloverColor() : uint
      {
         return this._rolloverColor;
      }
      
      public function set listItemHeight(param1:Number) : void
      {
         this._listItemHeight = param1;
         this.makeListItems();
         invalidate();
      }
      
      public function get listItemHeight() : Number
      {
         return this._listItemHeight;
      }
      
      public function set items(param1:Array) : void
      {
         this._items = param1;
         invalidate();
      }
      
      public function get items() : Array
      {
         return this._items;
      }
      
      public function set listItemClass(param1:Class) : void
      {
         this._listItemClass = param1;
         this.makeListItems();
         invalidate();
      }
      
      public function get listItemClass() : Class
      {
         return this._listItemClass;
      }
      
      public function set alternateColor(param1:uint) : void
      {
         this._alternateColor = param1;
         invalidate();
      }
      
      public function get alternateColor() : uint
      {
         return this._alternateColor;
      }
      
      public function set alternateRows(param1:Boolean) : void
      {
         this._alternateRows = param1;
         invalidate();
      }
      
      public function get alternateRows() : Boolean
      {
         return this._alternateRows;
      }
      
      public function set autoHideScrollBar(param1:Boolean) : void
      {
         this._scrollbar.autoHide = param1;
      }
      
      public function get autoHideScrollBar() : Boolean
      {
         return this._scrollbar.autoHide;
      }
      
      public function get spacing() : uint
      {
         return this._spacing;
      }
      
      public function set spacing(param1:uint) : void
      {
         this._spacing = param1;
         invalidate();
      }
      
      public function get labelShift() : uint
      {
         return this._labelShift;
      }
      
      public function set labelShift(param1:uint) : void
      {
         this._labelShift = param1;
         invalidate();
      }
   }
}

