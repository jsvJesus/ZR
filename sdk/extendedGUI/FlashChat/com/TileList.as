package com
{
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.utils.getDefinitionByName;
   import scaleform.clik.constants.DirectionMode;
   import scaleform.clik.constants.InputValue;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.constants.NavigationCode;
   import scaleform.clik.constants.WrappingMode;
   import scaleform.clik.controls.CoreList;
   import scaleform.clik.controls.ScrollIndicator;
   import scaleform.clik.data.DataProvider;
   import scaleform.clik.data.ListDataSO;
   import scaleform.clik.events.InputEvent;
   import scaleform.clik.interfaces.IScrollBar;
   import scaleform.clik.ui.InputDetails;
   import scaleform.clik.utils.Padding;
   
   public class TileList extends CoreList
   {
      public var wrapping:String = "normal";
      
      public var thumbOffset:Object;
      
      public var thumbSizeFactor:Number = 1;
      
      public var externalColumnCount:Number = 0;
      
      protected var _rowHeight:Number = NaN;
      
      protected var _autoRowHeight:Number = NaN;
      
      protected var _totalRows:uint = 0;
      
      protected var _columnWidth:Number = NaN;
      
      protected var _autoColumnWidth:Number = NaN;
      
      protected var _totalColumns:uint = 0;
      
      protected var _scrollPosition:uint = 0;
      
      protected var _autoScrollBar:Boolean = false;
      
      protected var _scrollBarValue:Object;
      
      protected var _margin:Number = 0;
      
      protected var _padding:Padding;
      
      protected var _direction:String = "horizontal";
      
      public var _dataArray:Array = new Array();
      
      protected var _scrollBar:IScrollBar;
      
      public function TileList()
      {
         super();
      }
      
      override protected function initialize() : void
      {
         super.initialize();
      }
      
      public function get scrollBar() : Object
      {
         return this._scrollBar;
      }
      
      public function set scrollBar(value:Object) : void
      {
         this._scrollBarValue = value;
         invalidate(InvalidationType.SCROLL_BAR);
      }
      
      public function get rowHeight() : Number
      {
         return isNaN(this._autoRowHeight) ? this._rowHeight : this._autoRowHeight;
      }
      
      public function set rowHeight(value:Number) : void
      {
         if(value == 0)
         {
            value = NaN;
            if(_inspector)
            {
               return;
            }
         }
         this._rowHeight = value;
         this._autoRowHeight = NaN;
         invalidateSize();
      }
      
      public function get columnWidth() : Number
      {
         return isNaN(this._autoColumnWidth) ? this._columnWidth : this._autoColumnWidth;
      }
      
      public function set columnWidth(value:Number) : void
      {
         if(value == 0)
         {
            value = NaN;
            if(_inspector)
            {
               return;
            }
         }
         this._columnWidth = value;
         this._autoColumnWidth = NaN;
         invalidateSize();
      }
      
      public function getSizeTabs() : Number
      {
         return this._columnWidth * this.columnCount;
      }
      
      public function set dataArray(value:Array) : void
      {
         this._dataArray = value;
         dataProvider = new DataProvider(this._dataArray);
      }
      
      public function get dataArray() : Array
      {
         return this._dataArray;
      }
      
      public function addTab(i:Number = -1, s:Object = null) : *
      {
         if(i == -1)
         {
            this._dataArray.push(s);
            dataProvider = new DataProvider(this._dataArray);
         }
         else
         {
            this._dataArray.splice(i,0,s);
            dataProvider = new DataProvider(this._dataArray);
         }
      }
      
      public function delTabI(i:Number) : *
      {
         this._dataArray.splice(i,1);
         dataProvider = new DataProvider(this._dataArray);
      }
      
      public function get rowCount() : uint
      {
         return _totalRenderers;
      }
      
      public function set rowCount(value:uint) : void
      {
         var h:Number = this.rowHeight;
         if(isNaN(this.rowHeight))
         {
            this.calculateRendererTotal(this.availableWidth,this.availableHeight);
         }
         h = this.rowHeight;
         height = h * value + this.margin + this.padding.horizontal;
      }
      
      public function get columnCount() : uint
      {
         return this._totalColumns;
      }
      
      public function set columnCount(value:uint) : void
      {
         var w:Number = this._columnWidth;
         if(isNaN(this._columnWidth))
         {
            this.calculateRendererTotal(this.availableWidth,this.availableHeight);
         }
         w = this._columnWidth;
         width = w * value + this.margin + this.padding.horizontal;
      }
      
      public function get direction() : String
      {
         return this._direction;
      }
      
      public function set direction(value:String) : void
      {
         if(value == this._direction)
         {
            return;
         }
         this._direction = value;
         invalidate();
      }
      
      override public function set selectedIndex(value:int) : void
      {
         if(value == _selectedIndex || value == _newSelectedIndex)
         {
            return;
         }
         _newSelectedIndex = value;
         invalidateSelectedIndex();
      }
      
      public function get margin() : Number
      {
         return this._margin;
      }
      
      public function set margin(value:Number) : void
      {
         this._margin = value;
         invalidateSize();
      }
      
      public function get padding() : Padding
      {
         return this._padding;
      }
      
      public function set padding(value:Padding) : void
      {
         this._padding = value;
         invalidateSize();
      }
      
      public function set inspectablePadding(value:Object) : void
      {
         if(!componentInspectorSetting)
         {
            return;
         }
         this.padding = new Padding(value.top,value.right,value.bottom,value.left);
      }
      
      override public function get availableWidth() : Number
      {
         return Math.round(_width) - this.margin * 2 - (this._direction == DirectionMode.VERTICAL && this._autoScrollBar ? Math.round(this._scrollBar.width) : 0);
      }
      
      override public function get availableHeight() : Number
      {
         return Math.round(_height) - this.margin * 2 - (this._direction == DirectionMode.HORIZONTAL && this._autoScrollBar ? Math.round(this._scrollBar.width) : 0);
      }
      
      public function get scrollPosition() : Number
      {
         return this._scrollPosition;
      }
      
      public function set scrollPosition(value:Number) : void
      {
         var maxScrollPosition:Number = Math.ceil((_dataProvider.length - this._totalRows * this._totalColumns) / (this._direction == "horizontal" ? this._totalRows : this._totalColumns));
         value = Math.max(0,Math.min(maxScrollPosition,Math.round(value)));
         if(this._scrollPosition == value)
         {
            return;
         }
         this._scrollPosition = value;
         invalidateData();
         invalidateState();
      }
      
      override public function getRendererAt(index:uint, offset:int = 0) : MovieClip
      {
         if(_renderers == null)
         {
            return null;
         }
         var rendererIndex:uint = index - offset * (this._direction == DirectionMode.HORIZONTAL ? this._totalRows : this._totalColumns);
         if(rendererIndex >= _renderers.length)
         {
            return null;
         }
         return _renderers[rendererIndex] as MovieClip;
      }
      
      override public function scrollToIndex(index:uint) : void
      {
         if(_totalRenderers == 0)
         {
            return;
         }
         var factor:Number = this._direction == DirectionMode.HORIZONTAL ? this._totalRows : this._totalColumns;
         var startIndex:Number = this._scrollPosition * factor;
         if(factor == 0)
         {
            return;
         }
         if(index >= startIndex && index < startIndex + this._totalRows * this._totalColumns)
         {
            return;
         }
         if(index < startIndex)
         {
            this.scrollPosition = index / factor >> 0;
         }
         else
         {
            this.scrollPosition = Math.floor(index / factor) - (this._direction == DirectionMode.HORIZONTAL ? this._totalColumns : this._totalRows) + 1;
         }
      }
      
      override public function handleInput(event:InputEvent) : void
      {
         if(event.handled)
         {
            return;
         }
         var renderer:MovieClip = this.getRendererAt(_selectedIndex,this._scrollPosition);
         if(renderer != null)
         {
            renderer.handleInput(event);
            if(event.handled)
            {
               return;
            }
         }
         var details:InputDetails = event.details;
         var keyPress:Boolean = details.value == InputValue.KEY_DOWN || details.value == InputValue.KEY_HOLD;
         var nextIndex:uint = NaN;
         var nav:String = details.navEquivalent;
         if(this._direction == DirectionMode.HORIZONTAL)
         {
            switch(nav)
            {
               case NavigationCode.RIGHT:
                  nextIndex = uint(_selectedIndex + this._totalRows);
                  break;
               case NavigationCode.LEFT:
                  nextIndex = uint(_selectedIndex - this._totalRows);
                  break;
               case NavigationCode.UP:
                  nextIndex = uint(_selectedIndex - 1);
                  break;
               case NavigationCode.DOWN:
                  nextIndex = uint(_selectedIndex + 1);
            }
         }
         else
         {
            switch(nav)
            {
               case NavigationCode.DOWN:
                  nextIndex = uint(_selectedIndex + this._totalColumns);
                  break;
               case NavigationCode.UP:
                  nextIndex = uint(_selectedIndex - this._totalColumns);
                  break;
               case NavigationCode.LEFT:
                  nextIndex = uint(_selectedIndex - 1);
                  break;
               case NavigationCode.RIGHT:
                  nextIndex = uint(_selectedIndex + 1);
            }
         }
         if(isNaN(nextIndex))
         {
            switch(nav)
            {
               case NavigationCode.HOME:
                  nextIndex = 0;
                  break;
               case NavigationCode.END:
                  nextIndex = _dataProvider.length - 1;
                  break;
               case NavigationCode.PAGE_DOWN:
                  nextIndex = Math.min(_dataProvider.length - 1,_selectedIndex + this._totalColumns * this._totalRows);
                  break;
               case NavigationCode.PAGE_UP:
                  nextIndex = Math.max(0,_selectedIndex - this._totalColumns * this._totalRows);
            }
         }
         if(!isNaN(nextIndex))
         {
            if(!keyPress)
            {
               event.handled = true;
               return;
            }
            if(nextIndex >= 0 && nextIndex < dataProvider.length)
            {
               this.selectedIndex = Math.max(0,Math.min(_dataProvider.length - 1,nextIndex));
               event.handled = true;
            }
            else if(this.wrapping == WrappingMode.STICK)
            {
               nextIndex = Math.max(0,Math.min(_dataProvider.length - 1,nextIndex));
               if(selectedIndex != nextIndex)
               {
                  this.selectedIndex = nextIndex;
               }
               event.handled = true;
            }
            else if(this.wrapping == WrappingMode.WRAP)
            {
               this.selectedIndex = nextIndex < 0 ? int(_dataProvider.length - 1) : (selectedIndex < _dataProvider.length - 1 ? int(_dataProvider.length - 1) : 0);
               event.handled = true;
            }
         }
      }
      
      override public function toString() : String
      {
         return "[CLIK TileList " + name + "]";
      }
      
      override protected function configUI() : void
      {
         super.configUI();
         if(this.padding == null)
         {
            this.padding = new Padding();
         }
         if(_itemRenderer == null && !_usingExternalRenderers)
         {
            itemRendererName = _itemRendererName;
         }
      }
      
      override protected function draw() : void
      {
         if(isInvalid(InvalidationType.SCROLL_BAR))
         {
            this.createScrollBar();
         }
         if(isInvalid(InvalidationType.RENDERERS))
         {
            this._autoRowHeight = NaN;
            this._autoColumnWidth = NaN;
            if(_usingExternalRenderers)
            {
               this._totalColumns = this.externalColumnCount == 0 ? 1 : uint(this.externalColumnCount);
               this._totalRows = Math.ceil(_renderers.length / this._totalColumns);
            }
         }
         super.draw();
         if(isInvalid(InvalidationType.DATA))
         {
            this.updateScrollBar();
         }
      }
      
      protected function createScrollBar() : void
      {
         var sb:IScrollBar = null;
         var classRef:Class = null;
         var sbInst:Object = null;
         if(this._scrollBar)
         {
            this._scrollBar.removeEventListener(Event.SCROLL,this.handleScroll);
            this._scrollBar.removeEventListener(Event.CHANGE,this.handleScroll);
            this._scrollBar.focusTarget = null;
            if(container.contains(this._scrollBar as DisplayObject))
            {
               container.removeChild(this._scrollBar as DisplayObject);
            }
            this._scrollBar = null;
         }
         if(!this._scrollBarValue || this._scrollBarValue == "")
         {
            return;
         }
         this._autoScrollBar = false;
         if(this._scrollBarValue is String)
         {
            if(parent != null)
            {
               sb = parent.getChildByName(this._scrollBarValue.toString()) as IScrollBar;
            }
            if(sb == null)
            {
               classRef = getDefinitionByName(this._scrollBarValue.toString()) as Class;
               if(classRef)
               {
                  sb = new classRef() as IScrollBar;
               }
               if(sb)
               {
                  this._autoScrollBar = true;
                  sbInst = sb as Object;
                  if(Boolean(sbInst) && Boolean(this.thumbOffset))
                  {
                     sbInst.offsetTop = this.thumbOffset.top;
                     sbInst.offsetBottom = this.thumbOffset.bottom;
                  }
                  sb.addEventListener(MouseEvent.MOUSE_WHEEL,this.blockMouseWheel,false,0,true);
                  container.addChild(sb as DisplayObject);
               }
            }
         }
         else if(this._scrollBarValue is Class)
         {
            sb = new (this._scrollBarValue as Class)() as IScrollBar;
            sb.addEventListener(MouseEvent.MOUSE_WHEEL,this.blockMouseWheel,false,0,true);
            if(sb != null)
            {
               this._autoScrollBar = true;
               (sb as Object).offsetTop = this.thumbOffset.top;
               (sb as Object).offsetBottom = this.thumbOffset.bottom;
               container.addChild(sb as DisplayObject);
            }
         }
         else
         {
            sb = this._scrollBarValue as IScrollBar;
         }
         this._scrollBar = sb;
         invalidateSize();
         if(this._scrollBar == null)
         {
            return;
         }
         this._scrollBar.addEventListener(Event.SCROLL,this.handleScroll,false,0,true);
         this._scrollBar.addEventListener(Event.CHANGE,this.handleScroll,false,0,true);
         this._scrollBar.focusTarget = this;
         this._scrollBar.tabEnabled = false;
      }
      
      override protected function calculateRendererTotal(width:Number, height:Number) : uint
      {
         var renderer:MovieClip = null;
         var invalidRowHeight:Boolean = isNaN(this._rowHeight) && isNaN(this._autoRowHeight);
         var invalidColumnWidth:Boolean = isNaN(this._columnWidth) && isNaN(this._autoColumnWidth);
         if(invalidRowHeight || invalidColumnWidth)
         {
            renderer = createRenderer(0);
            if(invalidRowHeight)
            {
               this._autoRowHeight = renderer.height;
            }
            if(invalidColumnWidth)
            {
               this._autoColumnWidth = renderer.width;
            }
            cleanUpRenderer(renderer);
         }
         this._totalRows = this.availableHeight / this.rowHeight >> 0;
         this._totalColumns = this.availableWidth / this.columnWidth >> 0;
         _totalRenderers = this._totalRows * this._totalColumns;
         return _totalRenderers;
      }
      
      override protected function updateSelectedIndex() : void
      {
         if(_selectedIndex == _newSelectedIndex)
         {
            return;
         }
         if(_totalRenderers == 0)
         {
            return;
         }
         var renderer:MovieClip = this.getRendererAt(_selectedIndex,this.scrollPosition);
         if(renderer != null)
         {
            renderer.selected = false;
            renderer.validateNow();
         }
         super.selectedIndex = _newSelectedIndex;
         if(_selectedIndex < 0 || _selectedIndex >= _dataProvider.length)
         {
            return;
         }
         renderer = this.getRendererAt(_selectedIndex,this._scrollPosition);
         if(renderer != null)
         {
            renderer.selected = true;
            renderer.validateNow();
         }
         else
         {
            this.scrollToIndex(_selectedIndex);
            renderer = this.getRendererAt(_selectedIndex,this.scrollPosition);
            renderer.selected = true;
            renderer.validateNow();
         }
      }
      
      override protected function refreshData() : void
      {
         var itemsPerSet:Number = this._direction == DirectionMode.HORIZONTAL ? this._totalRows : this._totalColumns;
         var numberOfSets:Number = Math.ceil(_dataProvider.length / itemsPerSet);
         var maxScrollPosition:Number = numberOfSets - (this._direction == DirectionMode.HORIZONTAL ? this._totalColumns : this._totalRows);
         this._scrollPosition = Math.max(0,Math.min(maxScrollPosition,this._scrollPosition));
         var startIndex:Number = this._scrollPosition * itemsPerSet;
         var endIndex:Number = startIndex + this._totalColumns * this._totalRows - 1;
         this.selectedIndex = Math.min(_dataProvider.length - 1,_selectedIndex);
         _dataProvider.requestItemRange(startIndex,endIndex,this.populateData);
      }
      
      override protected function drawLayout() : void
      {
         var renderer:MovieClip = null;
         var l:uint = _renderers.length;
         var h:Number = this.rowHeight;
         var w:Number = this.columnWidth;
         var rx:Number = this.margin + this.padding.left;
         var ry:Number = this.margin + this.padding.top;
         var dataWillChange:Boolean = isInvalid(InvalidationType.DATA);
         for(var i:uint = 0; i < l; i++)
         {
            renderer = this.getRendererAt(i);
            if(this.direction == DirectionMode.HORIZONTAL)
            {
               renderer.y = i % this._totalRows * h + this.margin;
               renderer.x = (i / this._totalRows >> 0) * w + this.margin;
            }
            else
            {
               renderer.x = i % this._totalColumns * w + this.margin;
               renderer.y = (i / this._totalColumns >> 0) * h + this.margin;
            }
            renderer.width = w;
            renderer.height = h;
            if(!dataWillChange)
            {
               renderer.validateNow();
            }
         }
         this.drawScrollBar();
      }
      
      override protected function changeFocus() : void
      {
         super.changeFocus();
         var renderer:MovieClip = this.getRendererAt(_selectedIndex,this._scrollPosition);
         if(renderer != null)
         {
            renderer.displayFocus = focused > 0;
            renderer.validateNow();
         }
      }
      
      protected function populateData(data:Array) : void
      {
         var renderer:MovieClip = null;
         var index:uint = 0;
         var listData:ListDataSO = null;
         var dl:uint = data.length;
         var l:uint = _renderers.length;
         for(var i:uint = 0; i < l; i++)
         {
            renderer = this.getRendererAt(i);
            if(renderer != null)
            {
               index = this._scrollPosition * (this._direction == DirectionMode.HORIZONTAL ? this._totalRows : this._totalColumns) + i;
               listData = new ListDataSO(index,itemToLabel(data[i]),_selectedIndex + this._scrollPosition == index,this.itemToState(data[i]));
               renderer.enabled = i >= dl ? false : true;
               renderer.setListData(listData);
               renderer.setData(data[i]);
               renderer.selected = _selectedIndex + this._scrollPosition == index ? true : false;
               renderer.validateNow();
            }
         }
      }
      
      protected function itemToState(Item:Object) : String
      {
         try
         {
            if(Item.state == undefined)
            {
               return "";
            }
         }
         catch(e:Error)
         {
            return "";
         }
         return Item.state;
      }
      
      protected function drawScrollBar() : void
      {
         if(!this._autoScrollBar)
         {
            return;
         }
         var sb:ScrollIndicator = this._scrollBar as ScrollIndicator;
         sb.direction = this._direction;
         if(this._direction == DirectionMode.VERTICAL)
         {
            sb.rotation = 0;
            sb.x = _width - sb.width + this.margin;
            sb.y = this.margin;
            sb.height = this.availableHeight;
         }
         else
         {
            sb.rotation = -90;
            sb.x = this.margin;
            sb.y = _height - this.margin;
            sb.width = this.availableWidth;
         }
         this._scrollBar.validateNow();
      }
      
      protected function updateScrollBar() : void
      {
         var max:Number = NaN;
         var scrollIndicator:ScrollIndicator = null;
         if(this._scrollBar == null)
         {
            return;
         }
         if(this.direction == DirectionMode.HORIZONTAL)
         {
            max = Math.ceil(_dataProvider.length / this._totalRows) - this._totalColumns;
         }
         else
         {
            max = Math.ceil(_dataProvider.length / this._totalColumns) - this._totalRows;
         }
         if(this._scrollBar is ScrollIndicator)
         {
            scrollIndicator = this._scrollBar as ScrollIndicator;
            scrollIndicator.setScrollProperties(this._direction == DirectionMode.HORIZONTAL ? this._totalColumns : this._totalRows,0,max);
         }
         this._scrollBar.position = this._scrollPosition;
         this._scrollBar.validateNow();
      }
      
      protected function handleScroll(event:Event) : void
      {
         this.scrollPosition = this._scrollBar.position;
      }
      
      override protected function scrollList(delta:int) : void
      {
         this.scrollPosition -= delta;
         dispatchEvent(new Event(Event.SCROLL));
      }
      
      protected function blockMouseWheel(event:MouseEvent) : void
      {
         event.stopPropagation();
      }
   }
}

