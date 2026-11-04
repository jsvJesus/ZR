package scaleform.clik.controls
{
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.geom.Point;
   import flash.text.TextField;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InputValue;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.constants.NavigationCode;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.events.InputEvent;
   import scaleform.clik.interfaces.IScrollBar;
   import scaleform.clik.ui.InputDetails;
   import scaleform.clik.utils.Constraints;
   
   public class ScrollBar extends UIComponent implements IScrollBar
   {
      public var trackScrollPageSize:Number = 1;
      
      public var direction:String = "vertical";
      
      public var offsetTop:Number = 0;
      
      public var offsetBottom:Number = 0;
      
      protected var _isDragging:Boolean = false;
      
      protected var _maxPosition:Number = 10;
      
      protected var _minPosition:Number = 0;
      
      protected var _minThumbSize:Number = 10;
      
      protected var _pageScrollSize:Number = 1;
      
      protected var _pageSize:Number;
      
      protected var _position:Number = 5;
      
      protected var _scrollTarget:Object;
      
      public var thumb:MovieClip;
      
      public var track:MovieClip;
      
      protected var _dragOffset:Point;
      
      protected var _trackMode:String = "scrollPage";
      
      protected var _trackScrollPosition:Number = -1;
      
      protected var _trackDragMouseIndex:Number = -1;
      
      public var upArrow:Button;
      
      public var downArrow:Button;
      
      public function ScrollBar()
      {
         super();
      }
      
      override protected function initialize() : void
      {
         super.initialize();
         var r:Number = rotation;
         rotation = 0;
         if(this.downArrow)
         {
            constraints.addElement("downArrow",this.downArrow,Constraints.BOTTOM);
         }
         constraints.addElement("track",this.track,Constraints.TOP | Constraints.BOTTOM);
         rotation = r;
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.REFLOW);
      }
      
      override public function get enabled() : Boolean
      {
         return super.enabled;
      }
      
      override public function set enabled(value:Boolean) : void
      {
         if(this.enabled == value)
         {
            return;
         }
         super.enabled = value;
         gotoAndPlay(this.enabled ? "default" : "disabled");
         invalidate(InvalidationType.STATE);
      }
      
      public function set position1(value:Number) : void
      {
         value = Math.max(this._minPosition,Math.min(this._maxPosition,value));
         if(value == this._position)
         {
            return;
         }
         this._position = value;
         dispatchEvent(new Event(Event.SCROLL));
         invalidateData();
      }
      
      public function get position() : Number
      {
         return this._position;
      }
      
      public function set position(value:Number) : void
      {
         value = Math.round(value);
         if(value == this.position)
         {
            return;
         }
         this.position1 = value;
         this.updateScrollTarget();
      }
      
      public function get minThumbSize() : Number
      {
         return this._minThumbSize;
      }
      
      public function set minThumbSize(value:Number) : void
      {
         value = Math.max(1,value);
         this._minThumbSize = value;
         invalidateSize();
      }
      
      public function get isHorizontal() : Boolean
      {
         return this.direction == ScrollBarDirection.HORIZONTAL;
      }
      
      public function get scrollTarget() : Object
      {
         return this._scrollTarget;
      }
      
      public function set scrollTarget(value:Object) : void
      {
         if(value is String)
         {
            if(!componentInspectorSetting || value.toString() == "" || parent == null)
            {
               return;
            }
            value = parent.getChildByName(value.toString());
            if(value == null)
            {
               return;
            }
         }
         var oldTarget:Object = this._scrollTarget;
         this._scrollTarget = value;
         if(oldTarget != null)
         {
            oldTarget.removeEventListener(Event.SCROLL,this.handleTargetScroll,false);
         }
         if(value is UIComponent && "scrollBar" in value)
         {
            value.scrollBar = this;
            return;
         }
         if(this._scrollTarget == null)
         {
            tabEnabled = true;
            return;
         }
         this._scrollTarget.addEventListener(Event.SCROLL,this.handleTargetScroll,false,0,true);
         if(this._scrollTarget is UIComponent)
         {
            focusTarget = this._scrollTarget as UIComponent;
         }
         tabEnabled = false;
         this.handleTargetScroll(null);
         invalidate();
      }
      
      protected function handleTargetScroll(event:Event) : void
      {
         if(this._isDragging)
         {
            return;
         }
         var target:TextField = this._scrollTarget as TextField;
         if(target != null)
         {
            this.setScrollProperties(target.bottomScrollV - target.scrollV,1,target.maxScrollV);
            this.position = target.scrollV;
         }
      }
      
      public function get trackMode() : String
      {
         return this._trackMode;
      }
      
      public function set trackMode(value:String) : void
      {
         if(value == this._trackMode)
         {
            return;
         }
         this._trackMode = value;
         if(initialized)
         {
            this.track.autoRepeat = this.trackMode == ScrollBarTrackMode.SCROLL_PAGE;
         }
      }
      
      public function setScrollProperties(pageSize:Number, minPosition:Number, maxPosition:Number, pageScrollSize:Number = NaN) : void
      {
         this._pageSize = pageSize;
         if(!isNaN(pageScrollSize))
         {
            this._pageScrollSize = pageScrollSize;
         }
         this._minPosition = minPosition;
         this._maxPosition = maxPosition;
         invalidateSize();
      }
      
      public function get availableHeight() : Number
      {
         return this.track.height - this.thumb.height + this.offsetBottom + this.offsetTop;
      }
      
      override public function toString() : String
      {
         return "[CLIK ScrollBar " + name + "]";
      }
      
      override public function handleInput(event:InputEvent) : void
      {
         if(event.handled)
         {
            return;
         }
         var details:InputDetails = event.details;
         if(details.value == InputValue.KEY_UP)
         {
            return;
         }
         var isHorizontal:* = this.direction == ScrollBarDirection.HORIZONTAL;
         switch(details.navEquivalent)
         {
            case NavigationCode.UP:
               if(isHorizontal)
               {
                  return;
               }
               --this.position;
               break;
            case NavigationCode.DOWN:
               if(isHorizontal)
               {
                  return;
               }
               this.position += 1;
               break;
            case NavigationCode.LEFT:
               if(!isHorizontal)
               {
                  return;
               }
               --this.position;
               break;
            case NavigationCode.RIGHT:
               if(!isHorizontal)
               {
                  return;
               }
               this.position += 1;
               break;
            case NavigationCode.HOME:
               this.position = 0;
               break;
            case NavigationCode.END:
               this.position = this._maxPosition;
               break;
            default:
               return;
         }
         event.handled = true;
      }
      
      override protected function configUI() : void
      {
         super.configUI();
         mouseEnabled = mouseChildren = this.enabled;
         tabEnabled = tabChildren = _focusable;
         addEventListener(MouseEvent.MOUSE_WHEEL,this.handleMouseWheel,false,0,true);
         addEventListener(InputEvent.INPUT,this.handleInput,false,0,true);
         if(this.upArrow)
         {
            this.upArrow.addEventListener(MouseEvent.CLICK,this.handleUpArrowClick,false,0,true);
            this.upArrow.focusTarget = this;
            this.upArrow.autoRepeat = true;
         }
         if(this.downArrow)
         {
            this.downArrow.addEventListener(MouseEvent.CLICK,this.handleDownArrowClick,false,0,true);
            this.downArrow.focusTarget = this;
            this.downArrow.autoRepeat = true;
         }
         this.thumb.addEventListener(MouseEvent.MOUSE_DOWN,this.handleThumbPress,false,0,true);
         this.thumb.focusTarget = this;
         this.thumb.lockDragStateChange = true;
         this.track.addEventListener(MouseEvent.CLICK,this.handleTrackClick,false,0,true);
         if(this.track is UIComponent)
         {
            (this.track as UIComponent).focusTarget = this;
         }
         this.track.autoRepeat = this.trackMode == ScrollBarTrackMode.SCROLL_PAGE;
      }
      
      protected function scrollUp() : void
      {
         this.position -= this._pageScrollSize;
      }
      
      protected function scrollDown() : void
      {
         this.position += this._pageScrollSize;
      }
      
      override protected function draw() : void
      {
         var target:TextField = null;
         if(isInvalid(InvalidationType.SIZE))
         {
            setActualSize(_width,_height);
            this.drawLayout();
            this.updateThumb();
         }
         else if(isInvalid(InvalidationType.DATA))
         {
            if(this._scrollTarget is TextField)
            {
               target = this._scrollTarget as TextField;
               this.setScrollProperties(target.bottomScrollV - target.scrollV,1,target.maxScrollV);
            }
            this.updateThumbPosition();
         }
      }
      
      protected function drawLayout() : void
      {
         var yScaleFix:Number = NaN;
         this.thumb.y = this.track.y - this.offsetTop;
         if(this.isHorizontal)
         {
            constraints.update(_height,_width);
         }
         else
         {
            constraints.update(_width,_height);
         }
         if(this.isHorizontal && actualWidth != width)
         {
            yScaleFix = width / actualWidth;
            scaleY = yScaleFix;
         }
      }
      
      protected function updateThumb() : void
      {
         var per:Number = Math.max(1,this._maxPosition - this._minPosition + this._pageSize);
         var trackHeight:Number = this.track.height + this.offsetTop + this.offsetBottom;
         this.thumb.height = Math.max(this._minThumbSize,Math.min(trackHeight,this._pageSize / per * trackHeight));
         if(this.thumb is UIComponent)
         {
            (this.thumb as UIComponent).validateNow();
         }
         this.updateThumbPosition();
      }
      
      protected function updateThumbPosition() : void
      {
         var percent:Number = (this._position - this._minPosition) / (this._maxPosition - this._minPosition);
         var top:Number = this.track.y - this.offsetTop;
         var yPos:Number = Math.round(percent * this.availableHeight + top);
         this.thumb.y = Math.max(top,Math.min(this.track.y + this.track.height - this.thumb.height + this.offsetBottom,yPos));
         this.thumb.visible = !(isNaN(percent) || isNaN(this._pageSize) || this._maxPosition <= 0 || this._maxPosition == Infinity);
         var showThumb:Boolean = this.thumb.visible && this.enabled;
         if(this.upArrow)
         {
            this.upArrow.enabled = showThumb && this._position > this._minPosition;
            this.upArrow.validateNow();
         }
         if(this.downArrow)
         {
            this.downArrow.enabled = showThumb && this._position < this._maxPosition;
            this.downArrow.validateNow();
         }
         this.track.enabled = this.track.mouseEnabled = showThumb;
      }
      
      protected function handleUpArrowClick(event:MouseEvent) : void
      {
         this.scrollUp();
      }
      
      protected function handleUpArrowPress(event:MouseEvent) : void
      {
         this.scrollUp();
      }
      
      protected function handleDownArrowClick(event:MouseEvent) : void
      {
         this.scrollDown();
      }
      
      protected function handleDownArrowPress(event:MouseEvent) : void
      {
         this.scrollDown();
      }
      
      protected function handleThumbPress(event:Event) : void
      {
         if(this._isDragging)
         {
            return;
         }
         this._isDragging = true;
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.doDrag,false,0,true);
         stage.addEventListener(MouseEvent.MOUSE_UP,this.endDrag,false,0,true);
         this._dragOffset = new Point(0,mouseY - this.thumb.y);
      }
      
      protected function doDrag(event:MouseEvent) : void
      {
         var percent:Number = (mouseY - this._dragOffset.y - this.track.y) / this.availableHeight;
         this.position = this._minPosition + percent * (this._maxPosition - this._minPosition);
      }
      
      protected function endDrag(event:MouseEvent) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.doDrag);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.endDrag);
         this._isDragging = false;
      }
      
      protected function handleTrackPress(event:MouseEvent) : void
      {
         var percent:Number = NaN;
         if(event.shiftKey || this.trackMode == ScrollBarTrackMode.SCROLL_TO_CURSOR)
         {
            percent = (mouseY - this.thumb.height / 2 - this.track.y) / this.availableHeight;
            this.position = Math.round(percent * (this._maxPosition - this._minPosition) + this._minPosition);
            this.thumb.dispatchEvent(new MouseEvent(MouseEvent.MOUSE_OVER));
            this.thumb.dispatchEvent(new MouseEvent(MouseEvent.MOUSE_DOWN));
            this.handleThumbPress(event);
            this._dragOffset = new Point(0,this.thumb.height / 2);
         }
         if(this._isDragging || this.position == this._trackScrollPosition)
         {
            return;
         }
         if(mouseY > this.thumb.y && mouseY < this.thumb.y + this.thumb.height)
         {
            return;
         }
         this.position += this.thumb.y < mouseY ? this.trackScrollPageSize : -this.trackScrollPageSize;
      }
      
      protected function handleTrackClick(event:MouseEvent) : void
      {
         if(this._isDragging || this.position == this._trackScrollPosition)
         {
            return;
         }
         if(mouseY > this.thumb.y && mouseY < this.thumb.y + this.thumb.height)
         {
            return;
         }
         this.position += this.thumb.y < mouseY ? this.trackScrollPageSize : -this.trackScrollPageSize;
      }
      
      protected function updateScrollTarget() : void
      {
         if(this._scrollTarget == null || !this.enabled)
         {
            return;
         }
         this._scrollTarget.scrollV = this._position;
      }
      
      protected function handleMouseWheel(event:MouseEvent) : void
      {
         this.position -= (event.delta > 0 ? 1 : -1) * this._pageScrollSize;
      }
      
      override protected function changeFocus() : void
      {
         this.thumb.displayFocus = _focused || _displayFocus;
      }
   }
}

