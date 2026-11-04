package scaleform.clik.controls
{
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.utils.getDefinitionByName;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.data.DataProvider;
   import scaleform.clik.events.ButtonEvent;
   import scaleform.clik.events.InputEvent;
   import scaleform.clik.events.ListEvent;
   import scaleform.clik.interfaces.IDataProvider;
   import scaleform.gfx.MouseEventEx;
   
   public class CoreList extends UIComponent
   {
      protected var _selectedIndex:int = -1;
      
      protected var _newSelectedIndex:int = -1;
      
      protected var _dataProvider:IDataProvider;
      
      protected var _labelField:String = "label";
      
      protected var _labelFunction:Function;
      
      protected var _itemRenderer:Class;
      
      protected var _itemRendererName:String = "DefaultListItemRenderer";
      
      public var _renderers:Vector.<MovieClip>;
      
      protected var _usingExternalRenderers:Boolean = false;
      
      protected var _totalRenderers:uint = 0;
      
      protected var _state:String = "default";
      
      protected var _newFrame:String;
      
      public var container:Sprite;
      
      public function CoreList()
      {
         super();
      }
      
      override protected function initialize() : void
      {
         this.dataProvider = new DataProvider();
         super.initialize();
      }
      
      override public function get focusable() : Boolean
      {
         return _focusable;
      }
      
      override public function set focusable(value:Boolean) : void
      {
         super.focusable = value;
      }
      
      public function get itemRendererName() : String
      {
         return this._itemRendererName;
      }
      
      public function set itemRendererName(value:String) : void
      {
         if(_inspector && value == "" || value == "")
         {
            return;
         }
         var classRef:Class = getDefinitionByName(value) as Class;
         if(classRef != null)
         {
            this.itemRenderer = classRef;
         }
      }
      
      public function get itemRenderer() : Class
      {
         return this._itemRenderer;
      }
      
      public function set itemRenderer(value:Class) : void
      {
         this._itemRenderer = value;
         this.invalidateRenderers();
      }
      
      public function set itemRendererInstanceName(value:String) : void
      {
         var clip:MovieClip = null;
         if(value == null || value == "" || parent == null)
         {
            return;
         }
         var i:uint = 0;
         var newRenderers:Vector.<MovieClip> = new Vector.<MovieClip>();
         while(++i)
         {
            clip = parent.getChildByName(value + i) as MovieClip;
            if(clip == null)
            {
               if(i != 0)
               {
                  break;
               }
            }
            else
            {
               newRenderers.push(clip);
            }
         }
         if(newRenderers.length == 0)
         {
            if(componentInspectorSetting)
            {
               return;
            }
            newRenderers = null;
         }
         this.itemRendererList = newRenderers;
      }
      
      public function set itemRendererList(value:Vector.<MovieClip>) : void
      {
         var l:uint = 0;
         var i:uint = 0;
         if(this._usingExternalRenderers)
         {
            l = this._renderers.length;
            for(i = 0; i < l; i++)
            {
               this.cleanUpRenderer(this.getRendererAt(i));
            }
         }
         this._usingExternalRenderers = value != null;
         this._renderers = value;
         if(this._usingExternalRenderers)
         {
            l = this._renderers.length;
            for(i = 0; i < l; i++)
            {
               this.setupRenderer(this.getRendererAt(i));
            }
            this._totalRenderers = this._renderers.length;
         }
         this.invalidateRenderers();
      }
      
      public function get selectedIndex() : int
      {
         return this._selectedIndex;
      }
      
      public function set selectedIndex(value:int) : void
      {
         if(this._selectedIndex == value)
         {
            return;
         }
         this._selectedIndex = value;
         this.invalidateSelectedIndex();
         dispatchEvent(new ListEvent(ListEvent.INDEX_CHANGE,true,false,this._selectedIndex,-1,-1,this.getRendererAt(this._selectedIndex),this.dataProvider[this._selectedIndex]));
      }
      
      override public function get enabled() : Boolean
      {
         return super.enabled;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         var l:uint = 0;
         var i:uint = 0;
         var renderer:MovieClip = null;
         super.enabled = value;
         this.setState(super.enabled ? "default" : "disabled");
         if(this._renderers != null)
         {
            l = this._renderers.length;
            for(i = 0; i < l; i++)
            {
               renderer = this.getRendererAt(i);
               renderer.enabled = this.enabled;
            }
         }
      }
      
      public function get dataProvider() : IDataProvider
      {
         return this._dataProvider;
      }
      
      public function set dataProvider(value:IDataProvider) : void
      {
         if(this._dataProvider == value)
         {
            return;
         }
         if(this._dataProvider != null)
         {
            this._dataProvider.removeEventListener(Event.CHANGE,this.handleDataChange,false);
         }
         this._dataProvider = value;
         if(this._dataProvider == null)
         {
            return;
         }
         this._dataProvider.addEventListener(Event.CHANGE,this.handleDataChange,false,0,true);
         invalidateData();
      }
      
      public function get labelField() : String
      {
         return this._labelField;
      }
      
      public function set labelField(value:String) : void
      {
         this._labelField = value;
         invalidateData();
      }
      
      public function get labelFunction() : Function
      {
         return this._labelFunction;
      }
      
      public function set labelFunction(value:Function) : void
      {
         this._labelFunction = value;
         invalidateData();
      }
      
      public function get availableWidth() : Number
      {
         return _width;
      }
      
      public function get availableHeight() : Number
      {
         return _height;
      }
      
      public function scrollToIndex(index:uint) : void
      {
      }
      
      public function scrollToSelected() : void
      {
         this.scrollToIndex(this._selectedIndex);
      }
      
      public function itemToLabel(item:Object) : String
      {
         if(item == null)
         {
            return "";
         }
         if(this._labelFunction != null)
         {
            return this._labelFunction(item);
         }
         if(this._labelField != null && this._labelField in item && item[this._labelField] != null)
         {
            return item[this._labelField];
         }
         return item.toString();
      }
      
      public function getRendererAt(index:uint, offset:int = 0) : MovieClip
      {
         if(this._renderers == null)
         {
            return null;
         }
         var newIndex:uint = uint(index - offset);
         if(newIndex >= this._renderers.length)
         {
            return null;
         }
         return this._renderers[newIndex] as MovieClip;
      }
      
      public function invalidateRenderers() : void
      {
         invalidate(InvalidationType.RENDERERS);
      }
      
      public function invalidateSelectedIndex() : void
      {
         invalidate(InvalidationType.SELECTED_INDEX);
      }
      
      override public function toString() : String
      {
         return "[CLIK CoreList " + name + "]";
      }
      
      override protected function configUI() : void
      {
         super.configUI();
         if(this.container == null)
         {
            this.container = new Sprite();
            addChild(this.container);
         }
         tabEnabled = _focusable && this.enabled;
         tabChildren = false;
         addEventListener(MouseEvent.MOUSE_WHEEL,this.handleMouseWheel,false,0,true);
         addEventListener(InputEvent.INPUT,handleInput,false,0,true);
      }
      
      override protected function draw() : void
      {
         var i:uint = 0;
         var l:uint = 0;
         var renderer:MovieClip = null;
         var displayObject:DisplayObject = null;
         if(isInvalid(InvalidationType.SELECTED_INDEX))
         {
            this.updateSelectedIndex();
         }
         if(isInvalid(InvalidationType.STATE))
         {
            if(this._newFrame)
            {
               gotoAndPlay(this._newFrame);
               this._newFrame = null;
            }
         }
         if(!this._usingExternalRenderers && isInvalid(InvalidationType.RENDERERS))
         {
            if(this._renderers != null)
            {
               l = this._renderers.length;
               for(i = 0; i < l; i++)
               {
                  renderer = this.getRendererAt(i);
                  this.cleanUpRenderer(renderer);
                  displayObject = renderer as DisplayObject;
                  if(this.container.contains(displayObject))
                  {
                     this.container.removeChild(displayObject);
                  }
               }
            }
            this._renderers = new Vector.<MovieClip>();
            invalidateData();
         }
         if(!this._usingExternalRenderers && isInvalid(InvalidationType.SIZE))
         {
            removeChild(this.container);
            setActualSize(_width,_height);
            this.container.scaleX = 1 / scaleX;
            this.container.scaleY = 1 / scaleY;
            this._totalRenderers = this.calculateRendererTotal(this.availableWidth,this.availableHeight);
            addChild(this.container);
            invalidateData();
         }
         if(!this._usingExternalRenderers && isInvalid(InvalidationType.RENDERERS,InvalidationType.SIZE))
         {
            this.drawRenderers(this._totalRenderers);
            this.drawLayout();
         }
         if(isInvalid(InvalidationType.DATA))
         {
            this.refreshData();
         }
      }
      
      override protected function changeFocus() : void
      {
         if(Boolean(_focused) || _displayFocus)
         {
            this.setState("focused","default");
         }
         else
         {
            this.setState("default");
         }
      }
      
      protected function refreshData() : void
      {
      }
      
      protected function updateSelectedIndex() : void
      {
      }
      
      protected function calculateRendererTotal(width:Number, height:Number) : uint
      {
         return height / 20 >> 0;
      }
      
      protected function drawLayout() : void
      {
      }
      
      protected function drawRenderers(total:Number) : void
      {
         var i:int = 0;
         var l:int = 0;
         var renderer:MovieClip = null;
         var displayObject:DisplayObject = null;
         if(this._itemRenderer == null)
         {
            return;
         }
         for(i = int(this._renderers.length); i < this._totalRenderers; i++)
         {
            renderer = this.createRenderer(i);
            if(renderer == null)
            {
               break;
            }
            this._renderers.push(renderer);
            this.container.addChild(renderer as DisplayObject);
         }
         l = int(this._renderers.length);
         for(i = l - 1; i >= this._totalRenderers; i--)
         {
            renderer = this.getRendererAt(i);
            if(renderer != null)
            {
               this.cleanUpRenderer(renderer);
               displayObject = renderer as DisplayObject;
               if(this.container.contains(displayObject))
               {
                  this.container.removeChild(displayObject);
               }
            }
            this._renderers.splice(i,1);
         }
      }
      
      protected function createRenderer(index:uint) : MovieClip
      {
         var renderer:MovieClip = new this._itemRenderer() as MovieClip;
         if(renderer == null)
         {
            return null;
         }
         this.setupRenderer(renderer);
         return renderer;
      }
      
      protected function setupRenderer(renderer:MovieClip) : void
      {
         renderer.owner = this;
         renderer.focusTarget = this;
         renderer.tabEnabled = false;
         renderer.doubleClickEnabled = true;
         renderer.addEventListener(ButtonEvent.PRESS,this.dispatchItemEvent,false,0,true);
         renderer.addEventListener(ButtonEvent.CLICK,this.handleItemClick,false,0,true);
         renderer.addEventListener(MouseEvent.DOUBLE_CLICK,this.dispatchItemEvent,false,0,true);
         renderer.addEventListener(MouseEvent.ROLL_OVER,this.dispatchItemEvent,false,0,true);
         renderer.addEventListener(MouseEvent.ROLL_OUT,this.dispatchItemEvent,false,0,true);
         if(this._usingExternalRenderers)
         {
            renderer.addEventListener(MouseEvent.MOUSE_WHEEL,this.handleMouseWheel,false,0,true);
         }
      }
      
      protected function cleanUpRenderer(renderer:MovieClip) : void
      {
         renderer.owner = null;
         renderer.focusTarget = null;
         renderer.doubleClickEnabled = false;
         renderer.removeEventListener(ButtonEvent.PRESS,this.dispatchItemEvent);
         renderer.removeEventListener(ButtonEvent.CLICK,this.handleItemClick);
         renderer.removeEventListener(MouseEvent.DOUBLE_CLICK,this.dispatchItemEvent);
         renderer.removeEventListener(MouseEvent.ROLL_OVER,this.dispatchItemEvent);
         renderer.removeEventListener(MouseEvent.ROLL_OUT,this.dispatchItemEvent);
         renderer.removeEventListener(MouseEvent.MOUSE_WHEEL,this.handleMouseWheel);
      }
      
      protected function dispatchItemEvent(event:Event) : Boolean
      {
         var type:String = null;
         switch(event.type)
         {
            case ButtonEvent.PRESS:
               type = ListEvent.ITEM_PRESS;
               break;
            case ButtonEvent.CLICK:
               type = ListEvent.ITEM_CLICK;
               break;
            case MouseEvent.ROLL_OVER:
               type = ListEvent.ITEM_ROLL_OVER;
               break;
            case MouseEvent.ROLL_OUT:
               type = ListEvent.ITEM_ROLL_OUT;
               break;
            case MouseEvent.DOUBLE_CLICK:
               type = ListEvent.ITEM_DOUBLE_CLICK;
               break;
            default:
               return true;
         }
         var renderer:MovieClip = event.currentTarget as MovieClip;
         var controllerIdx:uint = 0;
         if(event is ButtonEvent)
         {
            controllerIdx = (event as ButtonEvent).controllerIdx;
         }
         else if(event is MouseEventEx)
         {
            controllerIdx = (event as MouseEventEx).mouseIdx;
         }
         var buttonIdx:uint = 0;
         if(event is ButtonEvent)
         {
            buttonIdx = (event as ButtonEvent).buttonIdx;
         }
         else if(event is MouseEventEx)
         {
            buttonIdx = (event as MouseEventEx).buttonIdx;
         }
         var isKeyboard:Boolean = false;
         if(event is ButtonEvent)
         {
            isKeyboard = (event as ButtonEvent).isKeyboard;
         }
         var newEvent:ListEvent = new ListEvent(type,false,true,renderer.index,0,renderer.index,renderer,this.dataProvider[renderer.index],controllerIdx,buttonIdx,isKeyboard);
         return dispatchEvent(newEvent);
      }
      
      protected function handleDataChange(event:Event) : void
      {
         invalidate(InvalidationType.DATA);
      }
      
      protected function handleItemClick(event:ButtonEvent) : void
      {
         var index:Number = Number((event.currentTarget as MovieClip).index);
         if(isNaN(index))
         {
            return;
         }
         if(this.dispatchItemEvent(event))
         {
            this.selectedIndex = index;
         }
      }
      
      protected function handleMouseWheel(event:MouseEvent) : void
      {
         this.scrollList(event.delta > 0 ? 1 : -1);
      }
      
      protected function scrollList(delta:int) : void
      {
      }
      
      protected function setState(... states) : void
      {
         var onlyState:String = null;
         var thisState:String = null;
         if(states.length == 1)
         {
            onlyState = states[0].toString();
            if(this._state != onlyState && Boolean(_labelHash[onlyState]))
            {
               this._state = this._newFrame = onlyState;
               invalidateState();
            }
            return;
         }
         var l:uint = uint(states.length);
         for(var i:uint = 0; i < l; i++)
         {
            thisState = states[i].toString();
            if(_labelHash[thisState])
            {
               this._state = this._newFrame = thisState;
               invalidateState();
               break;
            }
         }
      }
   }
}

