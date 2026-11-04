package communication
{
   import events.ApiEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import logging.Logger;
   
   public class Character
   {
      private static var _core:EventDispatcher;
      
      private static var onCreatingCharCallback:Function;
      
      public static const UPDATED:String = "updated";
      
      public static const CHANGE:String = "changed";
      
      public static const MAX_COUNT:uint = 3;
      
      public static var currentId:int = -1;
      
      public static var Gold:int = 0;
      
      private static var _list:Array = new Array();
      
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
      
      public function Character(data:Object)
      {
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
      
      protected static function onUpdateGoldHandler(event:ApiEvent) : void
      {
         var char:Character = null;
         var i:uint = 0;
         if(event.data.answer.new_gold != null)
         {
            for(i = 0; i < Character.list.length; i++)
            {
               char = Character.list[i] as Character;
               char.goldCredit = event.data.answer.new_gold;
               Gold = event.data.answer.new_gold;
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
      
      public static function selectById(id:int) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.selectById",id);
         Api.call(Api.SELECT_CHAR,[{"id":id}]);
         currentId = id;
         core.dispatchEvent(new Event(CHANGE));
      }
      
      public static function DeleteLastSelectedChar() : void
      {
         Api.self.addEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Api.call(Api.DELETE_CHAR,[]);
      }
      
      public static function RestoreLastSelectedChar() : void
      {
         Api.self.addEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Api.call(Api.RESTORE_CHAR,[]);
      }
      
      public static function StartCharCreating(callback:Function) : void
      {
         onCreatingCharCallback = callback;
         Api.self.addEventListener(Api.CREATING_CHAR,onCreatingCharHandler);
         Api.call(Api.CREATING_CHAR,[]);
      }
      
      protected static function onCreatingCharHandler(event:ApiEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.onCreatingCharHandler",event.data.answer);
         if(onCreatingCharCallback != null)
         {
            onCreatingCharCallback(event.data.answer);
         }
         onCreatingCharCallback = null;
      }
      
      public static function CancelCharCreating() : void
      {
         Api.call(Api.CANCEL_CREATE_CHAR,[]);
      }
      
      protected static function onAllCharInfoHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Logger.LogToChannel(Logger.DEBUG,"Character.onAllCharInfoHandler",event.data.answer);
         parseCharacterList(event.data.answer);
         core.dispatchEvent(new Event(Character.UPDATED));
      }
      
      protected static function onSelectCharHandler(event:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.SELECT_CHAR,onAllCharInfoHandler);
         Logger.LogToChannel(Logger.DEBUG,"Character.onSelectCharHandler",event.data.answer);
      }
      
      protected static function parseCharacterList(data:Object) : void
      {
         var char:Object = null;
         currentId = data.currentId;
         _list = new Array();
         for each(char in data.list)
         {
            _list.push(new Character(char));
         }
      }
      
      public static function get EveryoneNotPassed() : Boolean
      {
         var char:Character = null;
         var result:Boolean = true;
         for(var i:uint = 0; i < Character.list.length; i++)
         {
            char = Character.list[i] as Character;
            if(char.isTutorialPassed == true)
            {
               result = false;
            }
         }
         return result;
      }
      
      public static function get CurrentNotPassed() : Boolean
      {
         return !current.isTutorialPassed;
      }
      
      public static function Init() : void
      {
         core;
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

