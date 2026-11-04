package com.dvalimona.components
{
   public class DialogButtonItem
   {
      public var label:String;
      
      public var callback:Function;
      
      public var button:PushButton;
      
      public var widthPercent:Number = 1;
      
      public var shortCuts:Array = new Array();
      
      public function DialogButtonItem(label:String, callback:Function, widthPercent:Number = 1, shortCuts:Array = null)
      {
         super();
         this.label = label;
         this.callback = callback;
         this.widthPercent = widthPercent;
         if(shortCuts != null)
         {
            this.shortCuts = shortCuts;
         }
      }
   }
}

