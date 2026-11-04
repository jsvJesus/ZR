package com.dvalimona.components
{
   public class DialogButtonItem
   {
      public var label:String;
      
      public var callback:Function;
      
      public var button:PushButton;
      
      public var widthPercent:Number = 1;
      
      public var shortCut:uint = 0;
      
      public function DialogButtonItem(label:String, callback:Function, widthPercent:Number = 1, shortCut:uint = 0)
      {
         super();
         this.label = label;
         this.callback = callback;
         this.widthPercent = widthPercent;
         this.shortCut = shortCut;
      }
   }
}

