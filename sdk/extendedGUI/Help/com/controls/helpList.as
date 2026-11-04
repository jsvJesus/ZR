package com.controls
{
   import com.events.ScrollEvent;
   import com.events.helpEvent;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.utils.Constraints;
   
   public class helpList extends UIComponent
   {
      public var SizeSymb:MovieClip;
      
      protected var _list:Array = new Array();
      
      protected var _offSet:Number = 0;
      
      protected var _position:Number = 0;
      
      public var List:Sprite = new Sprite();
      
      public var _data:Array = new Array();
      
      protected var _scrollBarValue:Object = "sbList";
      
      protected var _scrollBar:ScrollBar;
      
      public function helpList()
      {
         super();
         constraints.addElement("List",this.List,Constraints.TOP | Constraints.LEFT);
         addEventListener(MouseEvent.MOUSE_WHEEL,this.handleMouseWheel);
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.REFLOW);
      }
      
      public function set position(param1:Number) : void
      {
         this._position = param1;
         this.List.y = param1;
         if(this._scrollBar == null)
         {
            return;
         }
         this._scrollBar.position = -param1;
      }
      
      public function get position() : Number
      {
         return this._position;
      }
      
      public function positionUp(param1:*) : *
      {
         if(this.List.y < 0)
         {
            this.position += param1 * 4;
         }
      }
      
      public function positionDown(param1:*) : *
      {
         if(this.List.height + this.List.y > this.SizeSymb.height)
         {
            this.position += param1 * 4;
         }
      }
      
      public function data(param1:Array) : *
      {
         var _loc2_:* = undefined;
         var _loc3_:helpListItem = null;
         for(_loc2_ in param1)
         {
            _loc3_ = new helpListItem();
            if(param1[_loc2_].subchapters != null)
            {
               _loc3_.data = param1[_loc2_].subchapters;
            }
            else
            {
               _loc3_.unselected = true;
            }
            _loc3_.label = param1[_loc2_].chapter;
            _loc3_.index = _loc2_;
            _loc3_.id = param1[_loc2_].id;
            _loc3_.addEventListener(helpEvent.SELECT,this.handleClickListItem);
            this._list.push(_loc3_);
         }
         this.drawList();
      }
      
      protected function handleMouseWheel(param1:MouseEvent) : *
      {
         if(param1.delta < 0)
         {
            this.positionDown(param1.delta);
         }
         else
         {
            this.positionUp(param1.delta);
         }
      }
      
      protected function handleClickListItem(param1:helpEvent) : *
      {
         this.drawList();
         this.calculateHeight(0);
         dispatchEvent(new helpEvent(helpEvent.SELECT,param1.parent_index,param1.index));
      }
      
      protected function calculateHeight(param1:Number) : Number
      {
         if(this.List.height > this.SizeSymb.height)
         {
         }
         var _loc2_:Number = 0;
         var _loc3_:* = param1;
         while(_loc3_ < this._list.length)
         {
            _loc2_ += this._list[_loc3_].height;
            _loc3_++;
         }
         return _loc2_;
      }
      
      protected function drawList() : *
      {
         var _loc3_:* = undefined;
         var _loc1_:* = 0;
         while(_loc1_ < this.List.numChildren)
         {
            if(!(this.List.getChildAt(_loc1_) is MovieClip))
            {
               this.List.removeChild(this.List.getChildAt(_loc1_));
            }
            _loc1_++;
         }
         var _loc2_:Number = 0;
         for(_loc3_ in this._list)
         {
            if(_loc2_ + this._list[_loc3_].height >= this.height)
            {
            }
            this._list[_loc3_].y = _loc2_;
            this._list[_loc3_].x = 0;
            this._list[_loc3_].width = this.SizeSymb.width;
            this._list[_loc3_].scaleX = 1;
            this._list[_loc3_].scaleY = 1;
            this.List.addChild(this._list[_loc3_]);
            _loc2_ += this._list[_loc3_].height;
         }
         addChild(this.List);
         this.updateScroll();
      }
      
      public function get scrollBar() : Object
      {
         return this._scrollBar;
      }
      
      public function set scrollBar(param1:Object) : void
      {
         this._scrollBarValue = param1;
         invalidate(InvalidationType.SCROLL_BAR);
      }
      
      override protected function draw() : void
      {
         if(isInvalid(InvalidationType.SCROLL_BAR))
         {
            this.createScrollBar();
         }
         if(isInvalid(InvalidationType.DATA))
         {
            this.updateScroll();
         }
      }
      
      protected function createScrollBar() : void
      {
         var _loc1_:ScrollBar = null;
         if(this._scrollBar)
         {
            this._scrollBar.removeEventListener(ScrollEvent.SCROLL,this.handleScroll,false);
            this._scrollBar.focusTarget = null;
            this._scrollBar = null;
         }
         if(!this._scrollBarValue || this._scrollBarValue == "")
         {
            return;
         }
         if(this._scrollBarValue is String)
         {
            if(parent != null)
            {
               _loc1_ = parent.getChildByName(this._scrollBarValue.toString()) as ScrollBar;
            }
         }
         this._scrollBar = _loc1_;
         if(this._scrollBar == null)
         {
            return;
         }
         this._scrollBar.addEventListener(ScrollEvent.SCROLL,this.handleScroll,false,0,true);
         this._scrollBar.focusTarget = this;
         this._scrollBar.tabEnabled = false;
         this._scrollBar.scrollTarget = this.name;
      }
      
      protected function handleScroll(param1:ScrollEvent) : void
      {
         if(this.position == param1.position)
         {
            return;
         }
         this.position = param1.position;
      }
      
      protected function updateScroll() : *
      {
         if(this._scrollBar == null)
         {
            return;
         }
         var _loc1_:Number = Math.max(0,this.List.height);
         if(this._scrollBar is ScrollBar)
         {
            this._scrollBar.setScrollProperties(this.SizeSymb.height,0,this.List.height - this.SizeSymb.height);
         }
         this._scrollBar.position = -this.position;
      }
   }
}

