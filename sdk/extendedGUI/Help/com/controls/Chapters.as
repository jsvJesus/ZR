package com.controls
{
   import com.components.Chapter;
   
   public class Chapters
   {
      protected static var _chapters:Object = new Object();
      
      public function Chapters()
      {
         super();
      }
      
      public static function addChapter(param1:Chapter) : *
      {
         _chapters[param1.id.toString()] = param1;
      }
      
      public static function getChapterById(param1:Number) : Chapter
      {
         var _loc2_:String = param1.toString();
         if(_chapters[_loc2_] != null)
         {
            return _chapters[_loc2_];
         }
         return null;
      }
   }
}

