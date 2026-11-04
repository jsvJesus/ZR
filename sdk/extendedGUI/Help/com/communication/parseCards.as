package com.communication
{
   import com.components.Chapter;
   import com.controls.Card;
   import com.controls.Cards;
   import com.controls.Chapters;
   
   public class parseCards
   {
      public function parseCards()
      {
         super();
      }
      
      public static function getContents(param1:Array) : Array
      {
         var _loc3_:* = undefined;
         var _loc4_:Array = null;
         var _loc5_:Array = null;
         var _loc6_:Object = null;
         var _loc7_:Card = null;
         var _loc8_:Array = null;
         var _loc9_:* = undefined;
         var _loc10_:Object = null;
         var _loc11_:Card = null;
         var _loc2_:Array = new Array();
         for(_loc3_ in param1)
         {
            _loc4_ = new Array();
            _loc5_ = new Array();
            _loc6_ = new Object();
            _loc6_.chapter = param1[_loc3_].name;
            _loc6_.id = param1[_loc3_].id;
            if(param1[_loc3_].full_card != null)
            {
               _loc7_ = new Card(param1[_loc3_].id,"",param1[_loc3_].full_card,"");
               Cards.addCard(_loc7_);
            }
            if(param1[_loc3_].cards != null)
            {
               _loc8_ = new Array();
               for(_loc9_ in param1[_loc3_].cards)
               {
                  _loc10_ = new Object();
                  _loc10_.name = param1[_loc3_].cards[_loc9_].title;
                  _loc10_.id = param1[_loc3_].cards[_loc9_].id;
                  _loc8_.push(_loc10_);
                  _loc4_.push({
                     "name":param1[_loc3_].cards[_loc9_].title,
                     "id":param1[_loc3_].cards[_loc9_].id
                  });
                  _loc5_.push(param1[_loc3_].cards[_loc9_].id);
                  _loc11_ = new Card(param1[_loc3_].cards[_loc9_].id,param1[_loc3_].cards[_loc9_].title,param1[_loc3_].cards[_loc9_].full_card,Localization.getLocal(param1[_loc3_].cards[_loc9_].little_card.value));
                  Cards.addCard(_loc11_);
               }
               _loc6_.subchapters = _loc8_;
            }
            Chapters.addChapter(new Chapter(param1[_loc3_].id,param1[_loc3_].name,_loc4_,_loc5_));
            _loc2_.push(_loc6_);
         }
         return _loc2_;
      }
   }
}

