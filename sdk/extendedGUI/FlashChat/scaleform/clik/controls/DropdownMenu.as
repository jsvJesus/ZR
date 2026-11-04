package scaleform.clik.controls
{
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.events.TimerEvent;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.utils.Timer;
   import flash.utils.getDefinitionByName;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InputValue;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.constants.NavigationCode;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.data.DataProvider;
   import scaleform.clik.events.ButtonEvent;
   import scaleform.clik.events.ComponentEvent;
   import scaleform.clik.events.InputEvent;
   import scaleform.clik.events.ListEvent;
   import scaleform.clik.interfaces.IDataProvider;
   import scaleform.clik.managers.PopUpManager;
   import scaleform.clik.ui.InputDetails;
   import scaleform.clik.utils.ConstrainedElement;
   import scaleform.clik.utils.Constraints;
   import scaleform.clik.utils.Padding;
   import scaleform.gfx.MouseEventEx;
   
   public class DropdownMenu extends UIComponent
   {
      public var dropdown:Object = "DefaultScrollingList";
      
      public var itemRenderer:Object = "DefaultListItemRenderer";
      
      public var scrollBar:Object;
      
      public var lockDragStateChange:Boolean = false;
      
      public var repeatDelay:Number = 500;
      
      public var repeatInterval:Number = 200;
      
      public var constraintsDisabled:Boolean = false;
      
      public var allowDeselect:Boolean = true;
      
      public var curID:Number = -1;
      
      public var curCom:String = "";
      
      public var color:uint;
      
      public var FontSize:Number = 14;
      
      public var menuWrapping:String = "normal";
      
      public var menuDirection:String = "down";
      
      public var menuWidth:Number = -1;
      
      public var menuMargin:Number = 1;
      
      public var menuRowCount:Number = 5;
      
      public var menuPadding:Padding;
      
      public var menuOffset:Padding;
      
      public var thumbOffsetTop:Number;
      
      public var thumbOffsetBottom:Number;
      
      protected var _selectedIndex:int = -1;
      
      protected var _dataProvider:IDataProvider;
      
      protected var _dataArray:Array = new Array();
      
      protected var _labelField:String = "label";
      
      protected var _labelFunction:Function;
      
      protected var _popup:MovieClip;
      
      protected var _toggle:Boolean = false;
      
      protected var _label:String;
      
      protected var _state:String;
      
      protected var _group:ButtonGroup;
      
      protected var _groupName:String;
      
      protected var _selected:Boolean = false;
      
      protected var _data:Object;
      
      protected var _autoRepeat:Boolean = false;
      
      protected var _autoSize:String = "none";
      
      protected var _pressedByKeyboard:Boolean = false;
      
      protected var _isRepeating:Boolean = false;
      
      protected var _owner:UIComponent = null;
      
      protected var _stateMap:Object = {
         "up":["up"],
         "over":["over"],
         "down":["down"],
         "release":["release","over"],
         "out":["out","up"],
         "disabled":["disabled"],
         "selecting":["selecting","over"],
         "toggle":["toggle","up"],
         "kb_selecting":["kb_selecting","up"],
         "kb_release":["kb_release","out","up"],
         "kb_down":["kb_down","down"]
      };
      
      protected var _newFrame:String;
      
      protected var _newFocusIndicatorFrame:String;
      
      protected var _repeatTimer:Timer;
      
      protected var _mouseDown:int = 0;
      
      protected var _focusIndicatorLabelHash:Object;
      
      protected var _autoRepeatEvent:ButtonEvent;
      
      private var _dropdownRef:MovieClip = null;
      
      public var textField:TextField;
      
      public var defaultTextFormat:TextFormat;
      
      protected var _focusIndicator:MovieClip;
      
      protected var statesDefault:Vector.<String> = Vector.<String>([""]);
      
      protected var statesSelected:Vector.<String> = Vector.<String>(["selected_",""]);
      
      public function DropdownMenu()
      {
         super();
      }
      
      override protected function preInitialize() : void
      {
         if(!this.constraintsDisabled)
         {
            constraints = new Constraints(this,ConstrainMode.COUNTER_SCALE);
         }
      }
      
      override protected function initialize() : void
      {
         this.dataProvider = new DataProvider();
         this.menuOffset = new Padding(0,0,0,0);
         this.menuPadding = new Padding(0,0,0,0);
         tabEnabled = true;
         super.initialize();
      }
      
      public function get autoRepeat() : Boolean
      {
         return false;
      }
      
      public function set autoRepeat(value:Boolean) : void
      {
      }
      
      public function get data() : Object
      {
         return null;
      }
      
      public function set data(value:Object) : void
      {
      }
      
      override public function get enabled() : Boolean
      {
         return super.enabled;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         var state:String = null;
         super.enabled = value;
         mouseChildren = false;
         if(super.enabled)
         {
            state = this._focusIndicator == null && (_displayFocus || _focused) ? "over" : "up";
         }
         else
         {
            state = "disabled";
         }
         this.setState(state);
      }
      
      public function get label() : String
      {
         return "";
      }
      
      public function set label(value:String) : void
      {
      }
      
      public function get state() : String
      {
         return this._state;
      }
      
      public function get selected() : Boolean
      {
         return this._selected;
      }
      
      public function set selected(value:Boolean) : void
      {
         if(this._selected == value)
         {
            return;
         }
         this._selected = value;
         if(this.enabled)
         {
            if(!this.owner)
            {
               if(!_focused)
               {
                  this.setState("toggle");
               }
               else if(this._pressedByKeyboard && this._focusIndicator != null)
               {
                  this.setState("kb_selecting");
               }
               else
               {
                  this.setState("selecting");
               }
            }
            else if(this.owner)
            {
               _displayFocus = this._selected && this.owner != null && this.checkOwnerFocused();
               this.setState(_displayFocus ? "selecting" : "toggle");
            }
         }
         else
         {
            this.setState("disabled");
         }
         validateNow();
         dispatchEventAndSound(new Event(Event.SELECT));
      }
      
      public function get toggle() : Boolean
      {
         return this._toggle;
      }
      
      public function set toggle(value:Boolean) : void
      {
         this._toggle = value;
      }
      
      public function get owner() : UIComponent
      {
         return this._owner;
      }
      
      public function set owner(value:UIComponent) : void
      {
         this._owner = value;
      }
      
      public function get autoSize() : String
      {
         return this._autoSize;
      }
      
      public function set autoSize(value:String) : void
      {
         if(value == this._autoSize)
         {
            return;
         }
         this._autoSize = value;
         invalidateData();
      }
      
      public function get focusIndicator() : MovieClip
      {
         return this._focusIndicator;
      }
      
      public function set focusIndicator(value:MovieClip) : void
      {
         this._focusIndicatorLabelHash = null;
         this._focusIndicator = value;
         this._focusIndicatorLabelHash = UIComponent.generateLabelHash(this._focusIndicator);
      }
      
      public function set inspectableMenuPadding(value:Object) : void
      {
         if(!componentInspectorSetting)
         {
            return;
         }
         this.menuPadding = new Padding(value.top,value.right,value.bottom,value.left);
      }
      
      public function set inspectableMenuOffset(value:Object) : void
      {
         if(!componentInspectorSetting)
         {
            return;
         }
         this.menuOffset = new Padding(value.top,value.right,value.bottom,value.left);
      }
      
      public function set inspectableThumbOffset(value:Object) : void
      {
         if(!componentInspectorSetting)
         {
            return;
         }
         this.thumbOffsetTop = Number(value.top);
         this.thumbOffsetBottom = Number(value.bottom);
      }
      
      override public function get focusable() : Boolean
      {
         return _focusable;
      }
      
      override public function set focusable(value:Boolean) : void
      {
         super.focusable = value;
      }
      
      public function get selectedIndex() : int
      {
         return this._selectedIndex;
      }
      
      public function set selectedIndex(value:int) : void
      {
         var dd:CoreList = null;
         var offset:uint = 0;
         this._selectedIndex = value;
         this.invalidateSelectedIndex();
         if(this.dataArray.length > 0)
         {
            this.curID = this.dataArray[value].id;
            this.curCom = this.dataArray[value].com;
            this.color = this.dataArray[value].color;
         }
         if(this._dropdownRef != null)
         {
            dd = this._dropdownRef as CoreList;
            offset = dd is ScrollingList ? uint((dd as ScrollingList).scrollPosition) : 0;
            dispatchEvent(new ListEvent(ListEvent.INDEX_CHANGE,true,false,this._selectedIndex,-1,-1,dd.getRendererAt(this._selectedIndex,offset),this._dataProvider[this._selectedIndex]));
         }
      }
      
      public function set_curID(id:int) : void
      {
         this.curID = id;
         for(var i:* = 0; i < this.dataArray.length; i++)
         {
            if(this.curID == this.dataArray[i].id)
            {
               this.selectedIndex = i;
               return;
            }
         }
      }
      
      public function get dataArray() : Array
      {
         return this._dataArray;
      }
      
      public function set dataArray(value:Array) : void
      {
         if(this._dataArray == value)
         {
            return;
         }
         var tmp:Array = new Array();
         this._dataArray = value;
         for(var i:* = 0; i < value.length; i++)
         {
            tmp.push(value[i].label);
         }
         this.menuRowCount = this._dataArray.length;
         this.dataProvider = new DataProvider(tmp);
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
      
      public function itemToLabel(item:Object) : String
      {
         if(item == null)
         {
            return "";
         }
         return item.toString();
      }
      
      public function open() : void
      {
         this.selected = true;
         stage.addEventListener(MouseEvent.MOUSE_DOWN,this.handleStageClick,false,0,true);
         this.showDropdown();
      }
      
      public function close() : void
      {
         this.selected = false;
         stage.removeEventListener(MouseEvent.MOUSE_DOWN,this.handleStageClick,false);
         this.hideDropdown();
      }
      
      public function invalidateSelectedIndex() : void
      {
         invalidate(InvalidationType.SELECTED_INDEX);
      }
      
      public function handleInput1(event:InputEvent) : void
      {
         if(event.isDefaultPrevented())
         {
            return;
         }
         var details:InputDetails = event.details;
         var index:* = details.controllerIndex;
         switch(details.navEquivalent)
         {
            case NavigationCode.ENTER:
               if(details.value == InputValue.KEY_DOWN)
               {
                  this.handlePress(index);
                  event.handled = true;
               }
               else if(details.value == InputValue.KEY_UP)
               {
                  if(this._pressedByKeyboard)
                  {
                     this.handleRelease(index);
                     event.handled = true;
                  }
               }
         }
      }
      
      override public function handleInput(event:InputEvent) : void
      {
         if(event.handled)
         {
            return;
         }
         if(this._dropdownRef != null && this.selected)
         {
            this._dropdownRef.handleInput(event);
            if(event.handled)
            {
               return;
            }
         }
         this.handleInput1(event);
         var details:InputDetails = event.details;
         var keyPress:* = details.value == InputValue.KEY_DOWN;
         switch(details.navEquivalent)
         {
            case NavigationCode.ESCAPE:
               if(this.selected)
               {
                  if(keyPress)
                  {
                     this.close();
                  }
                  event.handled = true;
                  break;
               }
         }
      }
      
      override public function toString() : String
      {
         return "[CLIK DropdownMenu " + name + "]";
      }
      
      override protected function configUI() : void
      {
         if(!this.constraintsDisabled)
         {
            constraints.addElement("textField",this.textField,Constraints.ALL);
         }
         super.configUI();
         tabEnabled = _focusable && this.enabled && tabEnabled;
         mouseChildren = tabChildren = false;
         addEventListener(MouseEvent.ROLL_OVER,this.handleMouseRollOver,false,0,true);
         addEventListener(MouseEvent.ROLL_OUT,this.handleMouseRollOut,false,0,true);
         addEventListener(MouseEvent.MOUSE_DOWN,this.handleMousePress,false,0,true);
         addEventListener(MouseEvent.CLICK,this.handleMouseRelease,false,0,true);
         addEventListener(MouseEvent.DOUBLE_CLICK,this.handleMouseRelease,false,0,true);
         addEventListener(InputEvent.INPUT,this.handleInput,false,0,true);
         if(this._focusIndicator != null && !_focused && this._focusIndicator.totalFrames == 1)
         {
            this.focusIndicator.visible = false;
         }
      }
      
      protected function draw1() : void
      {
         if(isInvalid(InvalidationType.STATE))
         {
            if(this._newFrame)
            {
               gotoAndPlay(this._newFrame);
               this._newFrame = null;
            }
            if(this._newFocusIndicatorFrame)
            {
               this.focusIndicator.gotoAndPlay(this._newFocusIndicatorFrame);
               this._newFocusIndicatorFrame = null;
            }
            this.updateAfterStateChange();
            dispatchEvent(new ComponentEvent(ComponentEvent.STATE_CHANGE));
            invalidate(InvalidationType.DATA,InvalidationType.SIZE);
         }
         if(isInvalid(InvalidationType.DATA))
         {
            this.updateText();
            if(this.autoSize != TextFieldAutoSize.NONE)
            {
               invalidateSize();
            }
         }
         if(isInvalid(InvalidationType.SIZE))
         {
            this.alignForAutoSize();
            setActualSize(_width,_height);
            if(!this.constraintsDisabled)
            {
               constraints.update(_width,_height);
            }
         }
      }
      
      override protected function draw() : void
      {
         if(isInvalid(InvalidationType.SELECTED_INDEX) || isInvalid(InvalidationType.DATA))
         {
            this._dataProvider.requestItemAt(this._selectedIndex,this.populateText);
            invalidateData();
         }
         this.draw1();
         this.textField.textColor = this.color;
      }
      
      protected function checkOwnerFocused() : Boolean
      {
         var ownerFocusTarget:Object = null;
         var ownerFocused:* = false;
         if(this.owner != null)
         {
            ownerFocused = this._owner.focused != 0;
            if(ownerFocused == 0)
            {
               ownerFocusTarget = this._owner.focusTarget;
               if(ownerFocusTarget != null)
               {
                  ownerFocused = ownerFocusTarget != 0;
               }
            }
         }
         return ownerFocused;
      }
      
      protected function updateText() : void
      {
         if(this._label != null && this.textField != null)
         {
            this.textField.htmlText = this._label;
         }
      }
      
      protected function alignForAutoSize() : void
      {
         var oldWidth:Number = NaN;
         var oldRight:Number = NaN;
         var oldCenter:Number = NaN;
         if(!initialized || this._autoSize == TextFieldAutoSize.NONE || this.textField == null)
         {
            return;
         }
         oldWidth = _width;
         var newWidth:Number = _width = this.calculateWidth();
         switch(this._autoSize)
         {
            case TextFieldAutoSize.RIGHT:
               oldRight = x + oldWidth;
               x = oldRight - newWidth;
               break;
            case TextFieldAutoSize.CENTER:
               oldCenter = x + oldWidth * 0.5;
               x = oldCenter - newWidth * 0.5;
         }
      }
      
      protected function calculateWidth() : Number
      {
         var element:ConstrainedElement = null;
         var w:Number = actualWidth;
         if(!this.constraintsDisabled)
         {
            element = constraints.getElement("textField");
            w = Math.ceil(this.textField.textWidth + element.left + element.right + 5);
         }
         return w;
      }
      
      override protected function changeFocus() : void
      {
         var focusFrame:String = null;
         if(!this.enabled)
         {
            return;
         }
         if(this._focusIndicator == null)
         {
            this.setState(Boolean(_focused) || _displayFocus ? "over" : "out");
            if(this._pressedByKeyboard && !_focused)
            {
               this._pressedByKeyboard = false;
            }
         }
         else
         {
            if(this._focusIndicator.totalframes == 1)
            {
               this._focusIndicator.visible = _focused > 0;
            }
            else
            {
               focusFrame = "state" + _focused;
               if(this._focusIndicatorLabelHash[focusFrame])
               {
                  this._newFocusIndicatorFrame = "state" + _focused;
               }
               else
               {
                  this._newFocusIndicatorFrame = Boolean(_focused) || _displayFocus ? "show" : "hide";
               }
               invalidateState();
            }
            if(this._pressedByKeyboard && !_focused)
            {
               this.setState("kb_release");
               this._pressedByKeyboard = false;
            }
         }
         if(this._selected && Boolean(this._dropdownRef))
         {
            this.close();
         }
      }
      
      protected function handleMouseRollOver(event:MouseEvent) : void
      {
         var sfEvent:MouseEventEx = event as MouseEventEx;
         var mouseIdx:uint = sfEvent == null ? 0 : sfEvent.mouseIdx;
         if(event.buttonDown)
         {
            dispatchEvent(new ButtonEvent(ButtonEvent.DRAG_OVER));
            if(!this.enabled)
            {
               return;
            }
            if(this.lockDragStateChange && Boolean(this._mouseDown << mouseIdx & 1))
            {
               return;
            }
            if(Boolean(_focused) || _displayFocus)
            {
               this.setState(this.focusIndicator == null ? "down" : "kb_down");
            }
            else
            {
               this.setState("over");
            }
         }
         else
         {
            if(!this.enabled)
            {
               return;
            }
            if(Boolean(_focused) || _displayFocus)
            {
               if(this._focusIndicator != null)
               {
                  this.setState("over");
               }
            }
            else
            {
               this.setState("over");
            }
         }
      }
      
      protected function handleMouseRollOut(event:MouseEvent) : void
      {
         var sfEvent:MouseEventEx = event as MouseEventEx;
         var index:uint = sfEvent == null ? 0 : sfEvent.mouseIdx;
         if(event.buttonDown)
         {
            dispatchEvent(new ButtonEvent(ButtonEvent.DRAG_OUT));
            if(Boolean(this._mouseDown & 1 << index))
            {
               if(stage != null)
               {
                  stage.addEventListener(MouseEvent.MOUSE_UP,this.handleReleaseOutside,false,0,true);
               }
            }
            if(this.lockDragStateChange || !this.enabled)
            {
               return;
            }
            if(Boolean(_focused) || _displayFocus)
            {
               this.setState(this._focusIndicator == null ? "release" : "kb_release");
            }
            else
            {
               this.setState("out");
            }
         }
         else
         {
            if(!this.enabled)
            {
               return;
            }
            if(Boolean(_focused) || _displayFocus)
            {
               if(this._focusIndicator != null)
               {
                  this.setState("out");
               }
            }
            else
            {
               this.setState("out");
            }
         }
      }
      
      protected function handleMousePress(event:MouseEvent) : void
      {
         var sfButtonEvent:ButtonEvent = null;
         var sfEvent:MouseEventEx = event as MouseEventEx;
         var mouseIdx:uint = sfEvent == null ? 0 : sfEvent.mouseIdx;
         var btnIdx:uint = sfEvent == null ? 0 : sfEvent.buttonIdx;
         if(btnIdx != 0)
         {
            return;
         }
         this._mouseDown |= 1 << mouseIdx;
         if(this.enabled)
         {
            this.setState("down");
            if(this.autoRepeat && this._repeatTimer == null)
            {
               this._autoRepeatEvent = new ButtonEvent(ButtonEvent.CLICK,true,false,mouseIdx,btnIdx,false,true);
               this._repeatTimer = new Timer(this.repeatDelay,1);
               this._repeatTimer.addEventListener(TimerEvent.TIMER_COMPLETE,this.beginRepeat,false,0,true);
               this._repeatTimer.start();
            }
            sfButtonEvent = new ButtonEvent(ButtonEvent.PRESS,true,false,mouseIdx,btnIdx,false,false);
            dispatchEvent(sfButtonEvent);
         }
      }
      
      protected function handleMouseRelease(event:MouseEvent) : void
      {
         var sfButtonEvent:ButtonEvent = null;
         this._autoRepeatEvent = null;
         if(!this.enabled)
         {
            return;
         }
         var sfEvent:MouseEventEx = event as MouseEventEx;
         var mouseIdx:uint = sfEvent == null ? 0 : sfEvent.mouseIdx;
         var btnIdx:uint = sfEvent == null ? 0 : sfEvent.buttonIdx;
         if(btnIdx != 0)
         {
            return;
         }
         this._mouseDown ^= 1 << mouseIdx;
         if(this._mouseDown == 0 && Boolean(this._repeatTimer))
         {
            this._repeatTimer.stop();
            this._repeatTimer.reset();
            this._repeatTimer.removeEventListener(TimerEvent.TIMER_COMPLETE,this.beginRepeat);
            this._repeatTimer.removeEventListener(TimerEvent.TIMER,this.handleRepeat);
            this._repeatTimer = null;
         }
         this.setState("release");
         this.handleClick(mouseIdx);
         if(!this._isRepeating)
         {
            sfButtonEvent = new ButtonEvent(ButtonEvent.CLICK,true,false,mouseIdx,btnIdx,false,false);
            dispatchEvent(sfButtonEvent);
         }
         this._isRepeating = false;
      }
      
      protected function handleReleaseOutside(event:MouseEvent) : void
      {
         this._autoRepeatEvent = null;
         if(contains(event.target as DisplayObject))
         {
            return;
         }
         var sfEvent:MouseEventEx = event as MouseEventEx;
         var mouseIdx:uint = sfEvent == null ? 0 : sfEvent.mouseIdx;
         var btnIdx:uint = sfEvent == null ? 0 : sfEvent.buttonIdx;
         if(btnIdx != 0)
         {
            return;
         }
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.handleReleaseOutside,false);
         this._mouseDown ^= 1 << mouseIdx;
         dispatchEvent(new ButtonEvent(ButtonEvent.RELEASE_OUTSIDE));
         if(!this.enabled)
         {
            return;
         }
         if(this.lockDragStateChange)
         {
            if(Boolean(_focused) || _displayFocus)
            {
               this.setState(this.focusIndicator == null ? "release" : "kb_release");
            }
            else
            {
               this.setState("kb_release");
            }
         }
      }
      
      protected function handlePress(controllerIndex:uint = 0) : void
      {
         if(!this.enabled)
         {
            return;
         }
         this._pressedByKeyboard = true;
         this.setState(this._focusIndicator == null ? "down" : "kb_down");
         if(this.autoRepeat && this._repeatTimer == null)
         {
            this._autoRepeatEvent = new ButtonEvent(ButtonEvent.CLICK,true,false,controllerIndex,0,true,true);
            this._repeatTimer = new Timer(this.repeatDelay,1);
            this._repeatTimer.addEventListener(TimerEvent.TIMER_COMPLETE,this.beginRepeat,false,0,true);
            this._repeatTimer.start();
         }
         var sfEvent:ButtonEvent = new ButtonEvent(ButtonEvent.PRESS,true,false,controllerIndex,0,true,false);
         dispatchEvent(sfEvent);
      }
      
      protected function handleRelease(controllerIndex:uint = 0) : void
      {
         var sfEvent:ButtonEvent = null;
         if(!this.enabled)
         {
            return;
         }
         this.setState(this.focusIndicator == null ? "release" : "kb_release");
         if(this._repeatTimer)
         {
            this._repeatTimer.stop();
            this._repeatTimer.reset();
            this._repeatTimer.removeEventListener(TimerEvent.TIMER_COMPLETE,this.beginRepeat);
            this._repeatTimer.removeEventListener(TimerEvent.TIMER,this.handleRepeat);
            this._repeatTimer = null;
         }
         this.handleClick(controllerIndex);
         this._pressedByKeyboard = false;
         if(!this._isRepeating)
         {
            sfEvent = new ButtonEvent(ButtonEvent.CLICK,true,false,controllerIndex,0,true,false);
            dispatchEvent(sfEvent);
         }
         this._isRepeating = false;
      }
      
      protected function handleClick(controllerIndex:uint = 0) : void
      {
         if(!this._selected)
         {
            this.open();
         }
         else
         {
            this.close();
         }
         if(this._toggle && (!this.selected || this.allowDeselect))
         {
            this.selected = !this.selected;
         }
      }
      
      protected function handleDataChange(event:Event) : void
      {
         invalidate(InvalidationType.DATA);
      }
      
      protected function beginRepeat(event:TimerEvent) : void
      {
         this._repeatTimer.delay = this.repeatInterval;
         this._repeatTimer.repeatCount = 0;
         this._repeatTimer.removeEventListener(TimerEvent.TIMER_COMPLETE,this.beginRepeat);
         this._repeatTimer.addEventListener(TimerEvent.TIMER,this.handleRepeat,false,0,true);
         this._repeatTimer.reset();
         this._repeatTimer.start();
      }
      
      protected function handleRepeat(event:TimerEvent) : void
      {
         if(this._mouseDown == 0 && !this._pressedByKeyboard)
         {
            this._repeatTimer.stop();
            this._repeatTimer.reset();
            this._repeatTimer.removeEventListener(TimerEvent.TIMER_COMPLETE,this.beginRepeat);
            this._repeatTimer.removeEventListener(TimerEvent.TIMER,this.handleRepeat);
            this._repeatTimer = null;
         }
         if(this._autoRepeatEvent)
         {
            this._isRepeating = true;
            dispatchEvent(this._autoRepeatEvent);
         }
      }
      
      protected function setState(state:String) : void
      {
         var prefix:String = null;
         var sl:uint = 0;
         var j:uint = 0;
         var thisLabel:String = null;
         this._state = state;
         var prefixes:Vector.<String> = this.getStatePrefixes();
         var states:Array = this._stateMap[state];
         if(states == null || states.length == 0)
         {
            return;
         }
         var l:uint = prefixes.length;
         for(var i:uint = 0; i < l; i++)
         {
            prefix = prefixes[i];
            sl = states.length;
            for(j = 0; j < sl; j++)
            {
               thisLabel = prefix + states[j];
               if(_labelHash[thisLabel])
               {
                  this._newFrame = thisLabel;
                  invalidateState();
                  return;
               }
            }
         }
      }
      
      protected function populateText(item:Object) : void
      {
         this.updateLabel(item);
         dispatchEventAndSound(new Event(Event.CHANGE));
      }
      
      public function changeLabel(s:String, colo:uint = 0) : *
      {
         this._label = "<font size=\"" + this.FontSize + "\" color=\"#" + colo.toString(16) + "\">" + s + "</font>";
         this.color = colo;
         invalidateState();
      }
      
      protected function updateLabel(item:Object) : void
      {
         try
         {
            if(this.selectedIndex > -1)
            {
               this._label = "<font size=\"" + this.FontSize + "\" color=\"#" + this.dataArray[this._selectedIndex].color.toString(16) + "\">" + this.itemToLabel(item) + "</font>";
               this.color = this.dataArray[this._selectedIndex].color;
            }
            else
            {
               this._label = this.itemToLabel(item);
            }
         }
         catch(e:Error)
         {
            _label = "";
         }
      }
      
      protected function handleStageClick(event:MouseEvent) : void
      {
         if(this.contains(event.target as DisplayObject))
         {
            return;
         }
         if(this._dropdownRef.contains(event.target as DisplayObject))
         {
            return;
         }
         this.close();
      }
      
      protected function showDropdown() : void
      {
         var dd:MovieClip = null;
         var classRef:Class = null;
         if(this.dropdown == null)
         {
            return;
         }
         if(this.dropdown is String && this.dropdown != "")
         {
            classRef = getDefinitionByName(this.dropdown.toString()) as Class;
            if(classRef != null)
            {
               dd = new classRef() as CoreList;
            }
         }
         if(dd)
         {
            if(this.itemRenderer is String && this.itemRenderer != "")
            {
               dd.itemRenderer = getDefinitionByName(this.itemRenderer.toString()) as Class;
            }
            else if(this.itemRenderer is Class)
            {
               dd.itemRenderer = this.itemRenderer as Class;
            }
            if(this.scrollBar is String && this.scrollBar != "")
            {
               dd.scrollBar = getDefinitionByName(this.scrollBar.toString()) as Class;
            }
            else if(this.scrollBar is Class)
            {
               dd.scrollBar = this.scrollBar as Class;
            }
            dd.selectedIndex = this._selectedIndex;
            dd.width = this.menuWidth == -1 ? width + this.menuOffset.left + this.menuOffset.right : this.menuWidth;
            dd.dataProvider = this._dataProvider;
            dd.padding = this.menuPadding;
            dd.wrapping = this.menuWrapping;
            dd.margin = this.menuMargin;
            dd.thumbOffset = {
               "top":this.thumbOffsetTop,
               "bottom":this.thumbOffsetBottom
            };
            dd.focusTarget = this;
            dd.rowCount = this.menuRowCount < 1 ? 5 : this.menuRowCount;
            if(this.dataProvider.length == 1)
            {
               dd.height = 24;
            }
            else
            {
               dd.height = 23 * this.dataProvider.length;
            }
            dd.labelField = this._labelField;
            dd.labelFunction = this._labelFunction;
            dd.addEventListener(ListEvent.ITEM_CLICK,this.handleMenuItemClick,false,0,true);
            dd.dataArray = this._dataArray;
            this._dropdownRef = dd;
            PopUpManager.show(dd,x + this.menuOffset.left,this.menuDirection == "down" ? y + height + this.menuOffset.top + 1 : y - this._dropdownRef.height + this.menuOffset.bottom + 1,parent);
            dd.scaleX = 1;
         }
      }
      
      protected function hideDropdown() : void
      {
         if(this._dropdownRef)
         {
            this._dropdownRef.parent.removeChild(this._dropdownRef);
            this._dropdownRef = undefined;
         }
      }
      
      protected function handleMenuItemClick(e:ListEvent) : void
      {
         this.selectedIndex = e.index;
         if(this.dataArray.length > 0)
         {
            this.curID = this.dataArray[e.index].id;
            this.curCom = this.dataArray[e.index].com;
            this.color = this.dataArray[e.index].color;
            dispatchEvent(new ListEvent(ListEvent.INDEX_CHANGE,true,false,e.index));
         }
         this.close();
      }
      
      protected function getStatePrefixes() : Vector.<String>
      {
         return this._selected ? this.statesSelected : this.statesDefault;
      }
      
      protected function updateAfterStateChange() : void
      {
         if(!initialized)
         {
            return;
         }
         if(constraints != null && !this.constraintsDisabled && this.textField != null)
         {
            constraints.updateElement("textField",this.textField);
         }
      }
   }
}

