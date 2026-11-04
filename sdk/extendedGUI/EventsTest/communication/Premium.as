package communication
{
   import com.dvalimona.components.DialogButtonItem;
   import events.ApiEvent;
   import flash.display.Bitmap;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import logging.Logger;
   import ui.PremiumIcons;
   
   public class Premium
   {
      public static var currentID:int;
      
      public static var Elapsed:Number;
      
      private static var _core:EventDispatcher;
      
      public static const UPDATED:String = "updated";
      
      public static const SUCCESS:String = "success";
      
      public static const FAIL:String = "fail";
      
      private static var _list:Array = new Array();
      
      public var time:int;
      
      public var price:int;
      
      public var ratio:Number;
      
      public var caption:String;
      
      public var id:int;
      
      public var description:String;
      
      public function Premium(data:Object)
      {
         super();
         Logger.LogToChannel(Logger.DEBUG,"new Premium");
         this.time = data.premium_time;
         this.price = data.price;
         this.ratio = data.ExpCoefficient;
         this.caption = data.name;
         this.id = data.premium_id;
         this.description = data.description;
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
      
      public static function Enable(pId:int) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Premium.Enable");
         Api.call(Api.UPDATE_PREMIUM,[{"premium_id":pId}]);
      }
      
      protected static function onUpdatePremiumHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.WARNING,"Premium.onUpdatePremiumHandler");
         if(event.data.answer.result == 1)
         {
            currentID = event.data.answer.info.current;
            Elapsed = event.data.answer.info.remaining_time;
            core.dispatchEvent(new Event(UPDATED));
            core.dispatchEvent(new Event(SUCCESS));
         }
         else
         {
            core.dispatchEvent(new Event(FAIL));
            Base.navigator.showDialog(event.data.answer.info.message.title,event.data.answer.info.message.text,false,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
         }
      }
      
      public static function Update() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Premium.Update");
      }
      
      public static function TestHaveCurrentPremium() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.TestPremiumNumberOne");
         var test:Object = {
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
         parse(test);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      public static function TestNoCurrentPremium() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.TestPremiumNumberTwo");
         var test:Object = {
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
         parse(test);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      protected static function onGetPremiumAccountDataHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.onAllCharInfoHandler",event.data.answer);
         parse(event.data.answer);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      protected static function parse(data:Object) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Premium.parse");
         currentID = data.current;
         Elapsed = data.remaining_time;
         _list = new Array();
         for(var i:uint = 0; i < data.premiums.length; i++)
         {
            _list.push(new Premium(data.premiums[i]));
         }
      }
      
      public static function byId(pId:int) : Premium
      {
         for(var i:uint = 0; i < list.length; i++)
         {
            if((list[i] as Premium).id == pId)
            {
               return list[i] as Premium;
            }
         }
         return null;
      }
      
      public function get icon() : Bitmap
      {
         return PremiumIcons.byId(this.id);
      }
   }
}

