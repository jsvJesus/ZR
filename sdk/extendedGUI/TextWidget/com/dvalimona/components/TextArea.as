package com.dvalimona.components
{
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   
   public class TextArea extends Text
   {
      protected var _scrollbar:VScrollBar;
      
      public function TextArea(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0, param4:String = "")
      {
         super(param1,param2,param3,param4);
      }
      
      override protected function init() : void
      {
         super.init();
         addEventListener(MouseEvent.MOUSE_WHEEL,this.onMouseWheel);
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this._scrollbar = new VScrollBar(this,0,0,this.onScrollbarScroll);
         _tf.addEventListener(Event.SCROLL,this.onTextScroll);
         addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseDown);
      }
      
      protected function onMouseDown(param1:MouseEvent) : void
      {
         param1.stopImmediatePropagation();
      }
      
      protected function updateScrollbar() : void
      {
         var _loc1_:int = _tf.numLines - _tf.maxScrollV + 1;
         var _loc2_:Number = _loc1_ / _tf.numLines;
         this._scrollbar.setSliderParams(1,_tf.maxScrollV,_tf.scrollV);
         this._scrollbar.setThumbPercent(_loc2_);
         this._scrollbar.pageSize = _loc1_;
      }
      
      override public function draw() : void
      {
         super.draw();
         _tf.width = _width - this._scrollbar.width - 4;
         this._scrollbar.x = _width - this._scrollbar.width;
         this._scrollbar.height = _height;
         this._scrollbar.draw();
         addEventListener(Event.ENTER_FRAME,this.onTextScrollDelay);
      }
      
      protected function onTextScrollDelay(param1:Event) : void
      {
         removeEventListener(Event.ENTER_FRAME,this.onTextScrollDelay);
         this.updateScrollbar();
      }
      
      override protected function onChange(param1:Event) : void
      {
         super.onChange(param1);
         this.updateScrollbar();
      }
      
      protected function onScrollbarScroll(param1:Event) : void
      {
         _tf.scrollV = Math.round(this._scrollbar.value);
      }
      
      protected function onTextScroll(param1:Event) : void
      {
         this._scrollbar.value = _tf.scrollV;
         this.updateScrollbar();
      }
      
      protected function onMouseWheel(param1:MouseEvent) : void
      {
         this._scrollbar.value -= param1.delta;
         _tf.scrollV = Math.round(this._scrollbar.value);
      }
      
      override public function set enabled(param1:Boolean) : void
      {
         super.enabled = param1;
         _tf.tabEnabled = param1;
      }
      
      public function set autoHideScrollBar(param1:Boolean) : void
      {
         this._scrollbar.autoHide = param1;
      }
      
      public function get autoHideScrollBar() : Boolean
      {
         return this._scrollbar.autoHide;
      }
   }
}

