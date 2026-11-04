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
      
      public function Dummy(param1:IEventDispatcher = null)
      {
         super(param1);
      }
      
      public static function get visible() : Boolean
      {
         return _visible;
      }
      
      public static function set visible(param1:Boolean) : void
      {
         if(_visible == param1)
         {
            return;
         }
         _visible = param1;
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
      
      public static function moveX(param1:Number, param2:Number) : void
      {
      }
      
      public static function moveY(param1:Number, param2:Number) : void
      {
      }
      
      public static function moveTo(param1:Point, param2:Number) : void
      {
         moveX(param1.x,param2);
         moveY(param1.y,param2);
      }
      
      public static function setSize(param1:Number) : void
      {
         Api.call(Api.RESIZE_DUMMY,[{"percent":param1}]);
      }
      
      public static function show() : void
      {
         if(Character.list.length)
         {
            _visible = true;
         }
      }
      
      protected static function onDummySizeAndPosition(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.DUMMY_SIZE_AND_POSITION,onDummySizeAndPosition);
         Logger.LogToChannel(Logger.DEBUG,"onDummySizeAndPosition",param1.data.answer.corner,param1.data.answer.height,param1.data.answer.width);
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
      
      public function set x(param1:int) : void
      {
         this._x = param1;
      }
      
      public function get y() : int
      {
         return this._y;
      }
      
      public function set y(param1:int) : void
      {
         this._y = param1;
      }
      
      public function get size() : Number
      {
         return this._size;
      }
      
      public function set size(param1:Number) : void
      {
         this._size = param1;
      }
   }
}

