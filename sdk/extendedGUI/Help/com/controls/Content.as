package com.controls
{
   import com.components.Chapter;
   import com.events.ScrollEvent;
   import com.events.helpEvent;
   import com.greensock.TweenMax;
   import com.greensock.easing.*;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.utils.Constraints;
   
   public class Content extends UIComponent
   {
      public var ShapeForSize:MovieClip;
      
      protected var _offSet:Number = 0;
      
      protected var _position:Number = 0;
      
      protected var current_card:Number = -1;
      
      public var contents:Sprite = new Sprite();
      
      protected var _scrollBarValue:Object = "sbText";
      
      protected var _scrollBar:ScrollBar;
      
      protected var _chapt:Chapter;
      
      public function Content()
      {
         super();
         addEventListener(MouseEvent.MOUSE_WHEEL,this.handleMouseWheel);
         addChild(this.contents);
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.REFLOW);
      }
      
      protected function scrollAnimation(param1:Number, param2:Number) : *
      {
         if(Math.abs(param1) + this.ShapeForSize.height > this.contents.height && param1 != 0 && this.contents.height > this.ShapeForSize.height)
         {
            param1 = param1 + this.ShapeForSize.height - (this.contents.height - Math.abs(param1));
         }
         if(this.contents.height < this.ShapeForSize.height)
         {
            param1 = 0;
         }
         TweenMax.to(this.contents,0.6,{
            "y":param1,
            "ease":Expo.easeInOut,
            "delay":0
         });
         this.position = param1;
      }
      
      protected function onScroll(param1:helpEvent) : *
      {
         this.scrollAnimation(-this._chapt.getPosition(param1.parent_index),-this._chapt.getEndPosition(param1.parent_index));
      }
      
      public function setCard(param1:*) : *
      {
         var _loc2_:int = int(param1 / 100) * 100;
         if(this._chapt)
         {
            this._chapt.removeEventListener(helpEvent.SCROLL,this.onScroll);
         }
         if(this.current_card != _loc2_)
         {
            this.position = 0;
            this.current_card = _loc2_;
            this._chapt = Chapters.getChapterById(_loc2_);
            while(this.contents.numChildren > 0)
            {
               this.contents.removeChildAt(0);
            }
            this.contents.addChild(this._chapt);
            this._chapt.DrawCapter(this.ShapeForSize.width);
         }
         else
         {
            this._chapt = Chapters.getChapterById(this.current_card);
         }
         this._chapt.addEventListener(helpEvent.SCROLL,this.onScroll);
         if(param1 != _loc2_)
         {
            this.scrollAnimation(0 - this._chapt.getPosition(param1),0 - this._chapt.getEndPosition(param1));
         }
         else
         {
            this.scrollAnimation(0,0);
         }
         this.updateScroll();
      }
      
      public function set position(param1:Number) : void
      {
         this._position = param1;
         this.contents.y = param1;
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
         if(this.contents.y < 0)
         {
            this.position += param1 * 8;
         }
      }
      
      public function positionDown(param1:*) : *
      {
         if(this.contents.height + this.contents.y > this.ShapeForSize.height)
         {
            this.position += param1 * 8;
         }
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
         var _loc1_:Number = Math.max(0,this.contents.height);
         if(this._scrollBar is ScrollBar)
         {
            this._scrollBar.setScrollProperties(this.ShapeForSize.height,0,this.contents.height - this.ShapeForSize.height);
         }
         this._scrollBar.position = -this.position;
      }
   }
}

