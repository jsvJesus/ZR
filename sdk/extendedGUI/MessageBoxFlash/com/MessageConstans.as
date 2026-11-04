package com
{
   public class MessageConstans
   {
      public static var Local:Object;
      
      public static const BTN_OK:* = "BTN_OK";
      
      public static const BTN_CANCEL:* = "BTN_CANCEL";
      
      public static const BTN_YES:* = "BTN_YES";
      
      public static const BTN_NO:* = "BTN_NO";
      
      public static const DESTROY:* = "destroy";
      
      public static const BUTTON_EV:* = {
         "BTN_OK":0,
         "BTN_CANCEL":1,
         "BTN_YES":2,
         "BTN_NO":3,
         "BTN_UNDO":4,
         "BTN_APPLY":5,
         "BTN_RESET":6
      };
      
      public static const EVENT_BTNPRESS:* = 0;
      
      public static const ERROR_NAME:* = "FlashMessageBox::object no has Name";
      
      public static const TYPE_MESSAGE:* = 0;
      
      public static const TYPE_INPUT:* = 1;
      
      public function MessageConstans()
      {
         super();
      }
   }
}

