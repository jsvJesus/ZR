package communication
{
   import com.dvalimona.components.*;
   import events.*;
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   import logging.*;
   import ui.*;
   
   public class Premium
   {
      protected static var Prem:Object;
      
      public static var currentID:int;
      
      public static var Elapsed:Number;
      
      private static var _list:Array;
      
      private static var _core:EventDispatcher;
      
      public static const MAX_DESCRIPTION_LENGTH:uint = 120;
      
      public static const UPDATED:String = "updated";
      
      public static const SUCCESS:String = "success";
      
      public static const FAIL:String = "fail";
      
      _list = new Array();
      
      public var time:int;
      
      public var price:int;
      
      public var ratio:Number;
      
      public var caption:String;
      
      public var id:int;
      
      public var description:String;
      
      protected var Icon:Bitmap;
      
      protected var iconLoader:Loader = new Loader();
      
      public function Premium(param1:Object)
      {
         super();
         Logger.LogToChannel(Logger.DEBUG,"new Premium");
         this.time = param1.premium_time;
         this.price = param1.price;
         this.ratio = param1.ExpCoefficient;
         this.caption = param1.name;
         this.id = param1.premium_id;
         this.description = param1.description;
         this.loadIcon(param1.icon_path);
      }
      
      public static function get Current() : Premium
      {
         return byId(currentID);
      }
      
      public static function get list() : Array
      {
         if(!_list)
         {
            _list = new Array();
         }
         return _list;
      }
      
      public static function get core() : EventDispatcher
      {
         if(!_core)
         {
            _core = new EventDispatcher();
            Api.self.addEventListener(Api.UPDATE_PREMIUM,onUpdatePremiumHandler);
            Api.self.addEventListener(Api.PREMIUM_ACCOUNT_DATA,onGetPremiumAccountDataHandler);
         }
         return _core;
      }
      
      public static function Init() : void
      {
         core;
      }
      
      public static function Enable(param1:int) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Premium.Enable");
         Api.call(Api.UPDATE_PREMIUM,[{"premium_id":param1}]);
      }
      
      protected static function onUpdatePremiumHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.WARNING,"Premium.onUpdatePremiumHandler");
         if(param1.data.answer.result != 1)
         {
            core.dispatchEvent(new Event(FAIL));
            Base.navigator.showDialog(param1.data.answer.info.message.title,param1.data.answer.info.message.text,false,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
         }
         else
         {
            currentID = param1.data.answer.info.current;
            Elapsed = param1.data.answer.info.remaining_time;
            core.dispatchEvent(new Event(UPDATED));
            core.dispatchEvent(new Event(SUCCESS));
         }
      }
      
      public static function Update() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Premium.Update");
      }
      
      public static function TestHaveCurrentPremium() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.TestPremiumNumberOne");
         var _loc1_:* = {
            "premiums":[{
               "premium_time":864000,
               "price":100,
               "ExpCoefficient":2,
               "name":"Премиум 0",
               "premium_id":0,
               "description":"Позволяет 10 дней нормально играть и не париться"
            },{
               "premium_time":1296000,
               "price":200,
               "ExpCoefficient":2,
               "name":"Премиум 1",
               "premium_id":1,
               "description":"Позволяет 15 дней нормально играть и не париться"
            }],
            "current":0,
            "remaining_time":160000
         };
         parse(_loc1_);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      public static function TestNoCurrentPremium() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.TestPremiumNumberTwo");
         var _loc1_:* = {
            "premiums":[{
               "premium_time":864000,
               "price":100,
               "ExpCoefficient":2,
               "name":"Премиум 0",
               "premium_id":0,
               "description":"Позволяет 10 дней нормально играть и не париться"
            },{
               "premium_time":1296000,
               "price":200,
               "ExpCoefficient":2,
               "name":"Премиум 1",
               "premium_id":1,
               "description":"Позволяет 15 дней нормально играть и не париться"
            },{
               "premium_time":1296000,
               "price":200,
               "ExpCoefficient":2,
               "name":"Премиум 2",
               "premium_id":2,
               "description":"Позволяет 20 дней нормально играть и не париться"
            },{
               "premium_time":1296000,
               "price":200,
               "ExpCoefficient":2,
               "name":"Премиум 3",
               "premium_id":3,
               "description":"Позволяет 25 дней нормально играть и не париться"
            }],
            "current":-1,
            "remaining_time":-1
         };
         parse(_loc1_);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      protected static function onGetPremiumAccountDataHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.onAllCharInfoHandler",param1.data.answer);
         parse(param1.data.answer);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      protected static function sravn(param1:Object, param2:Object) : Boolean
      {
      }
      
      protected static function parse(param1:Object) : void
      {
         var _loc4_:Object = null;
         trace("Парсим иконки");
         Logger.LogToChannel(Logger.DEBUG,"Premium.parse");
         currentID = param1.current;
         Elapsed = param1.remaining_time;
         _list = new Array();
         var _loc2_:Array = new Array();
         var _loc3_:* = 0;
         trace("Парсим иконки");
         while(_loc3_ < param1.premiums.length)
         {
            _list.push(new Premium(param1.premiums[_loc3_]));
            _loc4_ = new Object();
            _loc4_.ID = param1.premiums[_loc3_].premium_id;
            _loc4_.Path = param1.premiums[_loc3_].icon_path;
            trace(param1.premiums[_loc3_].icon_path);
            _loc2_.push(_loc4_);
            _loc3_++;
            trace(_loc4_.Path);
         }
         PremiumIcons.LoadIcons(_loc2_);
         Prem = param1.premiums;
      }
      
      public static function byId(param1:int) : Premium
      {
         var _loc2_:* = 0;
         while(_loc2_ < list.length)
         {
            if((list[_loc2_] as Premium).id == param1)
            {
               return list[_loc2_] as Premium;
            }
            _loc2_++;
         }
         return null;
      }
      
      public function loadIcon(param1:String) : *
      {
         var _loc2_:URLRequest = new URLRequest(param1);
         this.iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,this.completeIconLoad);
         this.iconLoader.load(_loc2_);
      }
      
      protected function completeIconLoad(param1:Event) : *
      {
         this.iconLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE,this.completeIconLoad);
         if(this.iconLoader.content != null)
         {
            this.Icon = Bitmap(this.iconLoader.content);
         }
         else
         {
            this.Icon = new Bitmap(new defaultIcon(),"auto",true);
         }
      }
      
      public function get icon() : Bitmap
      {
         return PremiumIcons.byId(this.id);
      }
   }
}

