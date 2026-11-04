package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.display.Stage;
   import flash.events.*;
   import flash.geom.*;
   
   public class ComboBox extends Component
   {
      public static const TOP:String = "top";
      
      public static const BOTTOM:String = "bottom";
      
      protected var _defaultLabel:String = "";
      
      protected var _dropDownButton:PushButton;
      
      protected var _items:Array;
      
      protected var _labelButton:PushButton;
      
      protected var _list:List;
      
      protected var _numVisibleItems:int = 6;
      
      protected var _open:Boolean = false;
      
      protected var _openPosition:String = "bottom";
      
      protected var _stage:Stage;
      
      private var _listWidth:uint;
      
      private var _listShift:int = 0;
      
      protected var _spacing:uint = 0;
      
      public function ComboBox(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, defaultLabel:String = "", items:Array = null)
      {
         this._defaultLabel = defaultLabel;
         this._items = items;
         addEventListener(Event.ADDED_TO_STAGE,this._onAddedToStageHandler);
         addEventListener(Event.REMOVED_FROM_STAGE,this.onRemovedFromStage);
         super(parent,xpos,ypos);
      }
      
      override protected function init() : void
      {
         super.init();
         setSize(100,20);
         this.setLabelButtonLabel();
      }
      
      public function set text(value:String) : *
      {
         this._labelButton.label = value;
      }
      
      override protected function addChildren() : void
      {
         this._list = new List(null,0,0,this._items);
         this._list.autoHideScrollBar = true;
         this._list.addEventListener(Event.SELECT,this.onSelect);
         this._list.showBackground = true;
         this._labelButton = new PushButton(this,0,0,"",this.onDropDown);
         this._labelButton.align = Label.LEFT;
         this._labelButton.marginLeft = 10;
         this._labelButton.tabEnabled = false;
         this._labelButton.upColor = 16777215;
         this._labelButton.upColorAlpha = 0.6;
         this._labelButton.labelUpColor = 0;
         this._labelButton.overColor = 16777215;
         this._labelButton.overColorAlpha = 0.8;
         this._labelButton.labelOverColor = 1392202;
         this._labelButton.downColor = 16777215;
         this._labelButton.downColorAlpha = 1;
         this._dropDownButton = new PushButton(this,0,0,"+",this.onDropDown);
         this._dropDownButton.upColor = 16777215;
         this._dropDownButton.upColorAlpha = 0.6;
         this._dropDownButton.labelUpColor = 0;
         this._dropDownButton.overColor = 16777215;
         this._dropDownButton.overColorAlpha = 0.8;
         this._dropDownButton.labelOverColor = 1392202;
         this._dropDownButton.focusMarginX = 0;
         this._dropDownButton.focusMarginY = 0;
         this._dropDownButton.downColor = 16777215;
         this._dropDownButton.downColorAlpha = 1;
         addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseDown);
      }
      
      protected function onMouseDown(event:MouseEvent) : void
      {
         event.stopImmediatePropagation();
      }
      
      protected function setLabelButtonLabel() : void
      {
         if(this.selectedItem == null)
         {
            this._labelButton.label = this._defaultLabel;
         }
         else if(this.selectedItem is String)
         {
            this._labelButton.label = this.selectedItem as String;
         }
         else if(Boolean(this.selectedItem.hasOwnProperty("label")) && this.selectedItem.label is String)
         {
            this._labelButton.label = this.selectedItem.label;
         }
         else
         {
            this._labelButton.label = this.selectedItem.toString();
         }
      }
      
      public function removeList() : void
      {
         this._open = false;
         if(this._stage.contains(this._list))
         {
            Base.comboLevel.removeChild(this._list);
         }
         this._stage.removeEventListener(MouseEvent.CLICK,this.onStageClick);
         this._dropDownButton.label = "+";
      }
      
      override public function draw() : void
      {
         super.draw();
         this._labelButton.setSize(_width - _height - 1,_height);
         this._labelButton.draw();
         this._dropDownButton.setSize(_height,_height);
         this._dropDownButton.draw();
         this._dropDownButton.x = _width - height;
         this._list.setSize(_width - _height - 1 + 10,this._numVisibleItems * this._list.listItemHeight);
      }
      
      public function addItem(item:Object) : void
      {
         this._list.addItem(item);
      }
      
      public function addItemAt(item:Object, index:int) : void
      {
         this._list.addItemAt(item,index);
      }
      
      public function removeItem(item:Object) : void
      {
         this._list.removeItem(item);
      }
      
      public function removeItemAt(index:int) : void
      {
         this._list.removeItemAt(index);
      }
      
      public function removeAll() : void
      {
         this._list.removeAll();
      }
      
      protected function onDropDown(event:MouseEvent) : void
      {
         var point:Point = null;
         this._open = !this._open;
         if(this._open)
         {
            point = new Point();
            if(this._openPosition == BOTTOM)
            {
               point.y = _height;
            }
            else
            {
               point.y = -this._numVisibleItems * this._list.listItemHeight;
            }
            point = this.localToGlobal(point);
            this._list.move(point.x + this.listShift,point.y);
            Base.comboLevel.addChild(this._list);
            this._stage.addEventListener(MouseEvent.CLICK,this.onStageClick);
            this._dropDownButton.label = "-";
         }
         else
         {
            this.removeList();
         }
      }
      
      protected function onStageClick(event:MouseEvent) : void
      {
         if(event.target == this._dropDownButton || event.target == this._labelButton)
         {
            return;
         }
         if(new Rectangle(this._list.x,this._list.y,this._list.width,this._list.height).contains(event.stageX,event.stageY))
         {
            return;
         }
         this._open = false;
         this.removeList();
      }
      
      protected function onSelect(event:Event) : void
      {
         this._open = false;
         this._dropDownButton.label = "+";
         if(stage != null && stage.contains(this._list))
         {
            Base.comboLevel.removeChild(this._list);
         }
         this.setLabelButtonLabel();
         dispatchEvent(event);
      }
      
      protected function _onAddedToStageHandler(event:Event) : void
      {
         this._stage = stage;
      }
      
      protected function onRemovedFromStage(event:Event) : void
      {
         this.removeList();
      }
      
      public function set selectedIndex(value:int) : void
      {
         this._list.selectedIndex = value;
         this.setLabelButtonLabel();
      }
      
      public function get selectedIndex() : int
      {
         return this._list.selectedIndex;
      }
      
      public function set selectedItem(item:Object) : void
      {
         this._list.selectedItem = item;
         this.setLabelButtonLabel();
      }
      
      public function get selectedItem() : Object
      {
         return this._list.selectedItem;
      }
      
      public function set defaultColor(value:uint) : void
      {
         this._list.defaultColor = value;
      }
      
      public function get defaultColor() : uint
      {
         return this._list.defaultColor;
      }
      
      public function set selectedColor(value:uint) : void
      {
         this._list.selectedColor = value;
      }
      
      public function get selectedColor() : uint
      {
         return this._list.selectedColor;
      }
      
      public function set rolloverColor(value:uint) : void
      {
         this._list.rolloverColor = value;
      }
      
      public function get rolloverColor() : uint
      {
         return this._list.rolloverColor;
      }
      
      public function set listItemHeight(value:Number) : void
      {
         this._list.listItemHeight = value;
         invalidate();
      }
      
      public function get listItemHeight() : Number
      {
         return this._list.listItemHeight;
      }
      
      public function set openPosition(value:String) : void
      {
         this._openPosition = value;
      }
      
      public function get openPosition() : String
      {
         return this._openPosition;
      }
      
      public function set defaultLabel(value:String) : void
      {
         this._defaultLabel = value;
         this.setLabelButtonLabel();
      }
      
      public function get defaultLabel() : String
      {
         return this._defaultLabel;
      }
      
      public function set numVisibleItems(value:int) : void
      {
         this._numVisibleItems = Math.max(1,value);
         invalidate();
      }
      
      public function get numVisibleItems() : int
      {
         return this._numVisibleItems;
      }
      
      public function set items(value:Array) : void
      {
         this._list.items = value;
      }
      
      public function get items() : Array
      {
         return this._list.items;
      }
      
      public function set listItemClass(value:Class) : void
      {
         this._list.listItemClass = value;
      }
      
      public function get listItemClass() : Class
      {
         return this._list.listItemClass;
      }
      
      public function set alternateColor(value:uint) : void
      {
         this._list.alternateColor = value;
      }
      
      public function get alternateColor() : uint
      {
         return this._list.alternateColor;
      }
      
      public function set alternateRows(value:Boolean) : void
      {
         this._list.alternateRows = value;
      }
      
      public function get alternateRows() : Boolean
      {
         return this._list.alternateRows;
      }
      
      public function set autoHideScrollBar(value:Boolean) : void
      {
         this._list.autoHideScrollBar = value;
         invalidate();
      }
      
      public function get autoHideScrollBar() : Boolean
      {
         return this._list.autoHideScrollBar;
      }
      
      public function get isOpen() : Boolean
      {
         return this._open;
      }
      
      public function get dropDownButton() : PushButton
      {
         return this._dropDownButton;
      }
      
      public function get labelButton() : PushButton
      {
         return this._labelButton;
      }
      
      public function get listComponent() : List
      {
         return this._list;
      }
      
      public function get listWidth() : uint
      {
         return this._listWidth;
      }
      
      public function set listWidth(value:uint) : void
      {
         this._listWidth = value;
         invalidate();
      }
      
      public function get listShift() : int
      {
         return this._listShift;
      }
      
      public function set listShift(value:int) : void
      {
         this._listShift = value;
      }
      
      public function get spacing() : uint
      {
         return this._spacing;
      }
      
      public function set spacing(value:uint) : void
      {
         this._spacing = value;
         this._list.spacing = this.spacing;
         invalidate();
      }
   }
}

