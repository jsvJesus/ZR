package com.dvalimona.components
{
   public class DialogButtonItem
   {
      public var label:String;
      
      public var callback:Function;
      
      public var button:PushButton;
      
      public var widthPercent:Number = 1;
      
      public var shortCut:uint = 0;
      
      public function DialogButtonItem(param1:String, param2:Function, param3:Number = 1, param4:uint = 0)
      {
         super();
         this.label = param1;
         this.callback = param2;
         this.widthPercent = param3;
         this.shortCut = param4;
      }
   }
}

