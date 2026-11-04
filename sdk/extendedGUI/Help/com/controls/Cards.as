package com.controls
{
   public class Cards
   {
      protected static var _cards:Object = new Object();
      
      public function Cards()
      {
         super();
      }
      
      public static function addCard(param1:Card) : void
      {
         _cards[param1.id.toString()] = param1;
      }
      
      public static function getCardById(param1:Number) : Card
      {
         var _loc2_:String = param1.toString();
         if(_cards[_loc2_] != null)
         {
            return _cards[_loc2_];
         }
         return new Card(-1,"",new Array());
      }
      
      public static function getLittleCardById(param1:Number) : LittleCard
      {
         var _loc2_:String = param1.toString();
         if(_cards[_loc2_] != null)
         {
            return _cards[_loc2_].little;
         }
         return new LittleCard(-1,"null","null");
      }
      
      public static function getArrayForFullCardById(param1:Number) : Array
      {
         var _loc2_:String = param1.toString();
         if(_cards[_loc2_] != null)
         {
            return _cards[_loc2_].full;
         }
         return new Array();
      }
   }
}

