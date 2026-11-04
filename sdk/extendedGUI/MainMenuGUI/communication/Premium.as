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
      
      public static var NO_ICON:Bitmap;
      
      public static var currentID:int;
      
      public static var frizeID:int;
      
      public static var Elapsed:Number;
      
      public static var boosterCurrent:int;
      
      public static var boosterName:String;
      
      public static var boosterElapsed:Number;
      
      private static var _list:Array;
      
      private static var _core:EventDispatcher;
      
      public static const MAX_DESCRIPTION_LENGTH:uint = 120;
      
      public static const UPDATED:String = "updated";
      
      public static const UPDATED_BOOSTER:String = "updated_booster";
      
      public static const SUCCESS:String = "success";
      
      public static const FAIL:String = "fail";
      
      _list = new Array();
      
      public var time:int;
      
      public var holiday:Boolean = false;
      
      public var price:int;
      
      public var ratio:Number;
      
      public var caption:String;
      
      public var id:int;
      
      public var description:String;
      
      protected var Icon:Bitmap;
      
      protected var iconLoader:Loader = new Loader();
      
      public function Premium(arg1:Object)
      {
         super();
         Logger.LogToChannel(Logger.DEBUG,"new Premium");
         this.time = arg1.premium_time;
         this.price = arg1.price;
         this.ratio = arg1.ExpCoefficient;
         this.caption = arg1.name;
         this.id = arg1.premium_id;
         this.description = arg1.description;
         if(arg1.holiday)
         {
            this.holiday = arg1.holiday;
         }
         this.loadIcon(arg1.icon_path);
      }
      
      public static function get Current() : Premium
      {
         return byId(currentID);
      }
      
      public static function get list() : Array
      {
         var i:int = 0;
         if(!_list)
         {
            _list = new Array();
         }
         var tmp_list:* = new Array();
         for(i in _list)
         {
            if(!_list[i].holiday)
            {
               tmp_list.push(_list[i]);
            }
         }
         return tmp_list;
      }
      
      public static function get core() : EventDispatcher
      {
         if(!_core)
         {
            _core = new EventDispatcher();
            Api.self.addEventListener(Api.UPDATE_PREMIUM,onUpdatePremiumHandler);
            Api.self.addEventListener(Api.UPDATE_BOOSTER,onUpdateBoosterHandler);
            Api.self.addEventListener(Api.PREMIUM_ACCOUNT_DATA,onGetPremiumAccountDataHandler);
         }
         return _core;
      }
      
      public static function Init() : void
      {
         core;
      }
      
      public static function Enable(arg1:int) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Premium.Enable");
         Api.call(Api.UPDATE_PREMIUM,[{"premium_id":arg1}]);
      }
      
      protected static function onUpdatePremiumHandler(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.WARNING,"Premium.onUpdatePremiumHandler");
         if(arg1.data.answer.result != 1)
         {
            core.dispatchEvent(new Event(FAIL));
            Base.navigator.showDialog(arg1.data.answer.info.message.title,arg1.data.answer.info.message.text,false,[new DialogButtonItem("extendedGUI.Dialogs.Ok",null,1)]);
         }
         else
         {
            currentID = arg1.data.answer.info.current;
            Elapsed = arg1.data.answer.info.remaining_time;
            core.dispatchEvent(new Event(UPDATED));
            core.dispatchEvent(new Event(SUCCESS));
         }
      }
      
      protected static function onUpdateBoosterHandler(arg1:ApiEvent) : void
      {
         boosterCurrent = arg1.data.answer.current;
         boosterElapsed = arg1.data.answer.remaining_time;
         boosterName = arg1.data.answer.name;
         core.dispatchEvent(new Event(UPDATED_BOOSTER));
      }
      
      public static function Update() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Premium.Update");
      }
      
      public static function TestHaveCurrentPremium() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.TestPremiumNumberOne");
         var loc1:* = {
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
         parse(loc1);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      public static function TestNoCurrentPremium() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.TestPremiumNumberTwo");
         var loc1:* = {
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
         parse(loc1);
         core.dispatchEvent(new Event(UPDATED));
      }
      
      protected static function onGetPremiumAccountDataHandler(arg1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.onAllCharInfoHandler",arg1.data.answer);
         parse(arg1.data.answer);
         core.dispatchEvent(new Event(UPDATED));
         core.dispatchEvent(new Event(UPDATED_BOOSTER));
      }
      
      protected static function parse(arg1:Object) : void
      {
         var Obj:Object = null;
         Logger.LogToChannel(Logger.DEBUG,"Premium.parse");
         currentID = arg1.current;
         frizeID = arg1.frize;
         Elapsed = arg1.remaining_time;
         boosterCurrent = arg1.booster_id;
         boosterElapsed = arg1.booster_time;
         boosterName = arg1.booster_name;
         _list = new Array();
         var paths:Array = new Array();
         var boostPaths:Array = new Array();
         var loc1:* = 0;
         while(loc1 < arg1.premiums.length)
         {
            _list.push(new Premium(arg1.premiums[loc1]));
            Obj = new Object();
            Obj.ID = arg1.premiums[loc1].premium_id;
            Obj.path = arg1.premiums[loc1].icon_path;
            paths.push(Obj);
            loc1++;
         }
         loc1 = 0;
         while(loc1 < arg1.boosters.length)
         {
            Obj = new Object();
            Obj.ID = arg1.boosters[loc1].booster_id;
            Obj.path = arg1.boosters[loc1].icon_path;
            Obj.path_with_prem = arg1.boosters[loc1].icon_path_with_prem;
            boostPaths.push(Obj);
            loc1++;
         }
         PremiumIcons.LoadNoPremiumIcon(arg1.no_premium_icon_path);
         PremiumIcons.LoadIcons(paths);
         BoosterIcons.LoadIcons(boostPaths);
         Prem = arg1.premiums;
      }
      
      public static function byId(arg1:int) : Premium
      {
         var loc1:* = 0;
         while(loc1 < _list.length)
         {
            if((_list[loc1] as Premium).id == arg1)
            {
               return _list[loc1] as Premium;
            }
            loc1++;
         }
         return null;
      }
      
      public function loadIcon(path:String) : *
      {
         var url:URLRequest = new URLRequest(path);
         this.iconLoader.contentLoaderInfo.addEventListener(Event.COMPLETE,this.completeIconLoad);
         this.iconLoader.load(url);
      }
      
      protected function completeIconLoad(e:Event) : *
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

