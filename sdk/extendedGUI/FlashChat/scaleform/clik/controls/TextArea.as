package scaleform.clik.controls
{
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   import flash.text.TextFieldType;
   import flash.text.TextFormat;
   import flash.utils.getDefinitionByName;
   import flash.utils.setTimeout;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InputValue;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.events.ComponentEvent;
   import scaleform.clik.events.InputEvent;
   import scaleform.clik.interfaces.IScrollBar;
   import scaleform.clik.ui.InputDetails;
   import scaleform.clik.utils.ConstrainedElement;
   import scaleform.clik.utils.Constraints;
   import scaleform.gfx.Extensions;
   
   public class TextArea extends UIComponent
   {
      public var defaultText:String = "";
      
      public var defaultTextFormat:TextFormat;
      
      protected var _text:String = "";
      
      protected var _displayAsPassword:Boolean = false;
      
      protected var _maxChars:uint = 0;
      
      protected var _editable:Boolean = true;
      
      protected var _actAsButton:Boolean = false;
      
      protected var _alwaysShowSelection:Boolean = false;
      
      protected var isHtml:Boolean = false;
      
      protected var state:String = "default";
      
      protected var _newFrame:String;
      
      protected var _fontSize:Number = 14;
      
      private var hscroll:Number = 0;
      
      public var textField:TextField;
      
      public var Scrolling:Boolean = false;
      
      protected var _scrollPolicy:String = "auto";
      
      protected var _position:int = 1;
      
      protected var _maxScroll:Number = 1;
      
      protected var _resetScrollPosition:Boolean = false;
      
      protected var _scrollBarValue:Object;
      
      protected var _autoScrollBar:Boolean = false;
      
      protected var _thumbOffset:Object = {
         "top":0,
         "bottom":0
      };
      
      protected var _minThumbSize:uint = 1;
      
      protected var _scrollBar:IScrollBar;
      
      public var container:Sprite;
      
      public function TextArea()
      {
         super();
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.COUNTER_SCALE);
      }
      
      override protected function initialize() : void
      {
         tabEnabled = false;
         mouseEnabled = mouseChildren = this.enabled;
         super.initialize();
         if(this.container == null)
         {
            this.container = new Sprite();
            addChild(this.container);
         }
      }
      
      public function set textID(value:String) : void
      {
         this.text = value;
      }
      
      public function scrollMax() : *
      {
         var Fun:* = function():*
         {
            if(_scrollBar != null)
            {
               _scrollBar.position = textField.maxScrollV;
            }
         };
         setTimeout(Fun,0);
      }
      
      public function set FontSize(value:Number) : *
      {
         this._fontSize = value;
         var newFormat:TextFormat = new TextFormat();
         newFormat.size = this._fontSize;
         this.defaultTextFormat = newFormat;
         this.textField.setTextFormat(newFormat);
         this.textField.defaultTextFormat = newFormat;
      }
      
      public function get FontSize() : Number
      {
         return this._fontSize;
      }
      
      public function get maxScroll() : Number
      {
         return this._maxScroll;
      }
      
      public function get text() : String
      {
         return this._text;
      }
      
      public function set text(value:String) : void
      {
         this.isHtml = false;
         this._text = value;
         invalidateData();
      }
      
      public function get htmlText() : String
      {
         return this._text;
      }
      
      public function set htmlText(value:String) : void
      {
         this.isHtml = true;
         this._text = value;
         invalidateData();
      }
      
      public function get displayAsPassword() : Boolean
      {
         return this._displayAsPassword;
      }
      
      public function set displayAsPassword(value:Boolean) : void
      {
         this._displayAsPassword = value;
         if(this.textField != null)
         {
            this.textField.displayAsPassword = value;
         }
      }
      
      public function get maxChars() : uint
      {
         return this._maxChars;
      }
      
      public function set maxChars(value:uint) : void
      {
         this._maxChars = value;
         if(this.textField != null)
         {
            this.textField.maxChars = value;
         }
      }
      
      public function get editable() : Boolean
      {
         return this._editable;
      }
      
      public function set editable(value:Boolean) : void
      {
         this._editable = value;
         if(this.textField != null)
         {
            this.textField.type = this._editable && this.enabled ? TextFieldType.INPUT : TextFieldType.DYNAMIC;
         }
      }
      
      public function get actAsButton() : Boolean
      {
         return this._actAsButton;
      }
      
      public function set actAsButton(value:Boolean) : void
      {
         if(this._actAsButton == value)
         {
            return;
         }
         this._actAsButton = value;
         if(value)
         {
            addEventListener(MouseEvent.ROLL_OVER,this.handleRollOver,false,0,true);
            addEventListener(MouseEvent.ROLL_OUT,this.handleRollOut,false,0,true);
         }
         else
         {
            removeEventListener(MouseEvent.ROLL_OVER,this.handleRollOver);
            removeEventListener(MouseEvent.ROLL_OUT,this.handleRollOut);
         }
      }
      
      public function get alwaysShowSelection() : Boolean
      {
         return this._alwaysShowSelection;
      }
      
      public function set alwaysShowSelection(value:Boolean) : void
      {
         this._alwaysShowSelection = value;
         if(this.textField != null)
         {
            this.textField.alwaysShowSelection = value;
         }
      }
      
      override public function get enabled() : Boolean
      {
         return super.enabled;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         this.textField.selectable = true;
         super.enabled = value;
         mouseEnabled = mouseChildren = value;
         this.setState(this.defaultState);
         this.updateScrollBar();
      }
      
      public function get length() : uint
      {
         return this.textField.length;
      }
      
      public function get defaultState() : String
      {
         return !this.enabled ? "disabled" : (!!focused ? "focused" : "default");
      }
      
      public function appendText(value:String) : void
      {
         this.text += value;
         this.isHtml = false;
         invalidateData();
      }
      
      public function Scroll(e:Event) : *
      {
         var F:* = function():*
         {
            _scrollBar.position = textField.numLines;
         };
         setTimeout(F,0);
      }
      
      public function appendHtml(value:String) : void
      {
         var F:* = undefined;
         this.text += value;
         this.isHtml = true;
         invalidateData();
         if(this.Scrolling)
         {
            F = function():*
            {
               _scrollBar.position = textField.numLines;
               dispatchEvent(new Event("ADD_HTML"));
            };
            setTimeout(F,0);
         }
      }
      
      public function get position() : int
      {
         return this._position;
      }
      
      public function set position(value:int) : void
      {
         this._position = value;
         this.textField.scrollV = this._position;
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
      
      public function get minThumbSize() : uint
      {
         return this._minThumbSize;
      }
      
      public function set minThumbSize(value:uint) : void
      {
         this._minThumbSize = value;
         if(!this._autoScrollBar)
         {
            return;
         }
         var sb:ScrollIndicator = this._scrollBar as ScrollIndicator;
         sb.minThumbSize = value;
      }
      
      public function get thumbOffset() : Object
      {
         return this._thumbOffset;
      }
      
      public function set thumbOffset(value:Object) : void
      {
         this._thumbOffset = value;
         if(!this._autoScrollBar)
         {
            return;
         }
         var sb:ScrollIndicator = this._scrollBar as ScrollIndicator;
         sb.offsetTop = this._thumbOffset.top;
         sb.offsetBottom = this._thumbOffset.bottom;
      }
      
      public function get availableWidth() : Number
      {
         return Math.round(_width) - (this._autoScrollBar && (this._scrollBar as MovieClip).visible ? Math.round(this._scrollBar.width) : 0);
      }
      
      public function get availableHeight() : Number
      {
         return Math.round(_height);
      }
      
      override public function toString() : String
      {
         return "[CLIK TextArea " + name + "]";
      }
      
      override public function handleInput(event:InputEvent) : void
      {
         var _loc3_:String = null;
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         if(event.handled)
         {
            return;
         }
         var details:InputDetails = event.details;
         if(details.value == InputValue.KEY_DOWN || details.value == InputValue.KEY_HOLD)
         {
            return;
         }
      }
      
      public function StartSelectionText(e:MouseEvent) : *
      {
         addEventListener(MouseEvent.MOUSE_UP,this.StopSelectionText);
      }
      
      public function StopSelectionText(e:MouseEvent) : *
      {
         removeEventListener(MouseEvent.MOUSE_UP,this.StopSelectionText);
         if(this.textField.selectionEndIndex - this.textField.selectionBeginIndex > 0)
         {
            dispatchEvent(new Event("SELECTED_TEXT"));
         }
      }
      
      override protected function configUI() : void
      {
         constraints.addElement("textField",this.textField,Constraints.ALL);
         addEventListener(MouseEvent.MOUSE_DOWN,this.StartSelectionText);
         addEventListener(InputEvent.INPUT,this.handleInput,false,0,true);
         addEventListener("ADD_HTML",this.Scroll);
         this.setState(this.defaultState,"default");
         if(this.textField != null)
         {
            this.textField.addEventListener(Event.SCROLL,this.onScroller,false,0,true);
         }
      }
      
      protected function setState(... states) : void
      {
         var onlyState:String = null;
         var thisState:String = null;
         if(states.length == 1)
         {
            onlyState = states[0].toString();
            if(this.state != onlyState && Boolean(_labelHash[onlyState]))
            {
               this.state = this._newFrame = onlyState;
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
               this.state = this._newFrame = thisState;
               invalidateState();
               break;
            }
         }
      }
      
      override protected function draw() : void
      {
         var forceMaxScrollUpdate:uint = 0;
         if(isInvalid(InvalidationType.SCROLL_BAR))
         {
            this.createScrollBar();
         }
         if(isInvalid(InvalidationType.STATE))
         {
            if(this._newFrame)
            {
               gotoAndPlay(this._newFrame);
               this._newFrame = null;
            }
            this.updateAfterStateChange();
            this.updateTextField();
            dispatchEvent(new ComponentEvent(ComponentEvent.STATE_CHANGE));
            invalidate(InvalidationType.SIZE);
         }
         else if(isInvalid(InvalidationType.DATA))
         {
            this.updateText();
         }
         if(isInvalid(InvalidationType.SIZE))
         {
            removeChild(this.container);
            setActualSize(_width,_height);
            this.container.scaleX = 1 / scaleX;
            this.container.scaleY = 1 / scaleY;
            constraints.update(this.availableWidth,_height);
            if(!Extensions.enabled)
            {
               forceMaxScrollUpdate = this.textField.textWidth;
            }
            addChild(this.container);
            if(this._autoScrollBar)
            {
               this.drawScrollBar();
            }
         }
      }
      
      protected function updateAfterStateChange() : void
      {
         if(!initialized)
         {
            return;
         }
         constraints.updateElement("textField",this.textField);
         if(_focused)
         {
            stage.focus = this.textField;
         }
      }
      
      protected function handleRollOver(event:MouseEvent) : void
      {
         if(Boolean(focused) || !this.enabled)
         {
            return;
         }
         this.setState("over");
      }
      
      protected function handleRollOut(event:MouseEvent) : void
      {
         if(Boolean(focused) || !this.enabled)
         {
            return;
         }
         this.setState("out","default");
      }
      
      override protected function changeFocus() : void
      {
         this.setState(this.defaultState);
      }
      
      protected function createScrollBar() : void
      {
         var sb:IScrollBar = null;
         var classRef:Class = null;
         var sbInst:Object = null;
         if(this._scrollBar != null)
         {
            this._scrollBar.removeEventListener(Event.SCROLL,this.handleScroll,false);
            this._scrollBar.removeEventListener(Event.CHANGE,this.handleScroll,false);
            this._scrollBar.focusTarget = null;
            if(this.container.contains(this._scrollBar as DisplayObject))
            {
               this.container.removeChild(this._scrollBar as DisplayObject);
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
                  if(Boolean(sbInst) && Boolean(this._thumbOffset))
                  {
                     sbInst.offsetTop = this._thumbOffset.top;
                     sbInst.offsetBottom = this._thumbOffset.bottom;
                  }
                  sb.addEventListener(MouseEvent.MOUSE_WHEEL,this.blockMouseWheel,false,0,true);
                  (sb as Object).minThumbSize = this._minThumbSize;
                  this.container.addChild(sb as DisplayObject);
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
               (sb as Object).offsetTop = this._thumbOffset.top;
               (sb as Object).offsetBottom = this._thumbOffset.bottom;
               (sb as Object).minThumbSize = this._minThumbSize;
               this.container.addChild(sb as DisplayObject);
            }
         }
         else
         {
            sb = this._scrollBarValue as IScrollBar;
         }
         this._scrollBar = sb;
         invalidateSize();
         if(this._scrollBar != null)
         {
            this._scrollBar.addEventListener(Event.SCROLL,this.handleScroll,false,0,true);
            this._scrollBar.addEventListener(Event.CHANGE,this.handleScroll,false,0,true);
            this._scrollBar.focusTarget = this;
            (this._scrollBar as Object).scrollTarget = this.textField;
            this._scrollBar.tabEnabled = false;
         }
      }
      
      protected function drawScrollBar() : void
      {
         if(!this._autoScrollBar)
         {
            return;
         }
         this._scrollBar.x = _width - this._scrollBar.width;
         this._scrollBar.height = this.availableHeight;
         this._scrollBar.validateNow();
      }
      
      protected function updateScrollBar() : void
      {
         this._maxScroll = this.textField.maxScrollV;
         var sb:ScrollBar = this._scrollBar as ScrollBar;
         if(sb == null)
         {
            return;
         }
         var element:ConstrainedElement = constraints.getElement("textField");
         if(this._scrollPolicy == "on" || this._scrollPolicy == "auto" && this.textField.maxScrollV > 1)
         {
            if(this._autoScrollBar && !sb.visible)
            {
               if(element != null)
               {
                  constraints.update(_width,_height);
                  invalidate();
               }
               this._maxScroll = this.textField.maxScrollV;
            }
            sb.visible = true;
         }
         if(this._scrollPolicy == "off" || this._scrollPolicy == "auto" && this.textField.maxScrollV == 1)
         {
            if(this._autoScrollBar && sb.visible)
            {
               sb.visible = false;
               if(element != null)
               {
                  constraints.update(this.availableWidth,_height);
                  invalidate();
               }
            }
         }
         if(sb.enabled != this.enabled)
         {
            sb.enabled = this.enabled;
         }
      }
      
      protected function updateText() : void
      {
         var newFormat:TextFormat = null;
         if(this._text != "")
         {
            if(this.isHtml)
            {
               this.textField.htmlText = this._text;
               newFormat = new TextFormat();
               newFormat.size = this._fontSize;
               this.defaultTextFormat = newFormat;
               this.textField.setTextFormat(newFormat);
               this.textField.defaultTextFormat = newFormat;
            }
            else
            {
               this.textField.text = this._text;
            }
         }
         else
         {
            this.textField.text = "";
            if(!_focused && !this._displayAsPassword && this.defaultText != "")
            {
               this.textField.text = this.defaultText;
               if(this.defaultTextFormat != null)
               {
                  this.textField.setTextFormat(this.defaultTextFormat);
               }
            }
         }
         this.updateScrollBar();
      }
      
      protected function updateTextField() : void
      {
         this._resetScrollPosition = true;
         if(this.textField == null)
         {
            return;
         }
         this.updateText();
         this.textField.maxChars = this._maxChars;
         this.textField.displayAsPassword = this._displayAsPassword;
         this.textField.alwaysShowSelection = this._alwaysShowSelection;
         this.textField.selectable = this.enabled ? this._editable : this.enabled;
         this.textField.type = this._editable && this.enabled ? TextFieldType.INPUT : TextFieldType.DYNAMIC;
         this.textField.tabEnabled = this._editable && this.enabled;
         this.textField.selectable = true;
         this.textField.addEventListener(KeyboardEvent.KEY_UP,this.handleTextChange,false,0,true);
      }
      
      protected function handleScroll(event:Event) : void
      {
         this.position = this._scrollBar.position;
      }
      
      protected function blockMouseWheel(event:MouseEvent) : void
      {
         event.stopPropagation();
      }
      
      protected function handleTextChange(event:Event) : void
      {
         if(this._maxScroll != this.textField.maxScrollV)
         {
            this.updateScrollBar();
         }
         this._text = this.isHtml ? this.textField.htmlText : this.textField.text;
         dispatchEventAndSound(new Event(Event.CHANGE));
      }
      
      protected function onScroller(event:Event) : void
      {
         if(this._resetScrollPosition)
         {
            this.textField.scrollV = this._position;
         }
         else
         {
            this._position = this.textField.scrollV;
         }
         this._resetScrollPosition = false;
      }
   }
}

