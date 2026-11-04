package communication
{
   import events.*;
   import flash.events.*;
   import flash.geom.*;
   import logging.*;
   
   public class Dummy extends EventDispatcher
   {
      private static var _self:Dummy;
      
      public static var DEBUG:Boolean = false;
      
      private static var _visible:Boolean = false;
      
      DEBUG = false;
      _visible = false;
      
      private var _x:int;
      
      private var _y:int;
      
      private var _size:Number = 1;
      
      public function Dummy(arg1:IEventDispatcher = null)
      {
         super(arg1);
      }
      
      public static function get visible() : Boolean
      {
         return _visible;
      }
      
      public static function set visible(arg1:Boolean) : void
      {
         if(_visible == arg1)
         {
            return;
         }
         _visible = arg1;
         if(_visible)
         {
            show();
         }
         else
         {
            hide();
         }
      }
      
      public static function get self() : Dummy
      {
         if(!_self)
         {
            _self = new Dummy();
         }
         return _self;
      }
      
      public static function moveX(arg1:Number, arg2:Number) : void
      {
      }
      
      public static function moveY(arg1:Number, arg2:Number) : void
      {
      }
      
      public static function moveTo(arg1:Point, arg2:Number) : void
      {
         moveX(arg1.x,arg2);
         moveY(arg1.y,arg2);
      }
      
      public static function setSize(arg1:Number) : void
      {
         Api.call(Api.RESIZE_DUMMY,[{"percent":arg1}]);
      }
      
      public static function show() : void
      {
         if(Character.list.length)
         {
            _visible = true;
         }
      }
      
      protected static function onDummySizeAndPosition(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.DUMMY_SIZE_AND_POSITION,onDummySizeAndPosition);
         Logger.LogToChannel(Logger.DEBUG,"onDummySizeAndPosition",arg1.data.answer.corner,arg1.data.answer.height,arg1.data.answer.width);
      }
      
      public static function hide() : void
      {
         _visible = false;
      }
      
      public static function sizeTestRandom() : void
      {
         setSize(Math.random());
      }
      
      public static function moveTestRandomXY() : void
      {
         moveTo(new Point(Math.random(),Math.random()),0.3);
      }
      
      public static function moveTestRandomX() : void
      {
         moveX(Math.random(),0.3);
      }
      
      public static function moveTestRandomY() : void
      {
         moveY(Math.random(),0.3);
      }
      
      public function get x() : int
      {
         return this._x;
      }
      
      public function set x(arg1:int) : void
      {
         this._x = arg1;
      }
      
      public function get y() : int
      {
         return this._y;
      }
      
      public function set y(arg1:int) : void
      {
         this._y = arg1;
      }
      
      public function get size() : Number
      {
         return this._size;
      }
      
      public function set size(arg1:Number) : void
      {
         this._size = arg1;
      }
   }
}

