package communication
{
   import events.*;
   import flash.events.*;
   import flash.utils.*;
   import logging.*;
   
   public class Character
   {
      private static var onCreatingCharCallback:Function;
      
      private static var onConfigFaceCallback:Function;
      
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
      
      public var is_old:Boolean;
      
      public var name:String;
      
      public var last_space:String;
      
      public var last_money:Number;
      
      public var isTutorialPassed:Boolean;
      
      public var deletionRemainingTime:int;
      
      public var goldCredit:int;
      
      public function Character(arg1:Object)
      {
         var data:Object = null;
         var loc1:* = undefined;
         data = arg1;
         super();
         try
         {
            this.id = Boolean(data) && data.id != null ? int(data.id) : this.id;
            this.name = Boolean(data) && Boolean(data.name) ? String(data.name) : this.name;
            this.isTutorialPassed = Boolean(data) && data.isTutorialPassed != null ? data.isTutorialPassed == 1 : this.isTutorialPassed;
            this.deletionRemainingTime = Boolean(data) && data.deletion_remaining_time != null ? int(data.deletion_remaining_time) : this.deletionRemainingTime;
            this.goldCredit = Boolean(data) && data.goldCredit != null ? int(data.goldCredit) : this.goldCredit;
            this.is_old = data.is_old == null ? false : Boolean(data.is_old);
            this.last_space = Boolean(data) && Boolean(data.last_space) ? String(data.last_space) : this.last_space;
            this.last_money = Boolean(data) && data.last_money != null ? int(data.last_money) : this.last_money;
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"Character.Constructor",error);
         }
      }
      
      protected static function onUpdateGoldHandler(arg1:ApiEvent) : void
      {
         var loc1:* = null;
         var loc2:* = 0;
         if(arg1.data.answer.new_gold != null)
         {
            loc2 = 0;
            while(loc2 < Character.list.length)
            {
               loc1 = Character.list[loc2] as Character;
               loc1.goldCredit = arg1.data.answer.new_gold;
               Gold = arg1.data.answer.new_gold;
               loc2++;
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
      
      public static function selectById(arg1:int) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.selectById",arg1);
         Api.call(Api.SELECT_CHAR,[{"id":arg1}]);
         currentId = arg1;
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
      
      public static function StartCharCreating(arg1:Function, arg2:Function) : void
      {
         onCreatingCharCallback = arg1;
         onConfigFaceCallback = arg2;
         Api.self.addEventListener(Api.GET_FACE_CONFIG,onConfigFaceHandler);
         Api.self.addEventListener(Api.CREATING_CHAR,onCreatingCharHandler);
         Api.call(Api.GET_FACE_CONFIG,[]);
         Api.call(Api.CREATING_CHAR,[]);
      }
      
      public static function StartCharUpdating(arg1:Function, arg2:Function) : void
      {
         onCreatingCharCallback = arg1;
         onConfigFaceCallback = arg2;
         Api.self.addEventListener(Api.GET_FACE_CONFIG,onConfigFaceHandler);
         Api.self.addEventListener(Api.UPDATING_CHAR,onCreatingCharHandler);
         Api.call(Api.GET_FACE_CONFIG,[]);
         Api.call(Api.UPDATING_CHAR,[]);
      }
      
      public static function StartCharDonateFaceEdit(arg1:Function, arg2:Function) : *
      {
         onConfigFaceCallback = arg1;
         onCreatingCharCallback = arg2;
         Api.self.addEventListener(Api.GET_FACE_CONFIG,onConfigDonateFaceHandler);
         Api.self.addEventListener(Api.DONAT_UPDATING_CHAR,onCreatingCharHandler);
         Api.call(Api.GET_FACE_CONFIG,[]);
         Api.call(Api.DONAT_UPDATING_CHAR,[]);
      }
      
      protected static function onConfigDonateFaceHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_FACE_CONFIG,onConfigDonateFaceHandler);
         if(onConfigFaceCallback != null)
         {
            onConfigFaceCallback(arg1.data.answer,false);
         }
         onConfigFaceCallback = null;
      }
      
      protected static function onConfigFaceHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.GET_FACE_CONFIG,onConfigFaceHandler);
         if(onConfigFaceCallback != null)
         {
            onConfigFaceCallback(arg1.data.answer,true);
         }
         onConfigFaceCallback = null;
      }
      
      protected static function onCreatingCharHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.CREATING_CHAR,onCreatingCharHandler);
         Logger.LogToChannel(Logger.DEBUG,"Character.onCreatingCharHandler",arg1.data.answer);
         if(onCreatingCharCallback != null)
         {
            onCreatingCharCallback(arg1.data.answer);
         }
         onCreatingCharCallback = null;
      }
      
      public static function CancelCharCreating() : void
      {
         Api.call(Api.CANCEL_CREATE_CHAR,[]);
      }
      
      protected static function onAllCharInfoHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.ALL_CHARACTERS_INFO,onAllCharInfoHandler);
         Logger.LogToChannel(Logger.DEBUG,"Character.onAllCharInfoHandler",arg1.data.answer);
         parseCharacterList(arg1.data.answer);
         core.dispatchEvent(new Event(Character.UPDATED));
      }
      
      protected static function onSelectCharHandler(arg1:ApiEvent) : void
      {
         Api.self.removeEventListener(Api.SELECT_CHAR,onAllCharInfoHandler);
         Logger.LogToChannel(Logger.DEBUG,"Character.onSelectCharHandler",arg1.data.answer);
      }
      
      protected static function parseCharacterList(arg1:Object) : void
      {
         var loc1:* = null;
         currentId = arg1.currentId;
         _list = new Array();
         var loc2:* = 0;
         var loc3:* = arg1.list;
         for each(loc1 in loc3)
         {
            _list.push(new Character(loc1));
         }
      }
      
      public static function get EveryoneNotPassed() : Boolean
      {
         var loc1:* = null;
         var loc2:* = true;
         var loc3:* = 0;
         while(loc3 < Character.list.length)
         {
            loc1 = Character.list[loc3] as Character;
            if(loc1.isTutorialPassed == true)
            {
               loc2 = false;
            }
            loc3++;
         }
         return loc2;
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

