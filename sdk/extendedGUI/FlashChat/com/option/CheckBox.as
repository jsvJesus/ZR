package com.option
{
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.events.TimerEvent;
   import flash.geom.ColorTransform;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.utils.Timer;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InputValue;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.constants.NavigationCode;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.data.ListDataChan;
   import scaleform.clik.events.ButtonEvent;
   import scaleform.clik.events.ComponentEvent;
   import scaleform.clik.events.InputEvent;
   import scaleform.clik.ui.InputDetails;
   import scaleform.clik.utils.ConstrainedElement;
   import scaleform.clik.utils.Constraints;
   import scaleform.gfx.MouseEventEx;
   
   public class CheckBox extends UIComponent
   {
      public var lockDragStateChange:Boolean = false;
      
      public var repeatDelay:Number = 500;
      
      public var repeatInterval:Number = 200;
      
      public var constraintsDisabled:Boolean = false;
      
      public var allowDeselect:Boolean = true;
      
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
      
      protected var _ID:Number;
      
      protected var _owner:UIComponent = null;
      
      protected var _index:uint;
      
      protected var _colorR:uint = 0;
      
      protected var _colorG:uint = 0;
      
      protected var _colorB:uint = 0;
      
      protected var _alph:uint = 0;
      
      public var R:*;
      
      public var G:*;
      
      public var B:uint = 0;
      
      public var com:String;
      
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
      
      public var textField:TextField;
      
      public var defaultTextFormat:TextFormat;
      
      public var color1:colorView;
      
      protected var _focusIndicator:MovieClip;
      
      protected var statesDefault:Vector.<String> = Vector.<String>([""]);
      
      protected var statesSelected:Vector.<String> = Vector.<String>(["selected_",""]);
      
      public function CheckBox()
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
         super.initialize();
         this._toggle = true;
      }
      
      public function get data() : Object
      {
         return this._data;
      }
      
      public function set data(value:Object) : void
      {
         this._data = value;
      }
      
      public function get ID() : Number
      {
         return this._ID;
      }
      
      public function get autoRepeat() : Boolean
      {
         return false;
      }
      
      public function set autoRepeat(value:Boolean) : void
      {
      }
      
      public function get toggle() : Boolean
      {
         return true;
      }
      
      public function set toggle(value:Boolean) : void
      {
      }
      
      public function set index(value:uint) : void
      {
         this._index = value;
      }
      
      public function get index() : uint
      {
         return this._index;
      }
      
      public function setListData(listData:ListDataChan) : void
      {
         this.index = listData.index;
         this.selected = listData.selected;
         this.label = listData.label || "";
         this._colorR = listData.color;
         this.enabled = listData.enabled;
         this._ID = listData.id;
         this.com = listData.com;
         this.colorRGB(this._colorR);
      }
      
      public function setData(data:Object) : void
      {
         this.data = data;
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
      
      override public function get focusable() : Boolean
      {
         return _focusable;
      }
      
      override public function set focusable(value:Boolean) : void
      {
         super.focusable = value;
      }
      
      public function get owner() : UIComponent
      {
         return this._owner;
      }
      
      public function set owner(value:UIComponent) : void
      {
         this._owner = value;
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
      
      public function get group() : ButtonGroup
      {
         return this._group;
      }
      
      public function set group(value:ButtonGroup) : void
      {
         if(this._group != null)
         {
            this._group.removeButton(this);
         }
         this._group = value;
         if(this._group != null)
         {
            this._group.addButton(this);
         }
      }
      
      public function colorRGB(R:Number = 0, G:Number = 0, B:Number = 0, A:Number = 1) : *
      {
         this._colorR = R;
         this._colorG = G;
         this._colorB = B;
         this._alph = A;
         this.setcolor();
      }
      
      protected function setcolor() : *
      {
         this.color1.transform.colorTransform = new ColorTransform(0,0,0,this._alph,this._colorR,this._colorG,this._colorB,0);
      }
      
      public function get groupName() : String
      {
         return this._groupName;
      }
      
      public function set groupName(value:String) : void
      {
         if(_inspector && value == "")
         {
            return;
         }
         if(this._groupName == value)
         {
            return;
         }
         if(value != null)
         {
            addEventListener(Event.ADDED,this.addToAutoGroup,false,0,true);
            addEventListener(Event.REMOVED,this.addToAutoGroup,false,0,true);
         }
         else
         {
            removeEventListener(Event.ADDED,this.addToAutoGroup,false);
            removeEventListener(Event.REMOVED,this.addToAutoGroup,false);
         }
         this._groupName = value;
         this.addToAutoGroup(null);
      }
      
      public function get label() : String
      {
         return this._label;
      }
      
      public function set label(value:String) : void
      {
         if(this._label == value)
         {
            return;
         }
         this._label = value;
         invalidateData();
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
      
      override public function handleInput(event:InputEvent) : void
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
      
      override public function toString() : String
      {
         return "[CLIK CheckBox " + name + "]";
      }
      
      override protected function configUI() : void
      {
         if(!this.constraintsDisabled)
         {
            constraints.addElement("textField",this.textField,Constraints.ALL);
            constraints.addElement("color1",this.color1,Constraints.LEFT);
         }
         super.configUI();
         tabEnabled = _focusable && this.enabled && tabEnabled;
         mouseChildren = tabChildren = false;
         addEventListener(MouseEvent.CLICK,this.handleMouseRelease,false,0,true);
         addEventListener(MouseEvent.DOUBLE_CLICK,this.handleMouseRelease,false,0,true);
         if(this._focusIndicator != null && !_focused && this._focusIndicator.totalFrames == 1)
         {
            this.focusIndicator.visible = false;
         }
      }
      
      override protected function draw() : void
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
            setSize(_width,_height);
            if(!this.constraintsDisabled)
            {
               constraints.update(_width,_height);
            }
         }
         this.setcolor();
         scaleX = 1;
      }
      
      protected function addToAutoGroup(event:Event) : void
      {
         if(parent == null)
         {
            this.group = null;
            return;
         }
         var g:ButtonGroup = ButtonGroup.getGroup(this._groupName,parent);
         if(g == this.group)
         {
            return;
         }
         this.group = g;
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
      
      protected function updateText() : void
      {
         if(this._label != null && this.textField != null)
         {
            this.textField.text = this._label;
         }
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
         if(this._toggle && (!this.selected || this.allowDeselect))
         {
            this.selected = !this.selected;
         }
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

