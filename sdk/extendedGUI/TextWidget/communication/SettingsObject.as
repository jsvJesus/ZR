package communication
{
   import logging.*;
   
   public class SettingsObject
   {
      public var path:Array;
      
      public function SettingsObject(param1:Array)
      {
         super();
         this.path = param1;
      }
      
      public static function Explore(param1:Object, param2:Array) : Object
      {
         var _loc3_:Object = null;
         var _loc4_:Object = param1;
         var _loc5_:int = 0;
         while(_loc5_ < param2.length)
         {
            if(_loc4_[param2[_loc5_]])
            {
               if(_loc4_.hasOwnProperty(param2[_loc5_]))
               {
                  _loc3_ = _loc4_[param2[_loc5_]];
                  _loc4_ = _loc3_;
               }
            }
            _loc5_++;
         }
         return _loc3_;
      }
      
      public static function makeApiObjectFromPathArray(param1:Array, param2:*) : Object
      {
         var _loc3_:Object = {};
         Logger.LogToChannel(Logger.DEBUG,"makeObjectFromPathArray",param2,Number(param2));
         switch(param1.length)
         {
            case 1:
               _loc3_[param1[0]] = param2;
               break;
            case 2:
               _loc3_[param1[0]] = {};
               _loc3_[param1[0]][param1[1]] = param2;
               break;
            case 3:
               _loc3_[param1[0]] = {};
               _loc3_[param1[0]][param1[1]] = {};
               _loc3_[param1[0]][param1[1]][param1[2]] = param2;
               break;
            case 4:
               _loc3_[param1[0]] = {};
               _loc3_[param1[0]][param1[1]] = {};
               _loc3_[param1[0]][param1[1]][param1[2]] = {};
               _loc3_[param1[0]][param1[1]][param1[2]][param1[3]] = param2;
               break;
            case 5:
               _loc3_[param1[0]] = {};
               _loc3_[param1[0]][param1[1]] = {};
               _loc3_[param1[0]][param1[1]][param1[2]] = {};
               _loc3_[param1[0]][param1[1]][param1[2]][param1[3]] = {};
               _loc3_[param1[0]][param1[1]][param1[2]][param1[3]][param1[4]] = param2;
         }
         Logger.LogToChannel(Logger.DEBUG,"makeObjectFromPathArray",_loc3_);
         return _loc3_;
      }
      
      public static function merge(param1:Array) : Object
      {
         var _loc3_:Object = null;
         var _loc4_:String = null;
         var _loc2_:Object = {};
         for each(_loc3_ in param1)
         {
            for(_loc4_ in _loc3_)
            {
               if(_loc2_[_loc4_] != null)
               {
                  _loc2_[_loc4_] = merge([_loc2_[_loc4_],_loc3_[_loc4_]]);
               }
               else
               {
                  _loc2_[_loc4_] = _loc3_[_loc4_];
               }
            }
         }
         return _loc2_;
      }
      
      public function get data() : Object
      {
         return Explore(Settings.Data,this.path);
      }
      
      public function get range() : Object
      {
         return Explore(Settings.Ranges,this.path);
      }
      
      public function get defaultData() : Object
      {
         return Explore(Settings.Default,this.path);
      }
   }
}

