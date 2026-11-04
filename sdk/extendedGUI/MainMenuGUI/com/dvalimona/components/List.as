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
      
      protected var _show_background:Boolean = false;
      
      protected var _spacing:uint = 0;
      
      protected var _labelShift:uint = 0;
      
      public function List(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, items:Array = null)
      {
         if(items != null)
         {
            this._items = items;
         }
         else
         {
            this._items = new Array();
         }
         super(parent,xpos,ypos);
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
         var item:ListItem = null;
         while(this._itemHolder.numChildren > 0)
         {
            item = ListItem(this._itemHolder.getChildAt(0));
            item.removeEventListener(MouseEvent.CLICK,this.onSelect);
            this._itemHolder.removeChildAt(0);
         }
         this._listItems = new Array();
         var numItems:int = Math.ceil(_height / this._listItemHeight);
         numItems = Math.min(numItems,this._items.length);
         numItems = Math.max(numItems,0);
         for(var i:int = 0; i < numItems; i++)
         {
            item = new this._listItemClass(this._itemHolder,0,i * this._listItemHeight + i * this.spacing);
            this._listItems.push(item);
            item.setSize(width - this._scrollbar.width,this._listItemHeight);
            item.defaultColor = this._defaultColor;
            item.selectedColor = this._selectedColor;
            item.rolloverColor = this._rolloverColor;
            item.addEventListener(MouseEvent.CLICK,this.onSelect);
            item.doubleClickEnabled = true;
            item.addEventListener(MouseEvent.DOUBLE_CLICK,this.onDoubleClick);
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
            for(i = 0; i < numItems; i++)
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
            }
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.DEBUG,"ERROR List.fillItems:",error);
         }
      }
      
      public function set showBackground(value:Boolean) : *
      {
         this._show_background = value;
         this.draw();
      }
      
      public function get showBackground() : Boolean
      {
         return this._show_background;
      }
      
      protected function scrollToSelection() : void
      {
         var numItems:int = Math.ceil(_height / this._listItemHeight);
         if(this._selectedIndex != -1)
         {
            if(this._scrollbar.value <= this._selectedIndex)
            {
               if(this._scrollbar.value + numItems < this._selectedIndex)
               {
                  this._scrollbar.value = this._selectedIndex - numItems + 1;
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
         var prevIndex:int = int(this._selectedIndex);
         this._selectedIndex = Math.min(this._selectedIndex,this._items.length - 1);
         if(prevIndex != this._selectedIndex)
         {
            dispatchEvent(new Event(Event.SELECT));
         }
         this._panel.setSize(_width,_height);
         this._panel.color = this._defaultColor;
         this._panel.draw();
         this._itemHolder.graphics.clear();
         if(this._show_background)
         {
            this._itemHolder.graphics.beginFill(0);
            this._itemHolder.graphics.drawRect(0,0,_width,_height);
            this._itemHolder.graphics.endFill();
         }
         this._scrollbar.x = _width - 10;
         var contentHeight:Number = this._items.length * this._listItemHeight;
         this._scrollbar.setThumbPercent(_height / contentHeight);
         var pageSize:Number = Math.floor(_height / this._listItemHeight);
         this._scrollbar.maximum = Math.max(0,this._items.length - pageSize);
         this._scrollbar.pageSize = pageSize;
         this._scrollbar.height = _height + this._items.length * this.spacing;
         this._scrollbar.draw();
         this.scrollToSelection();
      }
      
      public function addItem(item:Object) : void
      {
         this._items.push(item);
         invalidate();
         this.makeListItems();
         this.fillItems();
      }
      
      public function addItemAt(item:Object, index:int) : void
      {
         index = Math.max(0,index);
         index = Math.min(this._items.length,index);
         this._items.splice(index,0,item);
         invalidate();
         this.makeListItems();
         this.fillItems();
      }
      
      public function removeItem(item:Object) : void
      {
         var index:int = int(this._items.indexOf(item));
         this.removeItemAt(index);
      }
      
      public function removeItemAt(index:int) : void
      {
         if(index < 0 || index >= this._items.length)
         {
            return;
         }
         this._items.splice(index,1);
         invalidate();
         this.makeListItems();
      }
      
      public function removeAll() : void
      {
         this._items = new Array();
         invalidate();
         this.makeListItems();
      }
      
      protected function onSelect(event:Event) : void
      {
         var offset:int = 0;
         var i:int = 0;
         try
         {
            if(!(event.target is ListItem))
            {
               return;
            }
            offset = int(this._scrollbar.value);
            for(i = 0; i < this._itemHolder.numChildren; i++)
            {
               if(this._itemHolder.getChildAt(i) == event.target)
               {
                  this._selectedIndex = i + offset;
               }
               ListItem(this._itemHolder.getChildAt(i)).selected = false;
            }
            ListItem(event.target).selected = true;
            dispatchEvent(new Event(Event.SELECT));
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.DEBUG,"ERROR List.onSelect:",error);
         }
      }
      
      protected function onDoubleClick(event:MouseEvent) : void
      {
         dispatchEvent(new Event(Event.OPEN));
      }
      
      protected function onScroll(event:Event) : void
      {
         this.fillItems();
      }
      
      protected function onMouseWheel(event:MouseEvent) : void
      {
         this._scrollbar.value -= event.delta;
         this.fillItems();
      }
      
      protected function onResize(event:Event) : void
      {
         this.makeListItems();
         this.fillItems();
      }
      
      public function set selectedIndex(value:int) : void
      {
         if(value >= 0 && value < this._items.length)
         {
            this._selectedIndex = value;
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
      
      public function set selectedItem(item:Object) : void
      {
         var index:int = int(this._items.indexOf(item));
         this.selectedIndex = index;
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
      
      public function set defaultColor(value:uint) : void
      {
         this._defaultColor = value;
         invalidate();
      }
      
      public function get defaultColor() : uint
      {
         return this._defaultColor;
      }
      
      public function set selectedColor(value:uint) : void
      {
         this._selectedColor = value;
         invalidate();
      }
      
      public function get selectedColor() : uint
      {
         return this._selectedColor;
      }
      
      public function set rolloverColor(value:uint) : void
      {
         this._rolloverColor = value;
         invalidate();
      }
      
      public function get rolloverColor() : uint
      {
         return this._rolloverColor;
      }
      
      public function set listItemHeight(value:Number) : void
      {
         this._listItemHeight = value;
         this.makeListItems();
         invalidate();
      }
      
      public function get listItemHeight() : Number
      {
         return this._listItemHeight;
      }
      
      public function set items(value:Array) : void
      {
         this._items = value;
         invalidate();
      }
      
      public function get items() : Array
      {
         return this._items;
      }
      
      public function set listItemClass(value:Class) : void
      {
         this._listItemClass = value;
         this.makeListItems();
         invalidate();
      }
      
      public function get listItemClass() : Class
      {
         return this._listItemClass;
      }
      
      public function set alternateColor(value:uint) : void
      {
         this._alternateColor = value;
         invalidate();
      }
      
      public function get alternateColor() : uint
      {
         return this._alternateColor;
      }
      
      public function set alternateRows(value:Boolean) : void
      {
         this._alternateRows = value;
         invalidate();
      }
      
      public function get alternateRows() : Boolean
      {
         return this._alternateRows;
      }
      
      public function set autoHideScrollBar(value:Boolean) : void
      {
         this._scrollbar.autoHide = value;
      }
      
      public function get autoHideScrollBar() : Boolean
      {
         return this._scrollbar.autoHide;
      }
      
      public function get spacing() : uint
      {
         return this._spacing;
      }
      
      public function set spacing(value:uint) : void
      {
         this._spacing = value;
         invalidate();
      }
      
      public function get labelShift() : uint
      {
         return this._labelShift;
      }
      
      public function set labelShift(value:uint) : void
      {
         this._labelShift = value;
         invalidate();
      }
   }
}

