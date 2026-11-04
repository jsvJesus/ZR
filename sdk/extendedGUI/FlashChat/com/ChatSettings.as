package com
{
   public class ChatSettings
   {
      protected static var _active_sounds:Array = new Array();
      
      protected static var _locked_sounds:Array = new Array();
      
      public function ChatSettings()
      {
         super();
      }
      
      public static function setSoundschannels(data:Object) : *
      {
         var i:* = undefined;
         _active_sounds = new Array();
         for(i in data.active)
         {
            _active_sounds.push(data.active[i]);
         }
         _locked_sounds = new Array();
         for(i in data.locked)
         {
            _locked_sounds.push(data.locked[i]);
         }
      }
      
      public static function getSoundschannels() : Array
      {
         return _active_sounds;
      }
      
      public static function isLocked(chID:int) : Boolean
      {
         var i:* = undefined;
         for(i in _locked_sounds)
         {
            if(_locked_sounds[i] == chID)
            {
               return true;
            }
         }
         return false;
      }
      
      public static function getCh(chID:int) : Boolean
      {
         var i:* = undefined;
         for(i in _active_sounds)
         {
            if(_active_sounds[i] == chID)
            {
               return true;
            }
         }
         return false;
      }
      
      public static function setCh(chID:int, value:Boolean) : *
      {
         var index:int = int(_active_sounds.indexOf(chID));
         if(index >= 0 && !value)
         {
            if(index >= 0)
            {
               if(index === _active_sounds.length - 1)
               {
                  _active_sounds.pop();
               }
               else
               {
                  _active_sounds[index] = _active_sounds.pop();
               }
            }
         }
         if(index < 0 && value)
         {
            _active_sounds.push(chID);
         }
      }
   }
}

