package communication
{
   import events.*;
   import flash.events.*;
   import flash.utils.*;
   import logging.*;
   
   public class Character
   {
      private static var onCreatingCharCallback:Function;
      
      private static var _list:Array;
      
      private static var _core:EventDispatcher;
      
      public static const UPDATED:String = "updated";
      
      public static const CHANGE:String = "changed";
      
      public static const MAX_COUNT:uint = 3;
      
      public static var currentId:int = -1;
      
      public static var Gold:int = 0;
      
      currentId = -1;
      Gold = 0;
      _list = new Array();
      
      public var id:int;
      
      public var maxSpeed:Number;
      
      public var name:String;
      
      public var hpRegeneration:Number;
      
      public var staminaRegeneration:Number;
      
      public var maxWeight:Number;
      
      public var isTutorialPassed:Boolean;
      
      public var deletionRemainingTime:int;
      
      public var goldCredit:int;
      
      public var maxHp:int;
      
      public var maxStamina:int;
      
      public function Character(param1:Object)
      {
         var data:Object = null;
         var loc1:* = undefined;
         var arg1:Object = param1;
         data = arg1;
         super();
         try
         {
            this.id = Boolean(data) && data.id != null ? int(data.id) : this.id;
            this.maxSpeed = Boolean(data) && data.maxspeed != null ? Number(data.maxspeed) : this.maxSpeed;
            this.name = Boolean(data) && Boolean(data.name) ? String(data.name) : this.name;
            this.maxHp = Boolean(data) && data.maxhp != null ? int(data.maxhp) : this.maxHp;
            this.hpRegeneration = Boolean(data) && data.hp_regen != null ? Number(data.hp_regen) : this.hpRegeneration;
            this.maxStamina = Boolean(data) && data.maxstamina != null ? int(data.maxstamina) : this.maxStamina;
            this.staminaRegeneration = Boolean(data) && data.stamina_regen != null ? Number(data.stamina_regen) : this.staminaRegeneration;
            this.maxWeight = Boolean(data) && data.maxweight != null ? Number(data.maxweight) : this.maxWeight;
            this.isTutorialPassed = Boolean(data) && data.isTutorialPassed != null ? data.isTutorialPassed == 1 : this.isTutorialPassed;
            this.deletionRemainingTime = Boolean(data) && data.deletion_remaining_time != null ? int(data.deletion_remaining_time) : this.deletionRemainingTime;
            this.goldCredit = Boolean(data) && data.goldCredit != null ? int(data.goldCredit) : this.goldCredit;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"Character.Constructor",error);
         }
      }
      
      protected static function onUpdateGoldHandler(param1:ApiEvent) : void
      {
         var _loc2_:* = null;
         var _loc3_:* = 0;
         if(param1.data.answer.new_gold != null)
         {
            _loc3_ = 0;
            while(_loc3_ < Character.list.length)
            {
               _loc2_ = Character.list[_loc3_] as Character;
               _loc2_.goldCredit = param1.data.answer.new_gold;
               Gold = param1.data.answer.new_gold;
               _loc3_++;
            }
            core.dispatchEvent(new Event(UPDATED));
         }
      }
      
      public static function get list() : Array
      {
         if(!_list)
         {
            _list = new Array();
         }
         return _list;
      }
      
      public static function get AllowNewCharCreating() : Boolean
      {
         return true;
      }
      
      public static function Update() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.Update");
         Api.self.addEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Api.call(Api.ALL_CHARACTERS_INFO);
      }
      
      public static function selectById(param1:int) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.selectById",param1);
         Api.call(Api.SELECT_CHAR,[{"id":param1}]);
         currentId = param1;
         core.dispatchEvent(new Event(CHANGE));
      }
      
      public static function DeleteLastSelectedChar() : void
      {
         Api.self.addEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Api.call(Api.DELETE_CHAR,[]);
         setTimeout(Update,300);
      }
      
      public static function RestoreLastSelectedChar() : void
      {
         Api.self.addEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Api.call(Api.RESTORE_CHAR,[]);
         setTimeout(Update,300);
      }
      
      public static function StartCharCreating(param1:Function) : void
      {
         onCreatingCharCallback = param1;
         Api.self.addEventListener(Api.CREATING_CHAR,onCreatingCharHandler);
         Api.call(Api.CREATING_CHAR,[]);
      }
      
      protected static function onCreatingCharHandler(param1:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.onCreatingCharHandler",param1.data.answer);
         if(onCreatingCharCallback != null)
         {
            onCreatingCharCallback(param1.data.answer);
         }
         onCreatingCharCallback = null;
      }
      
      public static function CancelCharCreating() : void
      {
         Api.call(Api.CANCEL_CREATE_CHAR,[]);
      }
      
      protected static function onAllCharInfoHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Logger.LogToChannel(Logger.DEBUG,"Character.onAllCharInfoHandler",param1.data.answer);
         parseCharacterList(param1.data.answer);
         core.dispatchEvent(new Event(Character.UPDATED));
      }
      
      protected static function onSelectCharHandler(param1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.SELECT_CHAR,onAllCharInfoHandler);
         Logger.LogToChannel(Logger.DEBUG,"Character.onSelectCharHandler",param1.data.answer);
      }
      
      protected static function parseCharacterList(param1:Object) : void
      {
         var _loc2_:* = null;
         currentId = param1.currentId;
         _list = new Array();
         var _loc3_:* = 0;
         var _loc4_:* = param1.list;
         for each(_loc2_ in _loc4_)
         {
            _list.push(new Character(_loc2_));
         }
      }
      
      public static function get EveryoneNotPassed() : Boolean
      {
         var _loc1_:* = null;
         var _loc2_:* = true;
         var _loc3_:* = 0;
         while(_loc3_ < Character.list.length)
         {
            _loc1_ = Character.list[_loc3_] as Character;
            if(_loc1_.isTutorialPassed == true)
            {
               _loc2_ = false;
            }
            _loc3_++;
         }
         return _loc2_;
      }
      
      public static function get CurrentNotPassed() : Boolean
      {
         return !current.isTutorialPassed;
      }
      
      public static function Init() : void
      {
         core;
      }
      
      public static function get current() : Character
      {
         return currentId >= 0 ? list[currentId] : null;
      }
      
      public static function get core() : EventDispatcher
      {
         if(!_core)
         {
            _core = new EventDispatcher();
            Api.self.addEventListener(Api.UPDATE_GOLD,onUpdateGoldHandler);
         }
         return _core;
      }
      
      public function get isNotDeleted() : Boolean
      {
         Logger.LogToChannel(Logger.WARNING,this.id,"Character.isNotDeleted",this.deletionRemainingTime);
         return this.deletionRemainingTime < 0;
      }
      
      public function get isCurrent() : Boolean
      {
         return Character.current.id == this.id;
      }
   }
}

