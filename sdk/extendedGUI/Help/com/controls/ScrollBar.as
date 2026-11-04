package com.controls
{
   import com.events.ScrollEvent;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.geom.Point;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.constants.ScrollBarDirection;
   import scaleform.clik.constants.ScrollBarTrackMode;
   import scaleform.clik.controls.Button;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.interfaces.IScrollBar;
   import scaleform.clik.utils.Constraints;
   
   public class ScrollBar extends UIComponent implements IScrollBar
   {
      public var trackScrollPageSize:Number = 20;
      
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
      
      protected var _trackMode:String = "scrollToCursor";
      
      protected var _trackScrollPosition:Number = -1;
      
      protected var _trackDragMouseIndex:Number = -1;
      
      public var upArrow:Button;
      
      public var downArrow:Button;
      
      public function ScrollBar()
      {
         super();
      }
      
      public function set position(param1:Number) : void
      {
         param1 = Math.max(this._minPosition,Math.min(this._maxPosition,param1));
         if(param1 == this._position)
         {
            return;
         }
         this._position = param1;
         dispatchEvent(new ScrollEvent(ScrollEvent.SCROLL,-param1));
         this.updateThumb();
      }
      
      public function get position() : Number
      {
         return this._position;
      }
      
      public function get trackMode() : String
      {
         return this._trackMode;
      }
      
      public function set trackMode(param1:String) : void
      {
         if(param1 == this._trackMode)
         {
            return;
         }
         this._trackMode = param1;
         if(initialized)
         {
            this.track.autoRepeat = this.trackMode == ScrollBarTrackMode.SCROLL_TO_CURSOR;
         }
      }
      
      protected function updateTrack() : *
      {
      }
      
      public function set scrollTarget(param1:Object) : *
      {
         this._scrollTarget = parent.getChildByName(param1.toString());
         if(this._scrollTarget == null)
         {
            return;
         }
      }
      
      protected function onHandleScroll(param1:ScrollEvent) : *
      {
         this.position = param1.position;
         this.updateThumb();
      }
      
      override protected function initialize() : void
      {
         super.initialize();
         var _loc1_:Number = rotation;
         rotation = 0;
         if(this.downArrow)
         {
            constraints.addElement("downArrow",this.downArrow,Constraints.BOTTOM);
         }
         constraints.addElement("track",this.track,Constraints.TOP | Constraints.BOTTOM);
         rotation = _loc1_;
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.REFLOW);
      }
      
      override protected function configUI() : void
      {
         super.configUI();
         this.thumb.addEventListener(MouseEvent.MOUSE_DOWN,this.handleThumbPress,false,0,true);
         this.thumb.focusTarget = this;
         this.thumb.lockDragStateChange = true;
         this.track.addEventListener(MouseEvent.MOUSE_DOWN,this.handleTrackPress,false,0,true);
         this.track.addEventListener(MouseEvent.CLICK,this.handleTrackClick,false,0,true);
      }
      
      public function get isHorizontal() : Boolean
      {
         return this.direction == ScrollBarDirection.HORIZONTAL;
      }
      
      public function get availableHeight() : Number
      {
         return this.track.height - this.thumb.height + this.offsetBottom + this.offsetTop;
      }
      
      public function setScrollProperties(param1:Number, param2:Number, param3:Number, param4:Number = NaN) : void
      {
         this._pageSize = param1;
         if(!isNaN(param4))
         {
            this._pageScrollSize = param4;
         }
         this._minPosition = param2;
         this._maxPosition = param3;
         invalidateSize();
      }
      
      protected function updateThumb() : void
      {
         var _loc1_:Number = Math.max(1,this._maxPosition - this._minPosition + this._pageSize);
         var _loc2_:Number = this.track.height + this.offsetTop + this.offsetBottom;
         this.thumb.height = Math.max(this._minThumbSize,Math.min(_loc2_,this._pageSize / _loc1_ * _loc2_));
         if(this.thumb is UIComponent)
         {
            (this.thumb as UIComponent).validateNow();
         }
         this.updateThumbPosition();
      }
      
      protected function updateThumbPosition() : void
      {
         var _loc1_:Number = (this._position - this._minPosition) / (this._maxPosition - this._minPosition);
         var _loc2_:Number = this.track.y - this.offsetTop;
         var _loc3_:Number = Math.round(_loc1_ * this.availableHeight + _loc2_);
         this.thumb.y = _loc3_;
         this.thumb.visible = !(isNaN(_loc1_) || isNaN(this._pageSize) || this._maxPosition <= 0 || this._maxPosition == Infinity);
         var _loc4_:Boolean = this.thumb.visible && enabled;
         if(this.upArrow)
         {
            this.upArrow.enabled = _loc4_ && this.position > this._minPosition;
            this.upArrow.validateNow();
         }
         if(this.downArrow)
         {
            this.downArrow.enabled = _loc4_ && this.position < this._maxPosition;
            this.downArrow.validateNow();
         }
         this.track.enabled = this.track.mouseEnabled = _loc4_;
      }
      
      override protected function draw() : void
      {
         if(isInvalid(InvalidationType.SIZE))
         {
            setSize(_width,_height);
            this.drawLayout();
            this.updateThumb();
         }
         this.updateThumbPosition();
      }
      
      protected function drawLayout() : void
      {
         var _loc1_:Number = NaN;
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
            _loc1_ = width / actualWidth;
            scaleY = _loc1_;
         }
      }
      
      protected function handleThumbPress(param1:Event) : void
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
      
      protected function doDrag(param1:MouseEvent) : void
      {
         var _loc2_:Number = (mouseY - this._dragOffset.y - this.track.y) / this.availableHeight;
         this.position = this._minPosition + _loc2_ * (this._maxPosition - this._minPosition);
      }
      
      protected function endDrag(param1:MouseEvent) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.doDrag);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.endDrag);
         this._isDragging = false;
      }
      
      protected function handleTrackPress(param1:MouseEvent) : void
      {
         var _loc2_:Number = NaN;
         if(param1.shiftKey || this.trackMode == ScrollBarTrackMode.SCROLL_PAGE)
         {
            _loc2_ = (mouseY - this.thumb.height / 2 - this.track.y) / this.availableHeight;
            this.position = Math.round(_loc2_ * (this._maxPosition - this._minPosition) + this._minPosition);
            this.thumb.dispatchEvent(new MouseEvent(MouseEvent.MOUSE_OVER));
            this.thumb.dispatchEvent(new MouseEvent(MouseEvent.MOUSE_DOWN));
            this.handleThumbPress(param1);
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
      
      protected function handleTrackClick(param1:MouseEvent) : void
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
   }
}

